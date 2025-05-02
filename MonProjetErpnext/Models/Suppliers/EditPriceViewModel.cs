using System.ComponentModel.DataAnnotations;

namespace MonProjetErpnext.Models.Suppliers
{
    public class EditPriceViewModel
    {
        public string QuotationName { get; set; }

        public string SupplierName { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal Quantity { get; set; }
        
        [Display(Name = "Prix actuel")]
        [DisplayFormat(DataFormatString = "{0:N2} €")]
        public decimal CurrentPrice { get; set; }
        
        [Display(Name = "Nouveau prix")]
        [Required(ErrorMessage = "Le nouveau prix est requis")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le prix doit être supérieur à 0")]
        public decimal NewPrice { get; set; }
    }
}