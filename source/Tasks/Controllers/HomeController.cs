using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Common.MessageBroker;
using Common.SchemaRegistry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tasks.Context;
using Tasks.Models;

namespace Tasks.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly DataContext _context;
        private readonly IMessageBrokerProducer _messageBrokerProducer;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger<HomeController> _logger;

        public HomeController(DataContext context, IMessageBrokerProducer messageBrokerProducer, ISchemaRegistry schemaRegistry,
            ILogger<HomeController> logger)
        {
            _context = context;
            _messageBrokerProducer = messageBrokerProducer;
            _logger = logger;
            _schemaRegistry = schemaRegistry;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)
                             ?.Value;
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

            await _context.Entry(dbTask)
                          .Reference(t => t.Account)
                          .LoadAsync();

            dbTask.Completed = true;

            await _context.SaveChangesAsync();

            var data = new
            {
                eventId = Guid.NewGuid()
                              .ToString(),
                eventVersion = 1,
                eventName = "Tasks.Completed",
                eventTime = DateTimeOffset.Now.ToString(),
                producer = "Tasks",
                data = new
                {
                    taskId = dbTask.PublicId,
                    accountId = dbTask.Account?.PublicId
                }
            };

            if (_schemaRegistry.Validate(data, SchemaRegistry.Schemas.Tasks.Completed.V1))
            {
                _messageBrokerProducer.Produce("tasks-completed", data);
            }

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}