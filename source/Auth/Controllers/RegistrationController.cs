using System.Threading.Tasks;
using Auth.Context;
using Auth.Models;
using Common.MessageBroker;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMessageBrokerProducer _messageBrokerProducer;

        public RegistrationController(DataContext context, UserManager<AppUser> userManager, IMessageBrokerProducer messageBrokerProducer)
        {
            _context = context;
            _userManager = userManager;
            _messageBrokerProducer = messageBrokerProducer;
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
                await _messageBrokerProducer.Produce("auth-stream",
                    new { PublicId = user.Id, Username = user.UserName, Email = user.Email, Role = user.Role?.Name });

                return RedirectToAction("Index", "Home");
            }

            return BadRequest();
        }
    }
}