namespace MonProjetErpnext.Models.Import
{
    public class PurchaseInvoiceItemCsv
    {
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
    }
    
}