using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MonProjetErpnext.Models;
using MonProjetErpnext.Services.PurchasInvoice;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

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
    }
}