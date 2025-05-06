using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;

namespace MonProjetErpnext.Middlewares
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly List<string> _allowedRoutes = new List<string>
        {
            "/Login/Index",
            "/Login/Login",
            "/api/method/login",
            "/Home/Error",
            "/favicon.ico",
            "/css",
            "/js",
            "/lib"
        };

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path;
            
            // Vérifier si la route est autorisée
            if (_allowedRoutes.Any(r => path.StartsWithSegments(r, StringComparison.OrdinalIgnoreCase)) ||
                path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Vérifier la présence du cookie erpnext_sid
            if (!context.Request.Cookies.ContainsKey("erpnext_sid"))
            {
                // Stocker l'URL originale pour redirection après login
                context.Session.SetString("ReturnUrl", context.Request.Path);
                context.Response.Redirect("/Login/Index");
                return;
            }

            await _next(context);
        }
    }
}