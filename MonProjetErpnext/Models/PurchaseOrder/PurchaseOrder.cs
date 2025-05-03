using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MonProjetErpnext.Models.PurchaseOrder
{
    public class PurchaseOrder
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string TransactionDate { get; set; }
        
        public string Status { get; set; }
        
        [Required]
        public string Supplier { get; set; }
        
        public string SupplierName { get; set; }
        
        public string Currency { get; set; }
        
        public decimal Total { get; set; }
        
        public decimal GrandTotal { get; set; }
        
        public string Company { get; set; }
        
        public List<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }
}