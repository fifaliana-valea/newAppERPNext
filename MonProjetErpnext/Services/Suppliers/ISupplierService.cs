using MonProjetErpnext.Models.Suppliers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonProjetErpnext.Services.Suppliers
{
    public interface ISupplierService
    {
        Task<List<Supplier>> GetSuppliers();
        Task<List<SupplierQuotation>> GetSupplierQuotationsWithItems(string supplierId);
        Task<bool> UpdateQuotationItemRate(string itemName, decimal newRate, decimal quantity);
    }
}