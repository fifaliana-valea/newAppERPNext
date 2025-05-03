namespace MonProjetErpnext.Models.PurchaseOrder
{
    public class PurchaseOrderItem
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public decimal Rate { get; set; }
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Warehouse { get; set; }
        public string ExpectedDeliveryDate { get; set; }
    }
}