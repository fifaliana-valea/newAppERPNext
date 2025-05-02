using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonProjetErpnext.Models.Suppliers
{
    public class SupplierQuotation
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("transaction_date")]
        public string TransactionDate { get; set; } = string.Empty;
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        
        [JsonPropertyName("total")]
        public decimal Total { get; set; }
        
        [JsonPropertyName("supplier")]
        public string Supplier { get; set; } = string.Empty;
        
        [JsonPropertyName("supplier_name")]
        public string SupplierName { get; set; } = string.Empty;
        
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
        
        public string PaymentTerms { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<QuotationItem> Items { get; set; } = new List<QuotationItem>();

        // Méthode utilitaire pour calculer le montant total à partir des items
        public decimal CalculateTotalAmount()
        {
            return Items.Sum(item => item.Amount);
        }

        // Méthode pour trouver un item par son code
        public QuotationItem? GetItemByCode(string itemCode)
        {
            return Items.FirstOrDefault(i => i.ItemCode == itemCode);
        }
    }
}