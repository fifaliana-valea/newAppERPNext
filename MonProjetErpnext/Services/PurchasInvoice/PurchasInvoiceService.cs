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

        public async Task<bool> ValidatePurchaseInvoice(string invoiceName)
        {
            try
            {
                var endpoint = $"/api/resource/Purchase Invoice/{invoiceName}";
                
                var payload = new
                {
                    docstatus = 1
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
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to validate invoice {InvoiceName}. Status: {StatusCode}, Error: {Error}", 
                        invoiceName, response.StatusCode, errorContent);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating purchase invoice {InvoiceName}", invoiceName);
                return false;
            }
        }

        public async Task<bool> PayPurchaseInvoice(string invoiceName, PaymentInfo paymentInfo)
        {
            try
            {
                // 1. Get invoice details
                var invoiceEndpoint = $"/api/resource/Purchase%20Invoice/{invoiceName}";
                var invoiceResponse = await _loginService.MakeAuthenticatedRequest(HttpMethod.Get, invoiceEndpoint);

                if (!invoiceResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get invoice details for {InvoiceName}. Status: {StatusCode}", 
                        invoiceName, invoiceResponse.StatusCode);
                    return false;
                }

                var invoiceContent = await invoiceResponse.Content.ReadAsStringAsync();
                _logger.LogDebug("Invoice response: {Response}", invoiceContent);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new DecimalConverter() }
                };

                // Parse invoice data manually for better error handling
                var invoiceJson = JsonDocument.Parse(invoiceContent);
                var invoiceData = invoiceJson.RootElement.GetProperty("data");

                // Get required fields with validation
                var supplier = invoiceData.GetProperty("supplier").GetString();
                var company = invoiceData.GetProperty("company").GetString();
                var grandTotal = invoiceData.GetProperty("grand_total").GetDecimal();
                var outstandingAmount = invoiceData.GetProperty("outstanding_amount").GetDecimal();
                var currency = invoiceData.GetProperty("currency").GetString();

                if (string.IsNullOrEmpty(supplier) || string.IsNullOrEmpty(company))
                {
                    _logger.LogError("Missing required invoice data for {InvoiceName}", invoiceName);
                    return false;
                }

                // 2. Get default accounts and payment methods
                var defaultAccounts = await GetDefaultAccounts(company);
                if (defaultAccounts == null)
                {
                    _logger.LogError("Failed to get default accounts for company {Company}", company);
                    return false;
                }

                // 3. Create payment entry with validated accounts
                var paymentEndpoint = "/api/resource/Payment%20Entry";
                
                var paymentPayload = new
                {
                    doctype = "Payment Entry",
                    docstatus = 1,
                    payment_type = "Pay",
                    party_type = "Supplier",
                    party = supplier,
                    company = company,
                    paid_amount = outstandingAmount,
                    received_amount = outstandingAmount,
                    source_exchange_rate = 1.0m,
                    paid_from = defaultAccounts.PaidFromAccount,
                    paid_from_account_currency = currency,
                    paid_to = defaultAccounts.PaidToAccount,
                    paid_to_account_currency = currency,
                    mode_of_payment = paymentInfo.PaymentMethod,
                    reference_no = paymentInfo.ReferenceNumber,
                    reference_date = paymentInfo.PaymentDate.ToString("yyyy-MM-dd"),
                    references = new[]
                    {
                        new
                        {
                            reference_doctype = "Purchase Invoice",
                            reference_name = invoiceName,
                            total_amount = grandTotal,
                            outstanding_amount = outstandingAmount,
                            allocated_amount = outstandingAmount
                        }
                    }
                };

                var paymentContent = new StringContent(
                    JsonSerializer.Serialize(paymentPayload, options),
                    Encoding.UTF8,
                    "application/json");

                var paymentResponse = await _loginService.MakeAuthenticatedRequest(
                    HttpMethod.Post, 
                    paymentEndpoint, 
                    paymentContent);

                if (!paymentResponse.IsSuccessStatusCode)
                {
                    var errorContent = await paymentResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create payment for invoice {InvoiceName}. Status: {StatusCode}, Error: {Error}", 
                        invoiceName, paymentResponse.StatusCode, errorContent);
                    return false;
                }

                // 3. Submit payment
                var paymentResponseContent = await paymentResponse.Content.ReadAsStringAsync();
                var paymentData = JsonSerializer.Deserialize<PaymentCreationResponse>(paymentResponseContent, options);

                if (paymentData?.Data == null || string.IsNullOrEmpty(paymentData.Data.name))
                {
                    _logger.LogError("Payment data is null or invalid for invoice {InvoiceName}", invoiceName);
                    return false;
                }

                var paymentName = paymentData.Data.name;

                var submitEndpoint = "/api/method/frappe.model.workflow.apply_workflow";
                
                var submitPayload = new
                {
                    doctype = "Payment Entry",
                    name = paymentName,
                    action = "Submit"
                };

                var submitContent = new StringContent(
                    JsonSerializer.Serialize(submitPayload, options),
                    Encoding.UTF8,
                    "application/json");

                var submitResponse = await _loginService.MakeAuthenticatedRequest(
                    HttpMethod.Post, 
                    submitEndpoint, 
                    submitContent);

                _logger.LogInformation("Status {ok}", submitResponse.ToString());

                return true;
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Missing expected field in invoice data for {InvoiceName}", invoiceName);
                return false;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error for invoice {InvoiceName}", invoiceName);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error paying purchase invoice {InvoiceName}", invoiceName);
                return false;
            }
        }

        private async Task<DefaultAccounts> GetDefaultAccounts(string company)
        {
            try
            {
                // Get default cash account
                var cashAccountResponse = await _loginService.MakeAuthenticatedRequest(
                    HttpMethod.Get,
                    $"/api/resource/Company/{company}?fields=[\"default_cash_account\"]");

                if (!cashAccountResponse.IsSuccessStatusCode)
                    return null;

                var cashAccountContent = await cashAccountResponse.Content.ReadAsStringAsync();
                var cashAccountJson = JsonDocument.Parse(cashAccountContent);
                var defaultCashAccount = cashAccountJson.RootElement
                    .GetProperty("data")
                    .GetProperty("default_cash_account")
                    .GetString();

                // Get default payable account
                var payableAccountResponse = await _loginService.MakeAuthenticatedRequest(
                    HttpMethod.Get,
                    $"/api/resource/Company/{company}?fields=[\"default_payable_account\"]");

                if (!payableAccountResponse.IsSuccessStatusCode)
                    return null;

                var payableAccountContent = await payableAccountResponse.Content.ReadAsStringAsync();
                var payableAccountJson = JsonDocument.Parse(payableAccountContent);
                var defaultPayableAccount = payableAccountJson.RootElement
                    .GetProperty("data")
                    .GetProperty("default_payable_account")
                    .GetString();

                return new DefaultAccounts
                {
                    PaidFromAccount = defaultCashAccount,
                    PaidToAccount = defaultPayableAccount
                };
            }
            catch
            {
                return null;
            }
        }

        private class DefaultAccounts
        {
            public string PaidFromAccount { get; set; }
            public string PaidToAccount { get; set; }
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

        private class PaymentCreationResponse
        {
            public PaymentData Data { get; set; }
        }

        private class PaymentData
        {
            public string name { get; set; }
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
                if (reader.TokenType == JsonTokenType.String)
                {
                    if (decimal.TryParse(reader.GetString(), out var result))
                        return result;
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    return reader.GetDecimal();
                }
                
                return 0m;
            }

            public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }
    }
}