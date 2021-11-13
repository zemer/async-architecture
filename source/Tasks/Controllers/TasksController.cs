using System;
using System.Linq;
using System.Threading.Tasks;
using Common.MessageBroker;
using Common.SchemaRegistry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tasks.Context;
using Task = Tasks.Context.Task;

namespace Tasks.Controllers
{
    public class TasksController : Controller
    {
        private readonly DataContext _context;
        private readonly IMessageBrokerProducer _messageBrokerProducer;
        private readonly ISchemaRegistry _schemaRegistry;

        public TasksController(DataContext context, IMessageBrokerProducer messageBrokerProducer, ISchemaRegistry schemaRegistry)
        {
            _context = context;
            _messageBrokerProducer = messageBrokerProducer;
            _schemaRegistry = schemaRegistry;
        }

        // GET: Tasks
        public async Task<IActionResult> Index()
        {
            return View(await _context.Tasks.Include(t => t.Account)
                                      .ToListAsync());
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                                     .FirstOrDefaultAsync(m => m.TaskId == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // GET: Tasks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tasks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,JiraId")] Task task)
        {
            if (ModelState.IsValid)
            {
                task.PublicId = Guid.NewGuid()
                                    .ToString();
                _context.Add(task);
                await _context.SaveChangesAsync();

                var data = new
                {
                    eventId = Guid.NewGuid()
                                  .ToString(),
                    eventVersion = 1,
                    eventName = "Tasks.Stream",
                    eventTime = DateTimeOffset.Now.ToString(),
                    producer = "Tasks",
                    data = new
                    {
                        taskId = task.PublicId,
                        description = task.Description,
                        jiraId = task.JiraId
                    }
                };

                if (_schemaRegistry.Validate(data, SchemaRegistry.Schemas.Tasks.Stream.V1))
                {
                    _messageBrokerProducer.Produce("tasks-stream", data);
                }

                await AssignTask(task);

                return RedirectToAction(nameof(Index));
            }

            return View(task);
        }

        public async Task<IActionResult> Assign()
        {
            var tasks = await _context.Tasks.Where(t => !t.Completed)
                                      .ToArrayAsync();

            foreach (var task in tasks)
            {
                await AssignTask(task);
            }

            return RedirectToAction(nameof(Index));
        }

        private async System.Threading.Tasks.Task AssignTask(Task task)
        {
            var responsible = await _context.Accounts
                                            .Where(a => a.Role != "Administrator" && a.Role != "Manager")
                                            .OrderBy(a => Guid.NewGuid())
                                            .Take(1)
                                            .FirstOrDefaultAsync();

            if (responsible != null)
            {
                task.Account = responsible;
                await _context.SaveChangesAsync();

                var data = new
                {
                    eventId = Guid.NewGuid()
                                  .ToString(),
                    eventVersion = 1,
                    eventName = "Tasks.Assigned",
                    eventTime = DateTimeOffset.Now.ToString(),
                    producer = "Tasks",
                    data = new
                    {
                        taskId = task.PublicId,
                        accountId = task.Account?.PublicId
                    }
                };

                if (_schemaRegistry.Validate(data, SchemaRegistry.Schemas.Tasks.Assigned.V1))
                {
                    _messageBrokerProducer.Produce("tasks-assigned", data);
                }
            }
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: Tasks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TaskId,Description,JiraId,PublicId")] Task task)
        {
            if (id != task.TaskId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var dbTask = await _context.Tasks.FindAsync(id);
                    dbTask.Description = task.Description;
                    dbTask.JiraId = task.JiraId;

                    await _context.SaveChangesAsync();

                    var data = new
                    {
                        eventId = Guid.NewGuid()
                                      .ToString(),
                        eventVersion = 1,
                        eventName = "Tasks.Stream",
                        eventTime = DateTimeOffset.Now.ToString(),
                        producer = "Tasks",
                        data = new
                        {
                            taskId = dbTask.PublicId,
                            description = dbTask.Description,
                            jiraId = dbTask.JiraId
                        }
                    };

                    if (_schemaRegistry.Validate(data, SchemaRegistry.Schemas.Tasks.Stream.V1))
                    {
                        _messageBrokerProducer.Produce("tasks-stream", data);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(task.TaskId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(task);
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                                     .FirstOrDefaultAsync(m => m.TaskId == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: Tasks/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.TaskId == id);
        }
    }
}