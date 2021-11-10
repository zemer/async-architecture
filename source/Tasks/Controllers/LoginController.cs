using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Tasks.Context;
using Tasks.Models;

namespace Tasks.Controllers
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

                    //Response.Cookies.Append("Auth", user.Token);

                    var handler = new JwtSecurityTokenHandler();
                    var jwtSecurityToken = handler.ReadJwtToken(user.Token);


                    var claims = new List<Claim>()
                    {
                        new(ClaimTypes.NameIdentifier,
                            jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty),
                        new(ClaimTypes.Name,
                            jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId)?.Value ?? string.Empty)
                        //new Claim(ClaimTypes.Role, string.Join(" ") ?.Value ?? string.Empty),
                        //new Claim("FavoriteDrink", "Tea")
                    };

                    foreach (var claim in jwtSecurityToken.Claims.Where(c => c.Type == "role"))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                    }

                    //Initialize a new instance of the ClaimsIdentity with the claims and authentication scheme    
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    //Initialize a new instance of the ClaimsPrincipal with ClaimsIdentity    
                    var principal = new ClaimsPrincipal(identity);
                    //SignInAsync is a Extension method for Sign in a principal for the specified scheme.    
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