using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MonProjetErpnext.Models.PurchaseInvoice
{
    public class PaymentInfo
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}