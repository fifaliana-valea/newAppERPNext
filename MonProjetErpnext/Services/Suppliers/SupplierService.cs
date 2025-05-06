using System.Text.Json;
using MonProjetErpnext.Models;
using MonProjetErpnext.Services.Login;
using MonProjetErpnext.Models.Suppliers;
using System.Text;
using System.Text.Json.Serialization;
using MonProjetErpnext.Models.Utiles;

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

        public async Task<bool> ValidateSupplierQuotation(string quotationName)
        {
            try
            {
                var endpoint = $"/api/resource/Supplier%20Quotation/{quotationName}";
                
                var payload = new
                {
                    docstatus = 1, // 1 = Submitted
                    status = "Submitted"
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _loginService.MakeAuthenticatedRequest(
                    HttpMethod.Put, 
                    endpoint, 
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erreur API: {StatusCode} - {Error}", response.StatusCode, error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation du devis");
                return false;
            }
        }

        public async Task<List<SupplierQuotation>> GetSupplierQuotationsWithItems(string supplierId)
        {
            if (string.IsNullOrWhiteSpace(supplierId))
            {
                throw new ArgumentNullException(nameof(supplierId));
            }

            var encodedSupplierId = Uri.EscapeDataString(supplierId);
            var fields = new[]
            {
                "name as quotation_name",  // Alias clair pour le nom du document
                "transaction_date", "status", "total",
                "supplier", "supplier_name", "currency",
                "items.name as item_name_id",  // Alias pour le name crypté de l'item
                "items.item_code", "items.item_name",
                "items.description", "items.rate", "items.qty",
                "items.amount", "items.uom", "items.price_list_rate",
                "items.conversion_factor"
            };

            var url = $"/api/resource/Supplier%20Quotation?fields=[\"{string.Join("\",\"", fields)}\"]" +
                    $"&filters=[[\"supplier\",\"=\",\"{encodedSupplierId}\"]]";

            try
            {
                var response = await _loginService.MakeAuthenticatedRequest(HttpMethod.Get, url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Réponse API: {Content}", content);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Désérialisation directe dans une structure plate
                var flatResults = JsonSerializer.Deserialize<ErpListResponse<FlatQuotation>>(content, options);

                if (flatResults?.Data == null || !flatResults.Data.Any())
                {
                    return new List<SupplierQuotation>();
                }

                // Transformation manuelle des données plates en structure imbriquée
                var quotations = flatResults.Data
                    .GroupBy(f => f.QuotationName)
                    .Select(g => new SupplierQuotation
                    {
                        Name = g.Key,  // Nom de la quotation (ex: "PUR-SQTN-2025-00003")
                        TransactionDate = g.First().TransactionDate,
                        Status = g.First().Status,
                        Total = g.First().Total ?? 0,
                        Supplier = g.First().Supplier,
                        SupplierName = g.First().SupplierName,
                        Currency = g.First().Currency,
                        Items = g.Select(i => new QuotationItem
                        {
                            Name = i.ItemNameId,  // Nom crypté de l'item (ex: "61vhn1t30f")
                            ItemCode = i.ItemCode,
                            ItemName = i.ItemName,
                            Description = i.Description,
                            Rate = i.Rate,
                            PriceListRate = i.PriceListRate ?? i.Rate,
                            BaseRate = i.Rate,
                            Quantity = i.Qty,
                            Amount = i.Amount,
                            UnitOfMeasure = i.Uom,
                            ConversionFactor = i.ConversionFactor
                        }).ToList()
                    }).ToList();

                return quotations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des soumissions");
                return new List<SupplierQuotation>();
            }
        }
        // Classes pour la désérialisation des données plates
        public class FlatQuotation
        {
            [JsonPropertyName("quotation_name")]
            public string QuotationName { get; set; }  // Anciennement "name" dans la réponse
            [JsonPropertyName("transaction_date")]
            public string TransactionDate { get; set; }
            [JsonPropertyName("status")]
            public string Status { get; set; }
            [JsonPropertyName("total")]
            public decimal? Total { get; set; }
            [JsonPropertyName("supplier")]
            public string Supplier { get; set; }
            [JsonPropertyName("supplier_name")]
            public string SupplierName { get; set; }
            [JsonPropertyName("currency")]
            public string Currency { get; set; }
            
            // Champs des items
            [JsonPropertyName("item_name_id")]
            public string ItemNameId { get; set; }  // "name" de l'item
             [JsonPropertyName("item_code")]
            public string ItemCode { get; set; }
            [JsonPropertyName("item_name")]
            public string ItemName { get; set; }
            [JsonPropertyName("description")]
            public string Description { get; set; }
            [JsonPropertyName("rate")]
            public decimal Rate { get; set; }
            [JsonPropertyName("qty")]
            public decimal Qty { get; set; }
            [JsonPropertyName("amount")]
            public decimal Amount { get; set; }
            [JsonPropertyName("uom")]
            public string Uom { get; set; }
            [JsonPropertyName("price_list_rate")]
            public decimal? PriceListRate { get; set; }
            [JsonPropertyName("conversion_factor")]
            public decimal ConversionFactor { get; set; } = 1;
        }

        public async Task<bool> UpdateQuotationItemRate(string nameItem, decimal newRate, decimal quantity)
        {
            if (string.IsNullOrWhiteSpace(nameItem))
            {
                throw new ArgumentNullException(nameof(nameItem));
            }

            try
            {
                _logger.LogInformation("Prix mis à jour avec succès pour {NameItem}", nameItem);
                var payload = new
                {
                    rate = Math.Round(newRate, 4),  // 4 décimales
                    qty = Math.Round(quantity, 4)   // 4 décimales
                };

                var url = $"/api/resource/Supplier%20Quotation%20Item/{nameItem}";

                var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogDebug("Envoi à ERPNext - URL: {URL}, Payload: {Payload}", url, jsonPayload);

                var response = await _loginService.MakeAuthenticatedRequest(HttpMethod.Put, url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erreur ERPNext: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    throw new Exception($"Erreur ERPNext: {errorContent}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de {NameItem}", nameItem);
                throw;
            }
        }
    }
}