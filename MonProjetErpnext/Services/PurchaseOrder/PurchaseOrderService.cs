using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MonProjetErpnext.Models.PurchaseOrder;
using MonProjetErpnext.Services.Login;

namespace MonProjetErpnext.Services.PurchaseOrder
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly ILoginService _loginService;
        private readonly ILogger<PurchaseOrderService> _logger;

        public PurchaseOrderService(ILoginService loginService, ILogger<PurchaseOrderService> logger)
        {
            _loginService = loginService;
            _logger = logger;
        }

        public async Task<List<Models.PurchaseOrder.PurchaseOrder>> GetPurchaseOrdersWithItems(string supplier = null, string status = null)
        {
            var baseFields = new[] {
                "name", "transaction_date", "status", "supplier", "supplier_name", 
                "currency", "total", "grand_total", "company",
                "items.item_code", "items.item_name", "items.description", 
                "items.rate", "items.qty", "items.amount", "items.uom",
                "items.warehouse", "items.schedule_date"
            };

            var filters = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(supplier))
            {
                filters["supplier"] = supplier;
            }
            
            if (!string.IsNullOrEmpty(status))
            {
                filters["status"] = status;
            }

            var filterString = filters.Any() 
                ? $"&filters={JsonSerializer.Serialize(filters)}" 
                : string.Empty;

            var url = $"/api/resource/Purchase%20Order?fields=[\"{string.Join("\",\"", baseFields)}\"]{filterString}";

            try
            {
                var response = await _loginService.MakeAuthenticatedRequest(HttpMethod.Get, url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var result = JsonSerializer.Deserialize<ErpListResponse<IntermediateOrder>>(content, options);

                if (result?.Data == null || !result.Data.Any())
                {
                    _logger.LogInformation("No purchase orders found for supplier: {Supplier} with status: {Status}", supplier, status);
                    return new List<Models.PurchaseOrder.PurchaseOrder>();
                }

                return result.Data
                    .GroupBy(o => o.name)
                    .Select(g => new Models.PurchaseOrder.PurchaseOrder
                    {
                        Name = g.Key,
                        TransactionDate = g.First().transaction_date,
                        Status = g.First().status,
                        Supplier = g.First().supplier,
                        SupplierName = g.First().supplier_name,
                        Currency = g.First().currency,
                        Total = g.First().total,
                        GrandTotal = g.First().grand_total,
                        Company = g.First().company,
                        Items = g.Where(item => item.item_code != null)
                            .Select(item => new PurchaseOrderItem
                            {
                                ItemCode = item.item_code,
                                ItemName = item.item_name,
                                Description = item.description,
                                Rate = item.rate,
                                Quantity = item.qty,
                                Amount = item.amount,
                                UnitOfMeasure = item.uom,
                                Warehouse = item.warehouse,
                                ExpectedDeliveryDate = item.schedule_date
                            }).ToList()
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving purchase orders");
                throw;
            }
        }

        private class IntermediateOrder
        {
            public string name { get; set; }
            public string transaction_date { get; set; }
            public string status { get; set; }
            public string supplier { get; set; }
            public string supplier_name { get; set; }
            public string currency { get; set; }
            public decimal total { get; set; }
            public decimal grand_total { get; set; }
            public string company { get; set; }
            public string item_code { get; set; }
            public string item_name { get; set; }
            public string description { get; set; }
            public decimal rate { get; set; }
            public decimal qty { get; set; }
            public decimal amount { get; set; }
            public string uom { get; set; }
            public string warehouse { get; set; }
            public string schedule_date { get; set; }
        }

        private class ErpListResponse<T>
        {
            public List<T> Data { get; set; }
        }
    }
}