using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MonProjetErpnext.Models;
using MonProjetErpnext.Services.PurchasInvoice;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MonProjetErpnext.Models.PurchaseInvoice;
using System.ComponentModel.DataAnnotations;

namespace MonProjetErpnext.Controllers.PurchaseInvoice
{
    [Authorize]
    public class PurchaseInvoiceController : Controller
    {
        private readonly ILogger<PurchaseInvoiceController> _logger;
        private readonly IPurchasInvoiceService _purchaseInvoiceService;
        private const int PageSize = 3; // Nombre de factures par page

        public PurchaseInvoiceController(
            IPurchasInvoiceService purchaseInvoiceService,
            ILogger<PurchaseInvoiceController> logger)
        {
            _purchaseInvoiceService = purchaseInvoiceService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, string status = null)
        {
            try
            {
                var allInvoices = await _purchaseInvoiceService.GetPurchaseInvoiceWithItems(status);
                
                var paginatedInvoices = allInvoices
                    .OrderByDescending(i => i.DueDate)
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = PageSize;
                ViewBag.TotalItems = allInvoices.Count;
                ViewBag.TotalPages = (int)Math.Ceiling(allInvoices.Count / (double)PageSize);
                ViewBag.StatusFilter = status;

                return View(paginatedInvoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading purchase invoices");
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = ex.Message 
                });
            }
        }

        [HttpGet]
        public IActionResult PayInvoice(string invoiceName, decimal amountDue)
        {
            try
            {
                if (string.IsNullOrEmpty(invoiceName))
                {
                    TempData["ErrorMessage"] = "Identifiant de facture manquant";
                    return RedirectToAction("Index");
                }

                // Générer la référence automatique pour espèces
                // var cashRef = $"ESP-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
                // ViewBag.AutoReference = cashRef;

                        // Générer la référence automatique basée sur le type de paiement
                // var paymentRef = GeneratePaymentReference();
                // ViewBag.AutoReference = paymentRef;

                var model = new PayInvoiceRequest
                {
                    InvoiceName = invoiceName,
                    Amount = amountDue,
                    PaymentDate = DateTime.Now,
                    ReferenceNumber = "" // Pré-remplissage initial
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment form for invoice {InvoiceName}", invoiceName);
                TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(PayInvoiceRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("PayInvoice", request);
                }

                var paymentInfo = new PaymentInfo
                {
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    ReferenceNumber = request.ReferenceNumber,
                    PaymentDate = request.PaymentDate
                };

                _logger.LogInformation("Payment Info => Amount: {Amount}, Method: {PaymentMethod}, Reference: {ReferenceNumber}, Date: {PaymentDate}", 
                    paymentInfo.Amount, 
                    paymentInfo.PaymentMethod, 
                    paymentInfo.ReferenceNumber, 
                    paymentInfo.PaymentDate);


                var result = await _purchaseInvoiceService.PayPurchaseInvoice(request.InvoiceName, paymentInfo);

                if (result)
                {
                    TempData["SuccessMessage"] = $"Paiement de {request.Amount}€ ({request.PaymentMethod}) enregistré";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Échec du traitement du paiement");
                return View("PayInvoice", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for invoice {InvoiceName}", request.InvoiceName);
                TempData["ErrorMessage"] = "Erreur critique lors du paiement";
                return View("PayInvoice", request);
            }
        }
        
        
        
        [HttpPost]
        public async Task<IActionResult> ValidateInvoice(string invoiceName)
        {
            try
            {
                if (string.IsNullOrEmpty(invoiceName))
                {
                    TempData["ErrorMessage"] = "Identifiant de facture manquant";
                    return RedirectToAction("Index");
                }

                var result = await _purchaseInvoiceService.ValidatePurchaseInvoice(invoiceName);
                
                if (result)
                {
                    TempData["SuccessMessage"] = $"Facture {invoiceName} validée avec succès";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Échec de la validation de la facture {invoiceName}";
                }
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating invoice {InvoiceName}", invoiceName);
                TempData["ErrorMessage"] = "Erreur lors de la validation";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPurchaseInvoicePdf(string invoiceName)
        {
            if (string.IsNullOrEmpty(invoiceName))
            {
                TempData["ErrorMessage"] = "Nom de facture invalide";
                return RedirectToAction("Index");
            }

            try
            {
                var pdfBytes = await _purchaseInvoiceService.DownloadPurchaseInvoicePdf(invoiceName);
                return File(pdfBytes, "application/pdf", $"Facture_{invoiceName}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading PDF for invoice {InvoiceName}", invoiceName);
                TempData["ErrorMessage"] = "Erreur lors du téléchargement";
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
        private string GeneratePaymentReference(string paymentMethod = "")
        {
            string prefix = paymentMethod switch
            {
                "Credit Card" => "CC",
                "Bank Transfer" => "VIR",
                "Check" => "CHQ",
                "Cash" => "ESP",
                "Direct Debit" => "PRE",
                _ => "PAY" // Valeur par défaut
            };
            
            return $"{prefix}-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
        }
    }


    public class PayInvoiceRequest
    {
        public string InvoiceName { get; set; }
        
        [Required(ErrorMessage = "Le montant est obligatoire")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être positif")]
        public decimal Amount { get; set; }
        
        [Required(ErrorMessage = "La méthode de paiement est obligatoire")]
        public string PaymentMethod { get; set; }
        
        [Required(ErrorMessage = "La référence est obligatoire")]
        public string ReferenceNumber { get; set; }
        
        [Required(ErrorMessage = "La date est obligatoire")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; }
    }
}