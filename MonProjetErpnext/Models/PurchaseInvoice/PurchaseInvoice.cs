using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MonProjetErpnext.Models.PurchaseInvoice
{
    public class PurchaseInvoice
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string PostingDate { get; set; }
        
        public string DueDate { get; set; }
        
        public string Status { get; set; }
        
        [Required]
        public decimal Total { get; set; }
        
        [Required]
        public decimal GrandTotal { get; set; }
        
        public decimal OutstandingAmount { get; set; }
        
        public bool IsPaid { get; set; }
        
        [Required]
        public string Supplier { get; set; }
        
        public string SupplierName { get; set; }
        
        public string Currency { get; set; }
        
        public string Company { get; set; }

        public bool CanValidate => Status == "Draft";
        public bool CanPay => Status == "Unpaid" || Status == "Overdue";
        
        public List<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();
    }
}