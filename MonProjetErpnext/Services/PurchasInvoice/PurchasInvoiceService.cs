using System.Text.Json;
using MonProjetErpnext.Models;
using MonProjetErpnext.Services.Login;
using MonProjetErpnext.Models.PurchaseInvoice;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace MonProjetErpnext.Services.PurchasInvoice
{
    public class PurchasInvoiceService : IPurchasInvoiceService
    {
        private readonly ILoginService _loginService;
        private readonly ILogger<PurchasInvoiceService> _logger;

        public PurchasInvoiceService(ILoginService loginService, ILogger<PurchasInvoiceService> logger)
        {
            _loginService = loginService;
            _logger = logger;
        }

        public async Task<List<PurchaseInvoice>> GetPurchaseInvoiceWithItems(string status = null)
        {
            var baseFields = new[] {
                "name", "posting_date", "due_date", "status", "total", "supplier", "supplier_name", 
                "currency", "grand_total", "outstanding_amount", "is_paid", "company", 
                "items.item_code", "items.item_name", "items.description", "items.rate", 
                "items.qty", "items.amount", "items.uom", "items.conversion_factor", 
                "items.expense_account", "items.cost_center"
            };

            var filters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(status))
            {
                filters["status"] = status;
            }

            var filterString = filters.Any() 
                ? $"&filters={JsonSerializer.Serialize(filters, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })}" 
                : string.Empty;

            var url = $"/api/resource/Purchase%20Invoice?fields=[\"{string.Join("\",\"", baseFields)}\"]{filterString}";

            try
            {
                var response = await _loginService.MakeAuthenticatedRequest(HttpMethod.Get, url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new BoolToIntConverter() }
                };

                var result = JsonSerializer.Deserialize<ErpListResponse<IntermediateInvoice>>(content, options);

                if (result?.Data == null || !result.Data.Any())
                {
                    _logger.LogInformation("No purchase invoices found with status: {Status}", status);
                    return new List<PurchaseInvoice>();
                }

                return result.Data
                    .GroupBy(i => i.name)
                    .Select(g => new PurchaseInvoice
                    {
                        Name = g.Key,
                        PostingDate = g.First().posting_date,
                        DueDate = g.First().due_date,
                        Status = g.First().status,
                        Total = g.First().total ?? 0,
                        GrandTotal = g.First().grand_total ?? 0,
                        OutstandingAmount = g.First().outstanding_amount ?? 0,
                        IsPaid = g.First().is_paid ?? false,
                        Supplier = g.First().supplier,
                        SupplierName = g.First().supplier_name,
                        Currency = g.First().currency,
                        Company = g.First().company,
                        Items = g.Where(item => item.item_code != null)
                            .Select(item => new PurchaseInvoiceItem
                            {
                                ItemCode = item.item_code,
                                ItemName = item.item_name,
                                Description = item.description,
                                Rate = item.rate,
                                Quantity = item.qty,
                                Amount = item.amount,
                                UnitOfMeasure = item.uom,
                                ConversionFactor = item.conversion_factor,
                                ExpenseAccount = item.expense_account,
                                CostCenter = item.cost_center
                            }).ToList()
                    }).ToList();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while retrieving purchase invoices with status: {Status}", status);
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Deserialization error for purchase invoices with status: {Status}.", status);
                throw;
            }
        }

        public async Task<byte[]> DownloadPurchaseInvoicePdf(string invoiceName)
        {
            var url = "/api/method/frappe.utils.print_format.download_pdf";

            var payload = new Dictionary<string, object>
            {
                { "doctype", "Purchase Invoice" },
                { "name", invoiceName },
                { "format", "Standard" },
                { "no_letterhead", 0 }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _loginService.MakeAuthenticatedRequest(HttpMethod.Post, url, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erreur lors de la récupération du PDF : {response.StatusCode} - {errorMsg}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }


        public Task<List<PurchaseInvoice>> GetPurchasInvoiceWithItems()
        {
            return GetPurchaseInvoiceWithItems(null);
        }

        private class IntermediateInvoice
        {
            public string name { get; set; }
            public string posting_date { get; set; }
            public string due_date { get; set; }
            public string status { get; set; }
            public decimal? total { get; set; }
            public decimal? grand_total { get; set; }
            public decimal? outstanding_amount { get; set; }
            public bool? is_paid { get; set; }
            public string supplier { get; set; }
            public string supplier_name { get; set; }
            public string currency { get; set; }
            public string company { get; set; }
            public string item_code { get; set; }
            public string item_name { get; set; }
            public string description { get; set; }
            public decimal rate { get; set; }
            public decimal qty { get; set; }
            public decimal amount { get; set; }
            public string uom { get; set; }
            public decimal conversion_factor { get; set; }
            public string expense_account { get; set; }
            public string cost_center { get; set; }
        }

        private class ErpListResponse<T>
        {
            public List<T> Data { get; set; }
        }

        public class BoolToIntConverter : JsonConverter<bool?>
        {
            public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.True:
                        return true;
                    case JsonTokenType.False:
                        return false;
                    case JsonTokenType.Number:
                        return reader.TryGetInt32(out int intValue) ? intValue == 1 : 
                               reader.TryGetDecimal(out decimal decimalValue) && decimalValue == 1;
                    case JsonTokenType.String:
                        return reader.GetString() switch
                        {
                            "1" => true,
                            "0" => false,
                            "true" => true,
                            "false" => false,
                            _ => null
                        };
                    default:
                        return null;
                }
            }

            public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                {
                    writer.WriteBooleanValue(value.Value);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
        }

        public class DecimalConverter : JsonConverter<decimal>
        {
            public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String && decimal.TryParse(reader.GetString(), out decimal value))
                {
                    return value;
                }
                return reader.GetDecimal();
            }

            public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }
    }
}