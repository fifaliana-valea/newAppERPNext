using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using MonProjetErpnext.Models.Import;
using MonProjetErpnext.Models.PurchaseInvoice;
using MonProjetErpnext.Models.PurchaseOrder;
using MonProjetErpnext.Services.Login;

namespace MonProjetErpnext.Services.Import
{
    public class ImportService : IImportService
    {
        private readonly ILoginService _loginService;
        private readonly ILogger<ImportService> _logger;

        public ImportService(ILoginService loginService, ILogger<ImportService> logger)
        {
            _loginService = loginService;
            _logger = logger;
        }

        public async Task<CompanySettings> GetDefaultCompanySettingsAsync()
        {
            try
            {
                // Récupère les paramètres de la société par défaut depuis ERPNext
                var response = await _loginService.MakeAuthenticatedRequest(
                    HttpMethod.Get, 
                    "/api/method/frappe.client.get_list?doctype=Company&fields=[%22name%22,%22default_currency%22]&limit=1");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var companies = JsonSerializer.Deserialize<List<Company>>(content);
                    
                    if (companies != null && companies.Any())
                    {
                        return new CompanySettings
                        {
                            Company = companies[0].Name,
                            Currency = companies[0].DefaultCurrency
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paramètres de société");
            }

            // Valeurs par défaut si la récupération échoue
            return new CompanySettings
            {
                Company = "Valea",
                Currency = "BAM"
            };
        }

        public class Company
        {
            public string Name { get; set; }
            public string DefaultCurrency { get; set; }
        }

        public class CompanySettings
        {
            public string Company { get; set; }
            public string Currency { get; set; }
        }

        public async Task<ImportResult> ImportFromCsvAsync(ImportRequest request)
        {
            var result = new ImportResult();
            
            try
            {
                using var stream = request.File.OpenReadStream();
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                
                // Enregistrements CSV temporaires
                if (request.DocumentType == "Purchase Invoice")
                {
                    csv.Context.RegisterClassMap<PurchaseInvoiceCsvMap>();
                    var records = csv.GetRecords<PurchaseInvoiceCsvRecord>().ToList();
                    return await ProcessPurchaseInvoicesAsync(records, request, result);
                }
                else if (request.DocumentType == "Purchase Order")
                {
                    csv.Context.RegisterClassMap<PurchaseOrderCsvMap>();
                    var records = csv.GetRecords<PurchaseOrderCsvRecord>().ToList();
                    return await ProcessPurchaseOrdersAsync(records, request, result);
                }
                else
                {
                    result.Errors.Add("Type de document non supporté");
                    result.Success = false;
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'import CSV");
                result.Errors.Add($"Erreur de lecture CSV: {ex.Message}");
                result.Success = false;
                return result;
            }
        }

        private async Task<ImportResult> ProcessPurchaseInvoicesAsync(
            List<PurchaseInvoiceCsvRecord> records, 
            ImportRequest request,
            ImportResult result)
        {
            foreach (var record in records)
            {
                try
                {
                    // Validation des données requises
                    if (string.IsNullOrEmpty(record.InvoiceNumber))
                    {
                        result.Errors.Add($"Ligne {record.RowNumber}: Numéro de facture manquant");
                        result.RecordsSkipped++;
                        continue;
                    }

                    // Conversion vers le modèle ERPNext
                    var invoice = new PurchaseInvoice
                    {
                        Name = record.InvoiceNumber,
                        PostingDate = record.PostingDate,
                        DueDate = record.DueDate,
                        Supplier = request.Supplier,
                        Company = request.Company,
                        Currency = request.Currency ?? "EUR",
                        Status = "Draft",
                        Items = new List<PurchaseInvoiceItem>()
                    };

                    // Ajout des articles
                    foreach (var item in record.Items)
                    {
                        invoice.Items.Add(new PurchaseInvoiceItem
                        {
                            ItemCode = item.ItemCode,
                            Quantity = item.Quantity,
                            Rate = item.Rate,
                            Amount = item.Quantity * item.Rate,
                            Description = item.Description
                        });
                    }

                    // Calcul des totaux
                    invoice.Total = invoice.Items.Sum(i => i.Amount);
                    invoice.GrandTotal = invoice.Total; // À adapter avec taxes si nécessaire

                    // Envoi à ERPNext
                    var response = await CreateErpNextDocument("Purchase Invoice", invoice);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        result.RecordsCreated++;
                        result.DocumentNames.Add(invoice.Name);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        result.Errors.Add($"Erreur pour {invoice.Name}: {error}");
                        result.RecordsSkipped++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur ligne {RowNumber}", record.RowNumber);
                    result.Errors.Add($"Ligne {record.RowNumber}: {ex.Message}");
                    result.RecordsSkipped++;
                }
            }

            result.RecordsProcessed = records.Count;
            result.Success = !result.Errors.Any();
            return result;
        }

        private async Task<ImportResult> ProcessPurchaseOrdersAsync(
            List<PurchaseOrderCsvRecord> records, 
            ImportRequest request,
            ImportResult result)
        {
            foreach (var record in records)
            {
                try
                {
                    if (string.IsNullOrEmpty(record.OrderNumber))
                    {
                        result.Errors.Add($"Ligne {record.RowNumber}: Numéro de commande manquant");
                        result.RecordsSkipped++;
                        continue;
                    }

                    var order = new Models.PurchaseOrder.PurchaseOrder
                    {
                        Name = record.OrderNumber,
                        TransactionDate = record.OrderDate,
                        Supplier = request.Supplier,
                        Company = request.Company,
                        Currency = request.Currency ?? "EUR",
                        Status = "Draft",
                        Items = new List<PurchaseOrderItem>()
                    };

                    foreach (var item in record.Items)
                    {
                        order.Items.Add(new PurchaseOrderItem
                        {
                            ItemCode = item.ItemCode,
                            Quantity = item.Quantity,
                            Rate = item.Rate,
                            Amount = item.Quantity * item.Rate,
                            ExpectedDeliveryDate = item.DeliveryDate
                        });
                    }

                    order.Total = order.Items.Sum(i => i.Amount);
                    order.GrandTotal = order.Total;

                    var response = await CreateErpNextDocument("Purchase Order", order);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        result.RecordsCreated++;
                        result.DocumentNames.Add(order.Name);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        result.Errors.Add($"Erreur pour {order.Name}: {error}");
                        result.RecordsSkipped++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur ligne {RowNumber}", record.RowNumber);
                    result.Errors.Add($"Ligne {record.RowNumber}: {ex.Message}");
                    result.RecordsSkipped++;
                }
            }

            result.RecordsProcessed = records.Count;
            result.Success = !result.Errors.Any();
            return result;
        }

        private async Task<HttpResponseMessage> CreateErpNextDocument(string doctype, object doc)
        {
            var endpoint = $"/api/resource/{doctype}";
            var jsonContent = JsonSerializer.Serialize(new { data = doc });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            return await _loginService.MakeAuthenticatedRequest(HttpMethod.Post, endpoint, content);
        }

        #region Classes pour la lecture CSV

        // Mappings pour CsvHelper
        public sealed class PurchaseInvoiceCsvMap : ClassMap<PurchaseInvoiceCsvRecord>
        {
            public PurchaseInvoiceCsvMap()
            {
                Map(m => m.InvoiceNumber).Name("InvoiceNumber", "Numéro");
                Map(m => m.PostingDate).Name("PostingDate", "DateFacture");
                Map(m => m.DueDate).Name("DueDate", "DateEcheance");
            }
        }

        public sealed class PurchaseOrderCsvMap : ClassMap<PurchaseOrderCsvRecord>
        {
            public PurchaseOrderCsvMap()
            {
                Map(m => m.OrderNumber).Name("OrderNumber", "Numéro");
                Map(m => m.OrderDate).Name("OrderDate", "DateCommande");
            }
        }

        #endregion
    }
}