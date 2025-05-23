using MonProjetErpnext.Models.Suppliers;
using MonProjetErpnext.Models.PurchaseInvoice;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonProjetErpnext.Services.PurchasInvoice
{
    public interface IPurchasInvoiceService
    {
        Task<List<PurchaseInvoice>> GetPurchaseInvoiceWithItems(string status = null);
         Task<byte[]> DownloadPurchaseInvoicePdf(string invoiceName);
         Task<bool> ValidatePurchaseInvoice(string invoiceName);
        Task<bool> PayPurchaseInvoice(string invoiceName, PaymentInfo paymentInfo);
    }
}