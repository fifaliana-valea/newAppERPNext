using Microsoft.AspNetCore.Mvc;
using MonProjetErpnext.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using MonProjetErpnext.Models.Suppliers;
using MonProjetErpnext.Services.Suppliers;
using MonProjetErpnext.Models.Utiles;
using MonProjetErpnext.Models;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;

namespace MonProjetErpnext.Controllers.Suppliers
{
    [Authorize]
    public class SuppliersController : Controller
    {
        private readonly ISupplierService _supplierService;
        private const int DefaultPageSize = 10;
        private const int QuotationsPageSize = 1; // Un devis par page

        private readonly ILogger<SuppliersController> _logger;

        private readonly IConfiguration _configuration;

        // Correction ici : suppression du underscore avant logger dans les paramètres
        public SuppliersController(
            ILogger<SuppliersController> logger,
            ISupplierService supplierService,
            IConfiguration configuration)
        {
            _supplierService = supplierService;
            _logger = logger;
            _configuration = configuration;
        }

        // Liste paginée des fournisseurs
        public async Task<IActionResult> Index(int page = 1, int pageSize = DefaultPageSize)
        {
            var allSuppliers = await _supplierService.GetSuppliers();
            
            var paginatedSuppliers = allSuppliers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = allSuppliers.Count;
            ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalItems / pageSize);

            return View(paginatedSuppliers);
        }

        // Détails d'un devis avec pagination (1 devis par page)
        public async Task<IActionResult> GetSupplierQuotations(string supplierId, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(supplierId))
            {
                return BadRequest("L'identifiant du fournisseur est requis");
            }

            try
            {
                var allQuotations = await _supplierService.GetSupplierQuotationsWithItems(supplierId);
                
                // Pagination - un seul devis par page
                var quotation = allQuotations
                    .OrderByDescending(q => q.TransactionDate)
                    .Skip(page - 1)
                    .FirstOrDefault();

                if (quotation == null)
                {
                    return NotFound("Aucun devis trouvé pour ce fournisseur");
                }

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = QuotationsPageSize;
                ViewBag.TotalItems = allQuotations.Count;
                ViewBag.TotalPages = allQuotations.Count; // 1 item = 1 page
                ViewBag.SupplierId = supplierId;
                ViewBag.ErpNextBaseUrl = _configuration["ErpNext:BaseUrl"];


                return View("Quotations", quotation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des devis");
                return View("Error", new ErrorViewModel { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = ex.Message 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuotationItemRate([FromBody] QuotationItemRateDto data)
        {
            if (string.IsNullOrWhiteSpace(data.NameItem))
            {
                return BadRequest("Le nom de l'article est requis");
            }

            try
            {
                var result = await _supplierService.UpdateQuotationItemRate(data.NameItem, data.NewRate, data.Quantity);
                if (result)
                {
                    return Ok(new { success = true, message = "Prix mis à jour avec succès" });
                }
                return StatusCode(500, new { success = false, message = "Échec de la mise à jour" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du prix");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public class QuotationItemRateDto
        {
            public string NameItem { get; set; }
            public decimal NewRate { get; set; }
            public decimal Quantity { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ValidateInvoice(string invoiceName, string supplierId)
        {
            try
            {
                if (string.IsNullOrEmpty(invoiceName))
                {
                    TempData["ErrorMessage"] = "Identifiant du devis manquant";
                    return RedirectToAction("Index");
                }

                var result = await _supplierService.ValidateSupplierQuotation(invoiceName);
                
                if (result)
                {
                    TempData["SuccessMessage"] = $"Devis {invoiceName} validé avec succès";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Échec de la validation du devis {invoiceName}";
                }
                
                // Modification ici pour utiliser Query String au lieu de route param
                return RedirectToAction("GetSupplierQuotations", "Suppliers", new { supplierId = supplierId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating supplier quotation {InvoiceName}", invoiceName);
                TempData["ErrorMessage"] = "Erreur technique lors de la validation";
                return RedirectToAction("Index");
            }
        }





    }
}