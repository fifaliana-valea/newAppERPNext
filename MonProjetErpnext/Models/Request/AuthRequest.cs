namespace MonProjetErpnext.Models.Request
{
    using System.Text.Json.Serialization;

    public class AuthRequest
    {
        [JsonPropertyName("usr")]
        public string Usr { get; set; }

        [JsonPropertyName("pwd")]
        public string Pwd { get; set; }
    }
}