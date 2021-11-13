using System;
using Accounting.Context;
using Common.MessageBroker;
using Common.SchemaRegistry;

namespace Accounting.Billing
{
    public interface IBillService
    {
        void WriteOff(Account account, Task task);

        void Charge(Account account, Task task);
    }

    public class BillService : IBillService
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IMessageBrokerProducer _messageBrokerProducer;
        private readonly DataContext _context;

        public BillService(DataContext context, ISchemaRegistry schemaRegistry, IMessageBrokerProducer messageBrokerProducer)
        {
            _context = context;
            _schemaRegistry = schemaRegistry;
            _messageBrokerProducer = messageBrokerProducer;
        }

        public void WriteOff(Account account, Task task)
        {
            account.Bill -= task.AssignCost;

            var transaction = new Transaction()
            {
                PublicId = Guid.NewGuid()
                               .ToString(),
                Task = task,
                WrittenOff = task.AssignCost,
                Date = DateTimeOffset.Now
            };
            account.Transactions.Add(transaction);

            _context.SaveChanges();

            ProduceStream(account, task, transaction);
        }

        public void Charge(Account account, Task task)
        {
            account.Bill += task.CompleteCost;

            var transaction = new Transaction()
            {
                PublicId = Guid.NewGuid()
                               .ToString(),
                Task = task,
                Accrued = task.CompleteCost,
                Date = DateTimeOffset.Now
            };
            account.Transactions.Add(transaction);

            _context.SaveChanges();

            ProduceStream(account, task, transaction);
        }

        private void ProduceStream(Account account, Task task, Transaction transaction)
        {
            var data = new
            {
                eventId = Guid.NewGuid()
                              .ToString(),
                eventVersion = 1,
                eventName = "Accounting.Pay",
                eventTime = DateTimeOffset.Now.ToString(),
                producer = "Accounting",
                data = new
                {
                    transactionId = transaction.PublicId,
                    accountId = account.PublicId,
                    taskId = task.PublicId,
                    date = transaction.Date,
                    accrued = transaction.Accrued,
                    writtenOff = transaction.WrittenOff
                }
            };

            if (_schemaRegistry.Validate(data, SchemaRegistry.Schemas.Transactions.Stream.V1))
            {
                _messageBrokerProducer.Produce("transactions-stream", data);
            }
        }
    }
}