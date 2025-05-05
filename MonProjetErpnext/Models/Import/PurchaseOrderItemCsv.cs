namespace MonProjetErpnext.Models.Import
{
    public class PurchaseOrderItemCsv
    {
        public string ItemCode { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public string DeliveryDate { get; set; }
    }
}