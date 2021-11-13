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
            var todayTransactions = await _context.Transactions
                                                  .Where(t => t.Date.Date == DateTimeOffset.Now.Date)
                                                  .ToArrayAsync();

            var completedTaskAmount = todayTransactions
                                      .Select(t => t.Accrued)
                                      .Sum();
            var assignedTaskFee = todayTransactions
                                  .Select(t => t.WrittenOff)
                                  .Sum();

            var negativeParrots = await _context.Accounts.CountAsync(a => a.Bill < 0);

            var model = new HomeModel
            {
                Bill = (completedTaskAmount + assignedTaskFee) * -1,
                NegativeParrots = negativeParrots
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