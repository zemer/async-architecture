using System;
using System.Threading;
using Accounting.Context;
using Common.MessageBroker;
using Common.SchemaRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Task = System.Threading.Tasks.Task;

namespace Accounting.Schedule
{
    public class PaySchedule : IScheduledTask
    {
        private readonly IServiceProvider _scopeFactory;

        public PaySchedule(IServiceProvider scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        #region Implementation of IScheduledTask

        public string Schedule => "0 18 * * *";

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var schemaRegistry = scope.ServiceProvider.GetRequiredService<ISchemaRegistry>();
            var messageBrokerProducer = scope.ServiceProvider.GetRequiredService<IMessageBrokerProducer>();

            var accounts = await context.Accounts.Include(a => a.Transactions)
                                        .ToArrayAsync();
            foreach (var account in accounts)
            {
                if (account.Bill > 0)
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
                            accountId = account.PublicId,
                            fee = account.Bill
                        }
                    };

                    if (schemaRegistry.Validate(data, SchemaRegistry.Schemas.Accounting.Pay.V1))
                    {
                        messageBrokerProducer.Produce("accounting-pay", data);
                    }

                    await context.Payments.AddAsync(new Payment
                    {
                        Account = account,
                        Fee = account.Bill,
                        Date = DateTimeOffset.Now
                    });

                    account.Bill = 0;

                    await context.SaveChangesAsync();
                }
            }
        }

        #endregion
    }
}