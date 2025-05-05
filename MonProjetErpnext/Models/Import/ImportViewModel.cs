using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MonProjetErpnext.Models.Import
{
    public class ImportViewModel
    {
        [Required(ErrorMessage = "Le type de document est requis")]
        [Display(Name = "Type de document")]
        public string DocumentType { get; set; }

        [Display(Name = "Fournisseur")]
        public string Supplier { get; set; }

        [Display(Name = "Société")]
        public string Company { get; set; }

        [Display(Name = "Devise")]
        public string Currency { get; set; } = "EUR";

        [Required(ErrorMessage = "Veuillez sélectionner un fichier")]
        [Display(Name = "Fichier CSV")]
        public IFormFile File { get; set; }

        public List<string> AvailableTypes { get; set; }
    }
}
