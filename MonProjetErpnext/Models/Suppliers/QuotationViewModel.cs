// Models/Suppliers/QuotationViewModel.cs
using System.Collections.Generic;

namespace MonProjetErpnext.Models.Suppliers
{
    public class QuotationViewModel
    {
        public List<SupplierQuotation> Quotations { get; set; } = new List<SupplierQuotation>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalQuotations { get; set; }
        public string SupplierId { get; set; } // Ajouté pour la pagination
    }
}