using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MonProjetErpnext.Models.Request;
using MonProjetErpnext.Models.Response;
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

            // Parse l'API key pour séparer key et secret
            authResponse.ParseApiKey();

            // Stockage en session
            HttpContext.Session.SetString("FullName", authResponse.FullName);
            HttpContext.Session.SetString("ApiKey", authResponse.ApiKey);
            HttpContext.Session.SetString("ApiSecret", authResponse.ApiSecret);

            _logger.LogInformation($"Utilisateur connecté: {authResponse.FullName}");
            _logger.LogInformation($"ApiKey: {authResponse.ApiKey}");
            _logger.LogInformation($"ApiSecret: {authResponse.ApiSecret}");
            
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