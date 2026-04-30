using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FundingPlatform.Web.ViewModels;

public class AddQuotationViewModel
{
    public int ApplicationId { get; set; }
    public int ItemId { get; set; }
    public int SupplierId { get; set; }

    [Display(Name = "Proveedor")]
    public string SupplierName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Display(Name = "Precio")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a cero.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "La moneda es obligatoria.")]
    [Display(Name = "Moneda")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "La moneda debe ser un código de 3 caracteres.")]
    public string Currency { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de vigencia es obligatoria.")]
    [Display(Name = "Vigente hasta")]
    public DateOnly ValidUntil { get; set; }

    [Required(ErrorMessage = "El archivo de la cotización es obligatorio.")]
    [Display(Name = "Archivo de la cotización")]
    public IFormFile? QuotationFile { get; set; }
}
