using System;
using Accounting.Context;

namespace Accounting.Billing
{
    public interface IBillService
    {
        void WriteOff(Account account, Task task);

        void Charge(Account account, Task task);
    }

    public class BillService : IBillService
    {
        private readonly DataContext _context;

        public BillService(DataContext context)
        {
            _context = context;
        }

        public void WriteOff(Account account, Task task)
        {
            account.Bill -= task.AssignCost;

            account.Transactions.Add(new Transaction()
            {
                PublicId = Guid.NewGuid()
                               .ToString(),
                Task = task,
                WrittenOff = task.AssignCost,
                Date = DateTimeOffset.Now
            });

            _context.SaveChanges();
        }

        public void Charge(Account account, Task task)
        {
            account.Bill += task.CompleteCost;

            account.Transactions.Add(new Transaction()
            {
                PublicId = Guid.NewGuid()
                               .ToString(),
                Task = task,
                Accrued = task.CompleteCost,
                Date = DateTimeOffset.Now
            });

            _context.SaveChanges();
        }
    }
}