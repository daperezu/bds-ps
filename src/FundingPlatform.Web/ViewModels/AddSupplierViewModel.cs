using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FundingPlatform.Web.ViewModels;

public class AddSupplierViewModel
{
    public int ApplicationId { get; set; }
    public int ItemId { get; set; }

    [Required(ErrorMessage = "La cédula jurídica del proveedor es obligatoria.")]
    [Display(Name = "Cédula jurídica del proveedor")]
    [MaxLength(50, ErrorMessage = "La cédula jurídica debe tener máximo {1} caracteres.")]
    public string SupplierLegalId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [Display(Name = "Razón social del proveedor")]
    [MaxLength(200, ErrorMessage = "El nombre del proveedor debe tener máximo {1} caracteres.")]
    public string SupplierName { get; set; } = string.Empty;

    [Display(Name = "Persona de contacto")]
    [MaxLength(200, ErrorMessage = "El nombre de contacto debe tener máximo {1} caracteres.")]
    public string? ContactName { get; set; }

    [Display(Name = "Correo electrónico")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [MaxLength(200, ErrorMessage = "El correo electrónico debe tener máximo {1} caracteres.")]
    public string? Email { get; set; }

    [Display(Name = "Teléfono")]
    [Phone(ErrorMessage = "El teléfono no es válido.")]
    [MaxLength(50, ErrorMessage = "El teléfono debe tener máximo {1} caracteres.")]
    public string? Phone { get; set; }

    [Display(Name = "Ubicación")]
    [MaxLength(500, ErrorMessage = "La ubicación debe tener máximo {1} caracteres.")]
    public string? Location { get; set; }

    [Display(Name = "Emite factura electrónica")]
    public bool HasElectronicInvoice { get; set; }

    [Display(Name = "Detalles de envío")]
    [MaxLength(1000, ErrorMessage = "Los detalles de envío deben tener máximo {1} caracteres.")]
    public string? ShippingDetails { get; set; }

    [Display(Name = "Información de garantía")]
    [MaxLength(1000, ErrorMessage = "La información de garantía debe tener máximo {1} caracteres.")]
    public string? WarrantyInfo { get; set; }

    [Display(Name = "Al día con la CCSS")]
    public bool IsCompliantCCSS { get; set; }

    [Display(Name = "Al día con Hacienda")]
    public bool IsCompliantHacienda { get; set; }

    [Display(Name = "Inscrito en SICOP")]
    public bool IsCompliantSICOP { get; set; }

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
