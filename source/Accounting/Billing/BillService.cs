using Accounting.Context;

namespace Accounting.Billing
{
    public interface IBillService
    {
        void Debit(Account account, Task task);

        void Credit(Account account, Task task);
    }

    public class BillService : IBillService
    {
        private readonly DataContext _context;

        public BillService(DataContext context)
        {
            _context = context;
        }

        public void Debit(Account account, Task task)
        {
            account.Bill -= task.AssignCost;
            _context.SaveChanges();
        }

        public void Credit(Account account, Task task)
        {
            account.Bill += task.CompleteCost;
            _context.SaveChanges();
        }
    }
}