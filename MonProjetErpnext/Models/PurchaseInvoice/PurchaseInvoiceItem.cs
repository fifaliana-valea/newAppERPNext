using System.ComponentModel.DataAnnotations;

namespace MonProjetErpnext.Models.PurchaseInvoice
{
    public class PurchaseInvoiceItem
    {
        [Required]
        public string ItemCode { get; set; }
        
        public string ItemName { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        public decimal Rate { get; set; }
        
        [Required]
        public decimal Quantity { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        public string UnitOfMeasure { get; set; }
        
        public decimal ConversionFactor { get; set; }
        
        public string ExpenseAccount { get; set; }
        
        public string CostCenter { get; set; }
    }
}