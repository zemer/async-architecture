using System;
using System.Text;
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
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public Consumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            var factory = new ConnectionFactory() { HostName = "localhost" };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare("auth-stream",
                false,
                false,
                false,
                null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnReceived;
            _channel.BasicConsume("auth-stream",
                true,
                consumer);
        }

        private async void OnReceived(object model, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceProvider.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            var accountStream = JsonConvert.DeserializeObject<AccountStream>(message);

            var account = await context.Accounts.FirstOrDefaultAsync(a => a.PublicId == accountStream.PublicId);

            if (account is null)
            {
                await context.Accounts.AddAsync(new Account
                {
                    PublicId = accountStream.PublicId,
                    Username = accountStream.Username,
                    Email = accountStream.Email,
                    Role = accountStream.Role
                });

                await context.SaveChangesAsync();
            }
            else
            {
                account.Username = accountStream.Username;
                account.Email = accountStream.Email;
                account.Role = accountStream.Role;

                await context.SaveChangesAsync();
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

    public class AccountStream
    {
        public string PublicId { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string Role { get; set; }
    }
}