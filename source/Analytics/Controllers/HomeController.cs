using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Analytics.Context;
using Analytics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analytics.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class HomeController : Controller
    {
        private readonly DataContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(DataContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTimeOffset.Now;

            var todayTransactions = await _context.Transactions
                .Where(t => t.Date.Date == now.Date)
                .ToArrayAsync();

            var completedTaskAmount = todayTransactions
                .Select(t => t.Accrued)
                .Sum();
            var assignedTaskFee = todayTransactions
                .Select(t => t.WrittenOff)
                .Sum();

            var negativeParrots = await _context.Accounts.CountAsync(a => a.Bill < 0);

            var mostExpensiveMonthTask = await _context.Tasks
                .Where(t => t.CompleteCost == _context.Tasks
                    .Where(ti => ti.Completed)
                    .Where(ti => ti.DateCompleted.Month == now.Month)
                    .Max(ti => ti.CompleteCost))
                .FirstOrDefaultAsync();

            var mostExpensiveWeekTask = await _context.Tasks
                .Where(t => t.CompleteCost == _context.Tasks
                    .Where(ti => ti.Completed)
                    .Where(ti => ti.DateCompleted.DayOfYear <= now.DayOfYear && ti.DateCompleted.DayOfYear >= now.AddDays(-7)
                        .DayOfYear)
                    .Max(ti => ti.CompleteCost))
                .FirstOrDefaultAsync();

            var mostExpensiveDayTask = await _context.Tasks
                .Where(t => t.CompleteCost == _context.Tasks
                    .Where(ti => ti.Completed)
                    .Where(ti => ti.DateCompleted.DayOfYear == now.DayOfYear)
                    .Max(ti => ti.CompleteCost))
                .FirstOrDefaultAsync();

            var model = new HomeModel
            {
                Bill = (completedTaskAmount + assignedTaskFee) * -1,
                NegativeParrots = negativeParrots,
                MostExpensiveMonthTask = $"{mostExpensiveMonthTask?.Description} - {mostExpensiveMonthTask?.CompleteCost}$",
                MostExpensiveWeekTask = $"{mostExpensiveWeekTask?.Description} - {mostExpensiveWeekTask?.CompleteCost}$",
                MostExpensiveDayTask = $"{mostExpensiveDayTask?.Description} - {mostExpensiveDayTask?.CompleteCost}$"
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}