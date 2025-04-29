using Microsoft.AspNetCore.Mvc;

namespace MonProjetErpnext.Controllers
{
    public class DashboardController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var fullName = HttpContext.Session.GetString("FullName");
            if (string.IsNullOrEmpty(fullName))
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.FullName = fullName;
            return View();
        }
    }
}