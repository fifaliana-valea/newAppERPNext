using System.Text.Json.Serialization;
namespace MonProjetErpnext.Models.Utiles;
public class ErpListResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new List<T>();
}
