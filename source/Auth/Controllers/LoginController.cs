using System.Threading.Tasks;
using Auth.Context;
using Auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers
{
    public class LoginController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LoginController(DataContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult<User>> Login(LoginModel request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return View("Index", request);
            }

            var result = await _userManager.CheckPasswordAsync(user, request.Password);

            if (result)
            {
                return RedirectToAction("Index", "Home");

                return new User
                {
                    //Token = _jwtGenerator.CreateToken(user),
                    UserName = user.UserName,
                    Image = null
                };
            }

            return BadRequest();
        }
    }
}