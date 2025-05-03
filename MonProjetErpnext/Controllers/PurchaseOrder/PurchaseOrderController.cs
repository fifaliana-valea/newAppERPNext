using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using MonProjetErpnext.Services.PurchaseOrder;
using MonProjetErpnext.Services.Suppliers;

namespace MonProjetErpnext.Controllers.PurchaseOrder
{
    public class PurchaseOrderController : Controller
    {
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly ILogger<PurchaseOrderController> _logger;

        private readonly ISupplierService _supplierService;

        private const int DefaultPageSize = 10;

        public PurchaseOrderController(
            IPurchaseOrderService purchaseOrderService,
            ILogger<PurchaseOrderController> logger,
            ISupplierService SupplierService)
        {
            _purchaseOrderService = purchaseOrderService;
            _logger = logger;
            _supplierService = SupplierService;
        }

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

        public async Task<IActionResult> PurchaseOrders(string supplier = null, string status = null, int page = 1)
        {
            try
            {
                const int pageSize = 1; // 1 commande par page
                var allOrders = await _purchaseOrderService.GetPurchaseOrdersWithItems(supplier, status);
                
                var paginatedOrders = allOrders
                    .OrderByDescending(o => o.TransactionDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = allOrders.Count;
                ViewBag.TotalPages = (int)Math.Ceiling(allOrders.Count / (double)pageSize);
                ViewBag.SupplierFilter = supplier;
                ViewBag.StatusFilter = status;

                return View(paginatedOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading purchase orders");
                return View("Error");
            }
        }
    }
}