using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MonProjetErpnext.Models.Request;
using MonProjetErpnext.Services.Login;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MonProjetErpnext.Controllers.Login
{
    public class LoginController : Controller
    {
        private readonly ILoginService _loginService;
        private readonly ILogger<LoginController> _logger;

        public LoginController(
            ILoginService loginService, 
            ILogger<LoginController> logger)
        {
            _loginService = loginService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(AuthRequest authRequest, string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View("Index", authRequest);

            try
            {
                var authResponse = await _loginService.LoginAsync(authRequest);

                if (authResponse == null || authResponse.Message != "Logged In")
                {
                    ModelState.AddModelError(string.Empty, "Identifiants invalides");
                    return View("Index", authRequest);
                }

                _logger.LogInformation("Utilisateur connecté: {FullName}", authResponse.FullName);
                HttpContext.Session.SetString("FullName", authResponse.FullName);
                
                return LocalRedirect(returnUrl ?? "/Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion");
                ModelState.AddModelError(string.Empty, "Erreur lors de la connexion");
                return View("Index", authRequest);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _loginService.MakeAuthenticatedRequest(HttpMethod.Get, "/api/method/logout");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion d'ERPNext");
            }
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            
            return RedirectToAction("Index");
        }
    }
}