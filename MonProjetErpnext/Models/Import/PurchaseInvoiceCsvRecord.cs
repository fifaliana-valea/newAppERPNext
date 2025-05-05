namespace MonProjetErpnext.Models.Import
{
        public class PurchaseInvoiceCsvRecord
        {
            public int RowNumber { get; set; }
            public string InvoiceNumber { get; set; }
            public string PostingDate { get; set; }
            public string DueDate { get; set; }
            public List<PurchaseInvoiceItemCsv> Items { get; set; } = new();
        }
}