using System;
using System.Linq;
using System.Text;
using Accounting.Billing;
using Accounting.Context;
using Common.SchemaRegistry;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Accounting.MessageBroker
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

            ConsumeAccountsStream();
            ConsumeTasksStream();
            ConsumeTasksAssigned();
            ConsumeTasksCompleted();
        }

        private void ConsumeAccountsStream()
        {
            _channel.ExchangeDeclare("accounts-stream", "direct");

            var queue = _channel.QueueDeclare("accounts-stream-accounting", true, false, false, null);
            _channel.QueueBind(queue.QueueName, "accounts-stream", "");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnAccountsStreamReceived;
            _channel.BasicConsume(queue.QueueName, true, consumer);
        }

        private void ConsumeTasksStream()
        {
            _channel.ExchangeDeclare("tasks-stream", "direct");

            var queue = _channel.QueueDeclare("tasks-stream-accounting", true, false, false, null);
            _channel.QueueBind(queue.QueueName, "tasks-stream", "");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnTasksStreamReceived;
            _channel.BasicConsume(queue.QueueName, true, consumer);
        }

        private void ConsumeTasksAssigned()
        {
            _channel.ExchangeDeclare("tasks-assigned", "direct");

            var queue = _channel.QueueDeclare("tasks-assigned-accounting", true, false, false, null);
            _channel.QueueBind(queue.QueueName, "tasks-assigned", "");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnTasksAssignedReceived;
            _channel.BasicConsume(queue.QueueName, true, consumer);
        }

        private void ConsumeTasksCompleted()
        {
            _channel.ExchangeDeclare("tasks-completed", "direct");

            var queue = _channel.QueueDeclare("tasks-completed-accounting", true, false, false, null);
            _channel.QueueBind(queue.QueueName, "tasks-completed", "");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnTasksCompletedReceived;
            _channel.BasicConsume(queue.QueueName, true, consumer);
        }

        private async void OnAccountsStreamReceived(object model, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceProvider.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            if (_schemaRegistry.Validate(message, SchemaRegistry.Schemas.Accounts.Stream.V1))
            {
                var payload = JsonConvert.DeserializeObject<AccountsStream>(message);
                var data = payload.Data;

                lock (this)
                {
                    var account = context.Accounts.FirstOrDefault(a => a.PublicId == data.AccountId);

                    if (account is null)
                    {
                        context.Accounts.Add(new Account
                        {
                            PublicId = data.AccountId,
                            Username = data.Username,
                            Email = data.Email,
                            Role = data.Role
                        });

                        context.SaveChanges();
                    }
                    else
                    {
                        account.Username = data.Username;
                        account.Email = data.Email;
                        account.Role = data.Role;

                        context.SaveChanges();
                    }
                }
            }
        }

        private async void OnTasksStreamReceived(object model, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceProvider.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            if (_schemaRegistry.Validate(message, SchemaRegistry.Schemas.Tasks.Stream.V1))
            {
                var payload = JsonConvert.DeserializeObject<TasksStream>(message);
                var data = payload.Data;

                lock (this)
                {
                    var task = context.Tasks.FirstOrDefault(a => a.PublicId == data.TaskId);

                    if (task is null)
                    {
                        var costCalculator = _serviceProvider.GetRequiredService<ICostCalculator>();

                        context.Tasks.Add(new Task
                        {
                            PublicId = data.TaskId,
                            Description = data.Description,
                            JiraId = data.JiraId,
                            AssignCost = costCalculator.GetAssignCost(),
                            CompleteCost = costCalculator.GetCompleteCost()
                        });

                        context.SaveChanges();
                    }
                    else
                    {
                        task.Description = data.Description;
                        task.JiraId = data.JiraId;

                        context.SaveChanges();
                    }
                }
            }
        }

        private async void OnTasksAssignedReceived(object model, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceProvider.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            if (_schemaRegistry.Validate(message, SchemaRegistry.Schemas.Tasks.Assigned.V1))
            {
                var payload = JsonConvert.DeserializeObject<TasksAssigned>(message);
                var data = payload.Data;

                var account = context.Accounts.FirstOrDefault(a => a.PublicId == data.AccountId)
                              ?? new Account
                              {
                                  PublicId = data.AccountId
                              };

                lock (this)
                {
                    var task = context.Tasks.FirstOrDefault(a => a.PublicId == data.TaskId);

                    if (task is null)
                    {
                        var costCalculator = _serviceProvider.GetRequiredService<ICostCalculator>();

                        task = new Task
                        {
                            PublicId = data.TaskId,
                            Account = account,
                            AssignCost = costCalculator.GetAssignCost(),
                            CompleteCost = costCalculator.GetCompleteCost()
                        };

                        context.Tasks.Add(task);

                        context.SaveChanges();
                    }
                    else
                    {
                        task.Account = account;

                        context.SaveChanges();
                    }

                    var billService = scope.ServiceProvider.GetRequiredService<IBillService>();
                    billService.WriteOff(account, task);
                }
            }
        }

        private async void OnTasksCompletedReceived(object model, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceProvider.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            if (_schemaRegistry.Validate(message, SchemaRegistry.Schemas.Tasks.Completed.V1))
            {
                var payload = JsonConvert.DeserializeObject<TasksCompleted>(message);
                var data = payload.Data;

                lock (this)
                {
                    var account = context.Accounts.FirstOrDefault(a => a.PublicId == data.AccountId)
                                  ?? new Account
                                  {
                                      PublicId = data.AccountId
                                  };

                    var task = context.Tasks.FirstOrDefault(a => a.PublicId == data.TaskId);

                    if (task is null)
                    {
                        var costCalculator = _serviceProvider.GetRequiredService<ICostCalculator>();

                        task = new Task
                        {
                            PublicId = data.TaskId,
                            Account = account,
                            AssignCost = costCalculator.GetAssignCost(),
                            CompleteCost = costCalculator.GetCompleteCost()
                        };

                        context.Tasks.Add(task);

                        context.SaveChanges();
                    }
                    else
                    {
                        task.Account = account;

                        context.SaveChanges();
                    }

                    var billService = scope.ServiceProvider.GetRequiredService<IBillService>();
                    billService.Charge(account, task);
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

    internal record AccountsStream(string EventId, int EventVersion, string EventName, string EventTime, string Producer, AccountStreamInfo Data);

    internal record AccountStreamInfo(string AccountId, string Username, string Email, string Role);

    internal record TasksStream(string EventId, int EventVersion, string EventName, string EventTime, string Producer, TaskStreamInfo Data);

    internal record TaskStreamInfo(string TaskId, string Description, string JiraId);

    internal record TasksAssigned(string EventId, int EventVersion, string EventName, string EventTime, string Producer, TaskAssignedInfo Data);

    internal record TaskAssignedInfo(string TaskId, string AccountId);

    internal record TasksCompleted(string EventId, int EventVersion, string EventName, string EventTime, string Producer, TaskCompletedInfo Data);

    internal record TaskCompletedInfo(string TaskId, string AccountId);
}