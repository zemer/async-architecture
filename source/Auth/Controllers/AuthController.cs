using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Auth.Context;
using Auth.Models;
using Auth.Security;
using Microsoft.AspNetCore.Identity;

namespace Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IJwtGenerator _jwtGenerator;

        public AuthController(DataContext context, UserManager<AppUser> userManager, IJwtGenerator jwtGenerator)
        {
            _context = context;
            _userManager = userManager;
            _jwtGenerator = jwtGenerator;
        }

        [HttpPost]
        public async Task<ActionResult<User>> SignIn(LoginQuery request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.CheckPasswordAsync(user, request.Password);

            if (result)
            {
                await _context.Entry(user).Reference(r => r.Role).LoadAsync();

                return new User
                {
                    Token = await _jwtGenerator.CreateToken(user),
                    UserName = user.UserName,
                    Image = null
                };
            }

            return Forbid();
        }
    }

    public class LoginQuery
    {
        public string Email { get; set; }

        public string Password { get; set; }
    }

    public class User
    {
        public string Token { get; set; }

        public string UserName { get; set; }

        public string Image { get; set; }
    }
}