using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace SS14.Admin.Controllers
{
    [Controller]
    [Route("/Login")]
    public class Login : Controller
    {
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
    }
}
