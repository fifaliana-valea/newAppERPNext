using System.Text.Json.Serialization;
namespace MonProjetErpnext.Models;
public class ErpListResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new List<T>();
}
