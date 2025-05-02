using System.Text.Json;
using MonProjetErpnext.Models;
using MonProjetErpnext.Services.Login;
using MonProjetErpnext.Models.Utiles;
using MonProjetErpnext.Models.Suppliers;
using System.Text;

namespace MonProjetErpnext.Services.Suppliers
{
    public class SupplierService : ISupplierService
    {
        private readonly ILoginService _loginService;
        private readonly ILogger<SupplierService> _logger;

        public SupplierService(ILoginService loginService, ILogger<SupplierService> logger)
        {
            _loginService = loginService;
            _logger = logger;
        }

        public async Task<List<Supplier>> GetSuppliers()
        {
            try
            {
                var response = await _loginService.MakeAuthenticatedRequest(
                    HttpMethod.Get, 
                    "/api/resource/Supplier?fields=[\"name\",\"supplier_name\"]");
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = JsonSerializer.Deserialize<ErpListResponse<Supplier>>(content, options);
                
                return result?.Data ?? new List<Supplier>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des fournisseurs");
                return new List<Supplier>();
            }
        }

        public async Task<List<SupplierQuotation>> GetSupplierQuotationsWithItems(string supplierId)
        {
            if (string.IsNullOrWhiteSpace(supplierId))
            {
                throw new ArgumentNullException(nameof(supplierId));
            }

            var encodedSupplierId = Uri.EscapeDataString(supplierId);
            var baseFields = new[] { 
                "name", "transaction_date", "status", "total", 
                "supplier", "supplier_name", "currency"
            };
            
            var itemFields = new[] { 
                "item_code", "item_name", "description", "rate", 
                "qty", "amount", "uom", "price_list_rate",
                "conversion_factor"
            };
            
            var url = $"/api/resource/Supplier%20Quotation?fields=[\"{string.Join("\",\"", baseFields)}\",\"items.{string.Join("\",\"items.", itemFields)}\"]" +
                    $"&filters=[[\"supplier\",\"=\",\"{encodedSupplierId}\"]]";
            
            try
            {
                var response = await _loginService.MakeAuthenticatedRequest(HttpMethod.Get, url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var result = JsonSerializer.Deserialize<ErpListResponse<IntermediateQuotation>>(content, options);
                
                if (result?.Data == null || !result.Data.Any())
                {
                    _logger.LogInformation("Aucune soumission trouvée pour le fournisseur {SupplierId}", supplierId);
                    return new List<SupplierQuotation>();
                }

                var quotations = result.Data
                    .GroupBy(q => q.name)
                    .Select(g => new SupplierQuotation 
                    {
                        Name = g.Key,
                        TransactionDate = g.First().transaction_date,
                        Status = g.First().status,
                        Total = g.First().total ?? 0,
                        Supplier = g.First().supplier,
                        SupplierName = g.First().supplier_name,
                        Currency = g.First().currency,
                        Items = g.Select(item => new QuotationItem 
                        {
                            ItemCode = item.item_code,
                            ItemName = item.item_name,
                            Description = item.description,
                            Rate = item.rate,
                            PriceListRate = item.price_list_rate ?? item.rate,
                            BaseRate = item.rate,
                            Quantity = item.qty,
                            Amount = item.amount,
                            UnitOfMeasure = item.uom,
                            ConversionFactor = item.conversion_factor
                        }).ToList()
                    }).ToList();

                return quotations;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erreur HTTP lors de la récupération des soumissions pour {SupplierId}", supplierId);
                return new List<SupplierQuotation>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erreur de désérialisation pour les soumissions de {SupplierId}", supplierId);
                return new List<SupplierQuotation>();
            }
        }

        public async Task<bool> UpdateQuotationItemRate(string quotationName, string itemCode, decimal newRate, decimal quantity)
        {
            try
            {
                var payload = new
                {
                    items = new[]
                    {
                        new
                        {
                            item_code = itemCode,
                            rate = newRate,
                            amount = newRate * quantity
                        }
                    }
                };

                var url = $"/api/resource/Supplier%20Quotation/{Uri.EscapeDataString(quotationName)}";
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                
                var response = await _loginService.MakeAuthenticatedRequest(HttpMethod.Put, url, content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Prix mis à jour pour l'item {ItemCode} dans la soumission {QuotationName}", itemCode, quotationName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du prix pour {ItemCode} dans {QuotationName}", itemCode, quotationName);
                throw;
            }
        }

        private class IntermediateQuotation
        {
            public string name { get; set; } = string.Empty;
            public string transaction_date { get; set; } = string.Empty;
            public string status { get; set; } = string.Empty;
            public decimal? total { get; set; }
            public string supplier { get; set; } = string.Empty;
            public string supplier_name { get; set; } = string.Empty;
            public string currency { get; set; } = string.Empty;
            public string item_code { get; set; } = string.Empty;
            public string item_name { get; set; } = string.Empty;
            public string description { get; set; } = string.Empty;
            public decimal rate { get; set; }
            public decimal? price_list_rate { get; set; }
            public decimal qty { get; set; }
            public decimal amount { get; set; }
            public string uom { get; set; } = string.Empty;
            public decimal conversion_factor { get; set; } = 1;
        }
    
    
    }
}