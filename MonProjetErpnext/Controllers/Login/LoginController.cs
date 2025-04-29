using Microsoft.AspNetCore.Mvc;

namespace MonProjetErpnext.Controllers.Login
{
    public class LoginController : Controller
    {

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string username, string password, bool remember)
        {
            if (string.IsNullOrEmpty(username))
            {
                ModelState.AddModelError("username", "Le nom d'utilisateur est requis");
            }

            if (string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("password", "Le mot de passe est requis");
            }

            if (ModelState.IsValid)
            {
                
                if (username == "admin" && password == "password")
                {
                    if (remember)
                    {
                        Response.Cookies.Append("RememberMe", "true", new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(30)
                        });
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Identifiants incorrects");
                }
            }

            return View();
        }
    }
}