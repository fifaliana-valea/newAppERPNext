namespace MonProjetErpnext.Models.Import
{
    public class PurchaseOrderCsvRecord
    {
        public int RowNumber { get; set; }
        public string OrderNumber { get; set; }
        public string OrderDate { get; set; }
        public List<PurchaseOrderItemCsv> Items { get; set; } = new();
    }
}