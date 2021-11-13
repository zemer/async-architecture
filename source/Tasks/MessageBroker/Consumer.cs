using System;
using System.Text;
using Common.SchemaRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Tasks.Context;

namespace Tasks.MessageBroker
{
    public interface IMessageBrokerConsumer
    {
    }

    public class Consumer : IMessageBrokerConsumer, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISchemaRegistry _schemaRegistry;

        private readonly IConnection _connection;
        private readonly IModel _channel;

        public Consumer(IServiceProvider serviceProvider, ISchemaRegistry schemaRegistry)
        {
            _serviceProvider = serviceProvider;
            _schemaRegistry = schemaRegistry;
            var factory = new ConnectionFactory() { HostName = "localhost" };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare("accounts-stream", "direct");

            var queue = _channel.QueueDeclare("accounts-stream-tasks", true, false, false, null);

            _channel.QueueBind(queue.QueueName, "accounts-stream", "");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnReceived;
            _channel.BasicConsume(queue.QueueName, true, consumer);
        }

        private async void OnReceived(object model, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceProvider.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            if (_schemaRegistry.Validate(message, SchemaRegistry.Schemas.Accounts.Stream.V1))
            {
                var payload = JsonConvert.DeserializeObject<AccountsStream>(message);
                var data = payload.Data;

                var account = await context.Accounts.FirstOrDefaultAsync(a => a.PublicId == data.AccountId);

                if (account is null)
                {
                    await context.Accounts.AddAsync(new Account
                    {
                        PublicId = data.AccountId,
                        Username = data.Username,
                        Email = data.Email,
                        Role = data.Role
                    });

                    await context.SaveChangesAsync();
                }
                else
                {
                    account.Username = data.Username;
                    account.Email = data.Email;
                    account.Role = data.Role;

                    await context.SaveChangesAsync();
                }
            }
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
        }

        #endregion
    }

    internal record AccountsStream(string EventId, int EventVersion, string EventName, string EventTime, string Producer, AccountInfo Data);

    internal record AccountInfo(string AccountId, string Username, string Email, string Role);
}