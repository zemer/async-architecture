using System;
using System.Threading.Tasks;
using Auth.Context;
using Auth.Models;
using Common.MessageBroker;
using Common.SchemaRegistry;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMessageBrokerProducer _messageBrokerProducer;
        private readonly ISchemaRegistry _schemaRegistry;

        public RegistrationController(DataContext context, UserManager<AppUser> userManager, IMessageBrokerProducer messageBrokerProducer,
            ISchemaRegistry schemaRegistry)
        {
            _context = context;
            _userManager = userManager;
            _messageBrokerProducer = messageBrokerProducer;
            _schemaRegistry = schemaRegistry;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult<User>> Register(RegisterModel request)
        {
            if (await _userManager.FindByEmailAsync(request.Email) is not null)
            {
                return View("Index", request);
            }

            var user = new AppUser
            {
                Email = request.Email,
                UserName = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
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
                    await _messageBrokerProducer.Produce("accounts-stream", data);
                }

                return RedirectToAction("Index", "Home");
            }

            return BadRequest();
        }
    }
}