using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonProjetErpnext.Models;
using MonProjetErpnext.Models.Import;
using MonProjetErpnext.Services.Import;
using MonProjetErpnext.Services.Suppliers;
using System.Diagnostics;
using System.Text;

namespace MonProjetErpnext.Controllers.Import
{
    [Authorize]
    public class ImportController : Controller
    {
        private readonly IImportService _importService;
        private readonly ILogger<ImportController> _logger;

        private readonly ISupplierService _supplierService;

        public ImportController(
            IImportService importService,
            ISupplierService supplierService,
            ILogger<ImportController> logger)
        {
            _importService = importService;
            _logger = logger;
            _supplierService = supplierService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ImportViewModel
            {
                AvailableTypes = new List<string> { "Purchase Invoice", "Purchase Order" }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ImportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableTypes = new List<string> { "Purchase Invoice", "Purchase Order" };
                return View("Index", model);
            }

            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError("", "Veuillez sélectionner un fichier");
                return View("Index", model);
            }

            try
            {
                var request = new ImportRequest
                {
                    File = model.File,
                    DocumentType = model.DocumentType,
                    Supplier = model.Supplier,
                    Company = model.Company,
                    Currency = model.Currency
                };

                var result = await _importService.ImportFromCsvAsync(request);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Import réussi : {result.RecordsCreated} documents créés";
                    _logger.LogInformation("Import réussi : {Count} {Type} créés", 
                        result.RecordsCreated, model.DocumentType);
                }
                else
                {
                    TempData["ErrorMessage"] = "Erreurs lors de l'import : " + 
                        string.Join(", ", result.Errors.Take(3));
                    _logger.LogWarning("Import partiellement échoué : {Errors}", result.Errors);
                }

                return RedirectToAction("Results", new { 
                    success = result.Success,
                    recordsProcessed = result.RecordsProcessed,
                    recordsCreated = result.RecordsCreated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'import");
                TempData["ErrorMessage"] = $"Erreur technique : {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult Results(bool success, int recordsProcessed, int recordsCreated)
        {
            var model = new ImportResultViewModel
            {
                IsSuccess = success,
                RecordsProcessed = recordsProcessed,
                RecordsCreated = recordsCreated
            };

            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }

            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult DownloadTemplate(string type)
        {
            try
            {
                string templateContent, fileName;

                if (type == "Purchase Invoice")
                {
                    templateContent = "InvoiceNumber,PostingDate,DueDate,ItemCode,Description,Quantity,Rate\n" +
                                    "INV-001,2023-01-01,2023-02-01,ITEM-001,Produit A,10,25.50";
                    fileName = "template-factures.csv";
                }
                else if (type == "Purchase Order")
                {
                    templateContent = "OrderNumber,OrderDate,ItemCode,Quantity,Rate,DeliveryDate\n" +
                                    "PO-001,2023-01-01,ITEM-001,15,24.00,2023-01-15";
                    fileName = "template-commandes.csv";
                }
                else
                {
                    TempData["ErrorMessage"] = "Type de template non reconnu";
                    return RedirectToAction("Index");
                }

                return File(Encoding.UTF8.GetBytes(templateContent), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement du template");
                TempData["ErrorMessage"] = "Erreur lors de la génération du template";
                return RedirectToAction("Index");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }
}