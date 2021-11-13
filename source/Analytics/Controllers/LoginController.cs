using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Analytics.Context;
using Analytics.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Analytics.Controllers
{
    public class LoginController : Controller
    {
        private readonly DataContext _context;

        public LoginController(DataContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<ActionResult<User>> Login(LoginModel request)
        {
            using (var client = new HttpClient())
            {
                var response = await client.PostAsJsonAsync("https://localhost:44311/api/auth/", request);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(body);

                    var handler = new JwtSecurityTokenHandler();
                    var jwtSecurityToken = handler.ReadJwtToken(user.Token);

                    var claims = new List<Claim>()
                    {
                        new(ClaimTypes.NameIdentifier,
                            jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)
                                            ?.Value ?? string.Empty),
                        new(ClaimTypes.Name,
                            jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId)
                                            ?.Value ?? string.Empty)
                    };

                    foreach (var claim in jwtSecurityToken.Claims.Where(c => c.Type == "role"))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                    }

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties()
                    {
                        IsPersistent = true
                    });
                }
                else
                {
                    return RedirectToAction("Error");
                }

                return RedirectToAction("Index", "Home");
            }
        }
    }

    public class User
    {
        public string Token { get; set; }

        public string UserName { get; set; }

        public string Image { get; set; }
    }
}