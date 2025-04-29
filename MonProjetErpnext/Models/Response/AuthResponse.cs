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
    }
}