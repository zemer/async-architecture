using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Accounting.Context;
using Accounting.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accounting.Controllers
{
    [Authorize]
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
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)
                             ?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.PublicId == userId);

            var myTasks = await _context.Transactions
                                        .Include(t => t.Account)
                                        .Include(t => t.Task)
                                        .Where(t => t.Account != null)
                                        .Where(t => t.Account.PublicId == userId)
                                        .OrderByDescending(t => t.Date)
                                        .ToArrayAsync();

            var model = new HomeModel
            {
                Bill = account?.Bill,
                Transactions = myTasks
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