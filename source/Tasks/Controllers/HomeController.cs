using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Common.MessageBroker;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Tasks.Context;
using Tasks.Models;
using Task = System.Threading.Tasks.Task;

namespace Tasks.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly DataContext _context;
        private readonly IMessageBrokerProducer _messageBrokerProducer;
        private readonly ILogger<HomeController> _logger;

        public HomeController(DataContext context, IMessageBrokerProducer messageBrokerProducer, ILogger<HomeController> logger)
        {
            _context = context;
            _messageBrokerProducer = messageBrokerProducer;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var myTasks = _context.Tasks
                .Include(t => t.Account)
                .Where(t => t.Account != null)
                .Where(t => t.Account.PublicId == userId)
                .ToListAsync();

            return View(await myTasks);
        }

        public async Task<IActionResult> Complete(Context.Task task)
        {
            var dbTask = await _context.Tasks.FindAsync(task.TaskId);
            if (dbTask == null)
            {
                return NotFound();
            }

            dbTask.Completed = true;

            await _context.SaveChangesAsync();

            await _messageBrokerProducer.Produce("tasks-completed", new { task.PublicId });

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}