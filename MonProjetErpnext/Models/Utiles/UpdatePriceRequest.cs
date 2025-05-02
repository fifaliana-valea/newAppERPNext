using System.Text.Json.Serialization;
namespace MonProjetErpnext.Models.Utiles;

public class UpdatePriceRequest
{
    public string QuotationName { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public decimal NewPrice { get; set; }
    public decimal Quantity { get; set; }
}