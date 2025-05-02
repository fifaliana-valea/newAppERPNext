using System.Net;
using System.Net.Http.Headers;
using MonProjetErpnext.Models.Request;
using MonProjetErpnext.Models.Response;

namespace MonProjetErpnext.Services.Login
{
    public interface ILoginService
    {
        /// <summary>
        /// Authentifie un utilisateur auprès d'ERPNext
        /// </summary>
        /// <param name="authRequest">Informations d'authentification</param>
        /// <returns>Réponse d'authentification</returns>
        Task<AuthResponse?> LoginAsync(AuthRequest authRequest);

        /// <summary>
        /// Effectue une requête authentifiée vers l'API ERPNext
        /// </summary>
        /// <param name="method">Méthode HTTP</param>
        /// <param name="endpoint">Endpoint API</param>
        /// <param name="content">Contenu de la requête (optionnel)</param>
        /// <returns>Réponse HTTP</returns>
        Task<HttpResponseMessage> MakeAuthenticatedRequest(
            HttpMethod method, 
            string endpoint, 
            HttpContent? content = null);
    }
}