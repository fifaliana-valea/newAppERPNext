using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MonProjetErpnext.Models.Request;
using MonProjetErpnext.Models.Response;

namespace MonProjetErpnext.Services.Login
{
    public class LoginService : ILoginService
    {
        private readonly HttpClient _httpClient;
        private readonly string _erpNextBaseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LoginService> _logger;

        public LoginService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<LoginService> logger)
        {
            _erpNextBaseUrl = configuration["ErpNext:BaseUrl"] ?? 
                throw new ArgumentNullException("ErpNext:BaseUrl configuration is missing");
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_erpNextBaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<AuthResponse?> LoginAsync(AuthRequest authRequest)
        {
            try 
            {
                var jsonContent = JsonSerializer.Serialize(authRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var handler = new HttpClientHandler 
                {
                    UseCookies = true,
                    CookieContainer = new CookieContainer()
                };

                using var client = new HttpClient(handler) 
                {
                    BaseAddress = new Uri(_erpNextBaseUrl)
                };

                var response = await client.PostAsync("/api/method/login", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Login failed with status {StatusCode}: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Erreur HTTP: {response.StatusCode} - {errorContent}");
                }

                var cookies = handler.CookieContainer.GetCookies(new Uri(_erpNextBaseUrl));
                var sidCookie = cookies["sid"];

                if (sidCookie != null)
                {
                    _httpContextAccessor.HttpContext?.Response.Cookies.Append(
                        "erpnext_sid", 
                        sidCookie.Value,
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTimeOffset.Now.AddHours(8),
                            Path = "/"
                        });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent);

                if (authResponse?.Message == "Logged In")
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, authResponse.FullName),
                        new Claim("ERPNextSession", sidCookie?.Value ?? string.Empty),
                        new Claim("LastLogin", DateTime.UtcNow.ToString("o"))
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await _httpContextAccessor.HttpContext!.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                        });
                }

                return authResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dans LoginAsync pour l'utilisateur {Username}", authRequest.Usr);
                throw;
            }
        }

        public async Task<HttpResponseMessage> MakeAuthenticatedRequest(
            HttpMethod method, 
            string endpoint, 
            HttpContent? content = null)
        {
            try
            {
                var sidCookie = _httpContextAccessor.HttpContext?.Request.Cookies["erpnext_sid"];
                
                if (string.IsNullOrEmpty(sidCookie))
                {
                    _logger.LogWarning("Tentative de requête authentifiée sans session valide");
                    throw new UnauthorizedAccessException("Session invalide - Veuillez vous reconnecter");
                }

                using var request = new HttpRequestMessage(method, endpoint);
                
                request.Headers.Clear();
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Cookie", $"sid={sidCookie}");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                
                if (content != null)
                {
                    request.Content = content;
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }

                var response = await _httpClient.SendAsync(request);
                
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Session expirée lors de l'accès à {Endpoint}", endpoint);
                    throw new UnauthorizedAccessException("Session expirée");
                }

                _logger.LogDebug("Requête vers {Endpoint} - Statut: {StatusCode}", 
                    endpoint, response.StatusCode);
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dans MakeAuthenticatedRequest vers {Endpoint}", endpoint);
                throw;
            }
        }
    
    }
}