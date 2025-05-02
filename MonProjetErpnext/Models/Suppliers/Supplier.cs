using System.Text.Json.Serialization;
namespace MonProjetErpnext.Models.Suppliers;
public class Supplier
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("supplier_name")]
    public string SupplierName { get; set; } = string.Empty;
}
