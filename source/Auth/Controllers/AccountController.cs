using System;
using System.Linq;
using System.Threading.Tasks;
using Auth.Context;
using Auth.Models;
using Common.MessageBroker;
using Common.SchemaRegistry;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Auth.Controllers
{
    public class AccountController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IMessageBrokerProducer _messageBrokerProducer;
        private readonly ISchemaRegistry _schemaRegistry;

        public AccountController(DataContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager,
            IMessageBrokerProducer messageBrokerProducer, ISchemaRegistry schemaRegistry)
        {
            _context = context;
            _messageBrokerProducer = messageBrokerProducer;
            _schemaRegistry = schemaRegistry;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.Include(u => u.Role)
                                      .ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                                     .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoginModel request)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser
                {
                    Email = request.Email,
                    UserName = request.Email
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (result.Succeeded)
                {
                    await _context.Entry(user)
                                  .Reference(u => u.Role)
                                  .LoadAsync();

                    var data = new
                    {
                        eventId = Guid.NewGuid()
                                      .ToString(),
                        eventVersion = 1,
                        eventName = "Accounts.Stream",
                        eventTime = DateTimeOffset.Now.ToString(),
                        producer = "Accounts",
                        data = new
                        {
                            accountId = user.Id,
                            username = user.UserName,
                            email = user.Email,
                            role = user.Role?.Name
                        }
                    };

                    if (_schemaRegistry.Validate(data, SchemaRegistry.Schemas.Accounts.Stream.V1))
                    {
                        _messageBrokerProducer.Produce("accounts-stream", data);
                    }

                    return RedirectToAction(nameof(Index));
                }
            }

            return View(request);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _context.Entry(user)
                          .Reference(u => u.Role)
                          .LoadAsync();

            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToArrayAsync(), nameof(AppRole.Id), nameof(AppRole.Name));

            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id, UserName, Email, RoleId")] AppUser user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var dbUser = await _userManager.FindByIdAsync(id);

                    dbUser.UserName = user.UserName;
                    dbUser.Email = user.Email;
                    dbUser.RoleId = user.RoleId;

                    await _userManager.UpdateAsync(dbUser);
                    await _context.SaveChangesAsync();

                    await _context.Entry(dbUser)
                                  .Reference(u => u.Role)
                                  .LoadAsync();

                    var data = new
                    {
                        eventId = Guid.NewGuid()
                                      .ToString(),
                        eventVersion = 1,
                        eventName = "Accounts.Stream",
                        eventTime = DateTimeOffset.Now.ToString(),
                        producer = "Accounts",
                        data = new
                        {
                            accountId = dbUser.Id,
                            username = dbUser.UserName,
                            email = dbUser.Email,
                            role = dbUser.Role?.Name
                        }
                    };

                    if (_schemaRegistry.Validate(data, SchemaRegistry.Schemas.Accounts.Stream.V1))
                    {
                        _messageBrokerProducer.Produce("accounts-stream", data);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
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

            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                                     .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _context.Users.FindAsync(id);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(string id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }

    public class CrateUserRequest
    {
        public string Email { get; set; }
    }
}