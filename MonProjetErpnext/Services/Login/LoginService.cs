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
    public class LoginService
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

<<<<<<< Updated upstream
                if (!response.IsSuccessStatusCode)
                    return null;

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return JsonSerializer.Deserialize<AuthResponse>(responseContent, options);
=======
                using var client = new HttpClient(handler) 
                {
                    BaseAddress = new Uri(_erpNextBaseUrl)
                };

                var response = await client.PostAsync("/api/method/login", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Erreur HTTP: {response.StatusCode} - {errorContent}");
                }

                // Récupérer le cookie sid
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
                            SameSite = SameSiteMode.Lax,
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
                        new Claim("ERPNextSession", sidCookie?.Value ?? string.Empty)
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await _httpContextAccessor.HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                        });
                }

                return authResponse;
>>>>>>> Stashed changes
            }
            catch
            {
<<<<<<< Updated upstream
                return null;
=======
                _logger.LogError(ex, "Erreur dans LoginAsync");
                throw;
>>>>>>> Stashed changes
            }
        }

        public async Task<HttpResponseMessage> MakeAuthenticatedRequest(HttpMethod method, string endpoint, HttpContent? content = null)
        {
            var sidCookie = _httpContextAccessor.HttpContext?.Request.Cookies["erpnext_sid"];
            
            if (string.IsNullOrEmpty(sidCookie))
            {
                throw new UnauthorizedAccessException("Session invalide");
            }

            var request = new HttpRequestMessage(method, endpoint);
            request.Headers.Add("Cookie", $"sid={sidCookie}");
            
            if (content != null)
            {
                request.Content = content;
            }

            return await _httpClient.SendAsync(request);
        }
    }
}