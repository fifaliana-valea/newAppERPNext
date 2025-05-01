using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MonProjetErpnext.Models.Request;
using MonProjetErpnext.Models.Response;

namespace MonProjetErpnext.Services.Login
{
    public class LoginService
    {
        private readonly HttpClient _httpClient;
        private readonly string _erpNextBaseUrl;

        public LoginService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _erpNextBaseUrl = configuration["ErpNext:BaseUrl"];
        }

        public async Task<AuthResponse?> LoginAsync(AuthRequest authRequest)
        {
            try 
            {
                var jsonContent = JsonSerializer.Serialize(authRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_erpNextBaseUrl}/api/method/login", content);

                response.EnsureSuccessStatusCode(); 

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AuthResponse>(responseContent) 
                    ?? throw new Exception("La réponse de l'API est nulle") ;
            }
            catch (Exception ex)
            {
                // Loguer l'erreurasd
                Console.WriteLine($"Erreur lors de la connexion: {ex.Message}");
                return null;
            }
        }
    }
}