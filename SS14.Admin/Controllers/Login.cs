using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SS14.Admin.Admins;

namespace SS14.Admin.Controllers
{
    [Controller]
    [Route("/Login")]
    public class Login : Controller
    {
        private readonly IConfiguration _configuration;

        public Login(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = "/"
            });
        }

        [Route("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return Redirect("/");
        }

        [Route("DevBypass")]
        public async Task<IActionResult> DevBypass()
        {
            // Check if development mode is enabled
            var isDevelopmentMode = _configuration.GetValue<bool>("DevelopmentMode", false);

            if (!isDevelopmentMode)
            {
                return Unauthorized("Development mode is not enabled");
            }

            // Check if request is from localhost
            var isLocalhost = HttpContext.Connection.RemoteIpAddress?.IsIPv4MappedToIPv6 == true
                ? HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString() == "127.0.0.1"
                : HttpContext.Connection.RemoteIpAddress?.ToString() == "127.0.0.1"
                  || HttpContext.Connection.RemoteIpAddress?.ToString() == "::1"
                  || HttpContext.Request.Host.Host == "localhost";

            if (!isLocalhost)
            {
                return Unauthorized("This endpoint is only available on localhost");
            }

            // Create claims for development admin user with all flags
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "DevAdmin"),
                new Claim("name", "DevAdmin"),
                new Claim(ClaimTypes.Role, "ADMIN")
            };

            // Add all admin flag roles
            foreach (AdminFlags flag in Enum.GetValues(typeof(AdminFlags)))
            {
                if (flag != AdminFlags.None)
                {
                    claims.Add(new Claim(ClaimTypes.Role, flag.ToString().ToUpper()));
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return Redirect("/");
        }
    }
}
