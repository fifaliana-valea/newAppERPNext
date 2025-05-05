using System.ComponentModel.DataAnnotations;

namespace MonProjetErpnext.Models.Import
{
    public class ImportRequest
    {
        [Required]
        public IFormFile File { get; set; }
        
        [Required]
        public string DocumentType { get; set; } // "Purchase Invoice" ou "Purchase Order"
        
        public string Supplier { get; set; }
        public string Company { get; set; }
        public string Currency { get; set; }
        public bool OverwriteExisting { get; set; } = false;
    }
}