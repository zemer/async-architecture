using System;
using System.Linq;
using System.Threading.Tasks;
using Accounting.Context;
using Accounting.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Controllers
{
    [Authorize(Roles = "Administrator, Accountant")]
    public class StatisticsController : Controller
    {
        private readonly DataContext _context;

        public StatisticsController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var todayTransactions = await _context.Transactions.Where(t => t.Date.Date == DateTimeOffset.Now.Date)
                                                  .ToArrayAsync();

            var completedTaskAmount = todayTransactions.Select(t => t.Accrued)
                                                       .Sum();
            var assignedTaskFee = todayTransactions.Select(t => t.WrittenOff)
                                                   .Sum();

            var todayPayments = await _context.Payments.Where(p => p.Date.Date == DateTimeOffset.Now.Date)
                                              .Include(p => p.Account)
                                              .ToArrayAsync();

            var model = new StatisticsModel
            {
                Bill = (completedTaskAmount + assignedTaskFee) * -1,
                Payments = todayPayments
            };

            return View(model);
        }
    }
}