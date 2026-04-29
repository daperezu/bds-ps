using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FundingPlatform.Web.ViewModels;

public class UploadSignedAgreementViewModel
{
    [Required(ErrorMessage = "La versión generada es obligatoria.")]
    [Display(Name = "Versión generada")]
    public int GeneratedVersion { get; set; }

    [Required(ErrorMessage = "El archivo firmado es obligatorio.")]
    [Display(Name = "Archivo firmado")]
    public IFormFile File { get; set; } = default!;
}
