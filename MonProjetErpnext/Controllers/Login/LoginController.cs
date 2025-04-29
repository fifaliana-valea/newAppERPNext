using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MonProjetErpnext.Models.Request;
using MonProjetErpnext.Services.Login;
using System.Threading.Tasks;

namespace MonProjetErpnext.Controllers.Login
{
    public class LoginController : Controller
    {
        private readonly LoginService _loginService;
        private readonly ILogger<LoginController> _logger;

        public LoginController(LoginService loginService, ILogger<LoginController> logger)
        {
            _loginService = loginService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(AuthRequest authRequest)
        {
            if (!ModelState.IsValid)
                return View("Index", authRequest);

            var authResponse = await _loginService.LoginAsync(authRequest);

            if (authResponse == null || authResponse.Message != "Logged In")
            {
                ModelState.AddModelError(string.Empty, "Identifiants invalides");
                return View("Index", authRequest);
            }

            HttpContext.Session.SetString("FullName", authResponse.FullName);
            HttpContext.Session.SetString("HomePage", authResponse.HomePage);
            
            _logger.LogInformation($"Utilisateur connecté: {authResponse.FullName}");
            
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}