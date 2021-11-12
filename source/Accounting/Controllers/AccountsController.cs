using System.Threading.Tasks;
using Accounting.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AccountsController : Controller
    {
        private readonly DataContext _context;

        public AccountsController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Accounts.ToListAsync());
        }
    }
}