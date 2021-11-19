using System;
using System.Linq;
using System.Text;
using Analytics.Context;
using Common.SchemaRegistry;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Analytics.MessageBroker
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

            ConsumeTasksStream();
            ConsumeTransactionsStream();
            ConsumeAccountingCosts();
            ConsumeTasksCompleted();
        }

        private void ConsumeTasksStream()
        {
            _channel.ExchangeDeclare("tasks-stream", "direct");

            var queue = _channel.QueueDeclare("tasks-stream-analytics", true, false, false, null);
            _channel.QueueBind(queue.QueueName, "tasks-stream", "");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnTasksStreamReceived;
            _channel.BasicConsume(queue.QueueName, true, consumer);
        }

        private void ConsumeTransactionsStream()
        {
            _channel.ExchangeDeclare("transactions-stream", "direct");

            var queue = _channel.QueueDeclare("transactions-stream-analytics", true, false, false, null);
            _channel.QueueBind(queue.QueueName, "transactions-stream", "");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnTransactionsStreamReceived;
            _channel.BasicConsume(queue.QueueName, true, consumer);
        }

        private void ConsumeAccountingCosts()
        {
            _channel.ExchangeDeclare("accounting-costs", "direct");

            var queue = _channel.QueueDeclare("accounting-costs-analytics", true, false, false, null);
            _channel.QueueBind(queue.QueueName, "accounting-costs", "");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnAccountingCostsReceived;
            _channel.BasicConsume(queue.QueueName, true, consumer);
        }

        private void ConsumeTasksCompleted()
        {
            _channel.ExchangeDeclare("tasks-completed", "direct");

            var queue = _channel.QueueDeclare("tasks-completed-analytics", true, false, false, null);
            _channel.QueueBind(queue.QueueName, "tasks-completed", "");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnTasksCompletedReceived;
            _channel.BasicConsume(queue.QueueName, true, consumer);
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
                        context.Tasks.Add(new Task
                        {
                            PublicId = data.TaskId,
                            Description = data.Description,
                            JiraId = data.JiraId
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

        private async void OnTransactionsStreamReceived(object model, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceProvider.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            if (_schemaRegistry.Validate(message, SchemaRegistry.Schemas.Transactions.Stream.V1))
            {
                var payload = JsonConvert.DeserializeObject<TransactionsStream>(message);
                var data = payload.Data;

                lock (this)
                {
                    var account = context.Accounts.FirstOrDefault(a => a.PublicId == data.AccountId);
                    if (account is null)
                    {
                        account = new Account
                        {
                            PublicId = data.AccountId,
                            Bill = data.AccountBill
                        };
                        context.Accounts.Add(account);
                    }
                    else
                    {
                        account.Bill = data.AccountBill;
                    }

                    var task = context.Tasks.FirstOrDefault(a => a.PublicId == data.TaskId);
                    if (task is null)
                    {
                        task = new Task
                        {
                            PublicId = data.TaskId
                        };
                        context.Tasks.Add(task);
                    }

                    task.Account = account;

                    task.Transactions.Add(new Transaction
                    {
                        Account = account,
                        PublicId = data.TransactionId,
                        Date = data.Date,
                        WrittenOff = data.WrittenOff,
                        Accrued = data.Accrued
                    });

                    context.SaveChanges();
                }
            }
        }

        private async void OnAccountingCostsReceived(object model, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceProvider.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            if (_schemaRegistry.Validate(message, SchemaRegistry.Schemas.Accounting.Costs.V1))
            {
                var payload = JsonConvert.DeserializeObject<AccountingCosts>(message);
                var data = payload.Data;

                lock (this)
                {
                    var task = context.Tasks.FirstOrDefault(a => a.PublicId == data.TaskId);
                    if (task is null)
                    {
                        task = new Task
                        {
                            PublicId = data.TaskId
                        };
                        context.Tasks.Add(task);
                    }

                    task.CompleteCost = data.CompleteCost;

                    context.SaveChanges();
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
                    var account = context.Accounts.FirstOrDefault(a => a.PublicId == data.AccountId);
                    if (account is null)
                    {
                        account = new Account
                        {
                            PublicId = data.AccountId
                        };
                        context.Accounts.Add(account);
                    }

                    var task = context.Tasks.FirstOrDefault(a => a.PublicId == data.TaskId);
                    if (task is null)
                    {
                        task = new Task
                        {
                            PublicId = data.TaskId
                        };
                        context.Tasks.Add(task);
                    }

                    task.Account = account;
                    task.Completed = true;
                    task.DateCompleted = DateTimeOffset.Now;

                    context.SaveChanges();
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

    internal record AccountingCosts(string EventId, int EventVersion, string EventName, string EventTime, string Producer, AccountigCostsInfo Data);

    internal record AccountigCostsInfo(string TaskId, float AssignCost, float CompleteCost);

    internal record TasksCompleted(string EventId, int EventVersion, string EventName, string EventTime, string Producer, TaskCompletedInfo Data);

    internal record TaskCompletedInfo(string TaskId, string AccountId);

    internal record TransactionsStream(string EventId, int EventVersion, string EventName, string EventTime, string Producer,
        TransactionStreamInfo Data);

    internal record TransactionStreamInfo(string TransactionId, string AccountId, float AccountBill, string TaskId, DateTimeOffset Date,
        float Accrued,
        float WrittenOff);
}