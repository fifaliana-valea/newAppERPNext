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

namespace MonProjetErpnext.Controllers.PurchaseInvoice
{
    [Authorize]
    public class PurchaseInvoiceController : Controller
    {
        private readonly ILogger<PurchaseInvoiceController> _logger;
        private readonly IPurchasInvoiceService _purchaseInvoiceService;
        private const int PageSize = 2; // 2 factures par page

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

        public async Task<IActionResult> DownloadInvoicePdf([FromBody]  MonProjetErpnext.Models.PurchaseInvoice.PurchaseInvoice invoice)
        {

            if (invoice.Name == null)
                return NotFound("name non trouvée");

            var pdfBytes = new PdfGenerator().GeneratePurchaseInvoicePdf(invoice);
            return File(pdfBytes, "application/pdf", $"Facture_{invoice.Name}.pdf");
        }

        public async Task<IActionResult> DownloadPurchaseInvoicePdf([FromBody] string invoiceName)
        {
            if (string.IsNullOrEmpty(invoiceName))
            {
                return BadRequest("Le nom de la facture est requis.");
            }

            try
            {
                // Appel à la fonction de service pour récupérer le PDF
                var pdfBytes = await _purchaseInvoiceService.DownloadPurchaseInvoicePdf(invoiceName);

                // Retourne le fichier PDF au client
                return File(pdfBytes, "application/pdf", $"Facture_{invoiceName}.pdf");
            }
            catch (Exception ex)
            {
                // Gestion des erreurs
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


    }
}