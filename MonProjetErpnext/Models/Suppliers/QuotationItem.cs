using System.Text.Json.Serialization;

namespace MonProjetErpnext.Models.Suppliers
{
    public class QuotationItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("item_code")]
        public string ItemCode { get; set; } = string.Empty;
        
        [JsonPropertyName("item_name")]
        public string ItemName { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }  // Prix unitaire actuel (modifiable)
        
        [JsonPropertyName("price_list_rate")]
        public decimal? PriceListRate { get; set; }  // Prix catalogue de référence
        
        [JsonIgnore] // Ne pas sérialiser cette propriété pour l'API
        public decimal BaseRate { get; set; }  // Prix unitaire original
        
        [JsonPropertyName("qty")]
        public decimal Quantity { get; set; }
        
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }  // = Rate * Quantity
        
        [JsonPropertyName("uom")]
        public string UnitOfMeasure { get; set; } = string.Empty;
        
        [JsonPropertyName("conversion_factor")]
        public decimal ConversionFactor { get; set; } = 1;
        
        // Méthode pour mettre à jour le prix unitaire
        public void UpdateRate(decimal newRate)
        {
            BaseRate = Rate;  // Conserve l'ancien prix
            Rate = newRate;
            Amount = Rate * Quantity;
        }

        // Méthode pour vérifier si le prix a été modifié
        public bool IsPriceModified => Rate != BaseRate;
    }
}