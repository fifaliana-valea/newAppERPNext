using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonProjetErpnext.Services.PurchaseOrder
{
    public interface IPurchaseOrderService
    {
        Task<List<Models.PurchaseOrder.PurchaseOrder>> GetPurchaseOrdersWithItems(string supplier = null, string status = null);
    }
}