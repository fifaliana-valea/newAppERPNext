using System.Text.Json.Serialization;

namespace MonProjetErpnext.Models.Response
{
    public class AuthResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("home_page")]
        public string HomePage { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("api_key")]
        public string ApiKey { get; set; }

        [JsonIgnore] // Ne pas désérialiser directement
        public string ApiSecret { get; set; }

        public void ParseApiKey()
        {
            if (!string.IsNullOrEmpty(ApiKey))
            {
                var parts = ApiKey.Split(':');
                if (parts.Length == 2)
                {
                    ApiKey = parts[0];
                    ApiSecret = parts[1];
                }
            }
        }
    }
}