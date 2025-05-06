using System.Collections.Generic;

namespace MonProjetErpnext.Models.Suppliers
{
    public class QuotationViewModel
    {
        public List<SupplierQuotation> Quotations { get; set; } = new List<SupplierQuotation>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalQuotations { get; set; } = 0;
        public string SupplierId { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty; // Ajouté pour affichage
    }
}