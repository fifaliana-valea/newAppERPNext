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

namespace MonProjetErpnext.Controllers.Suppliers
{
    [Authorize]
    public class SuppliersController : Controller
    {
        private readonly ISupplierService _supplierService;
        private const int DefaultPageSize = 10;
        private const int QuotationsPageSize = 1; // Un devis par page

        public SuppliersController(ISupplierService SupplierService)
        {
            _supplierService = SupplierService;
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

                return View("Quotations", quotation);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = ex.Message 
                });
            }
        }

        // GET: Page de modification du prix
        public IActionResult EditItemPrice(string supplierName,string quotationName, string itemCode, decimal currentPrice, decimal quantity, string itemName, string unitOfMeasure)
        {
            var model = new EditPriceViewModel
            {
                SupplierName = supplierName,
                QuotationName = quotationName,
                ItemCode = itemCode,
                ItemName = itemName,
                UnitOfMeasure = unitOfMeasure,
                Quantity = quantity,
                CurrentPrice = currentPrice,
                NewPrice = currentPrice // Initialiser avec le prix actuel
            };

            return View(model);
        }

        // POST: Enregistrement de la modification
        [HttpPost]
        public async Task<IActionResult> EditItemPrice(EditPriceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _supplierService.UpdateQuotationItemRate(
                    model.QuotationName,
                    model.ItemCode,
                    model.NewPrice,
                    model.Quantity);

                if (result)
                {
                    TempData["SuccessMessage"] = "Le prix a été mis à jour avec succès";
                    return RedirectToAction("QuotationDetails", new { id = model.QuotationName });
                }
                
                ModelState.AddModelError("", "Échec de la mise à jour du prix");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erreur: {ex.Message}");
                return View(model);
            }
        }
    }
}