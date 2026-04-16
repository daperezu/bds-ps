using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FundingPlatform.Web.ViewModels;

public class AddSupplierViewModel
{
    public int ApplicationId { get; set; }
    public int ItemId { get; set; }

    [Required, Display(Name = "Supplier Legal ID"), MaxLength(50)]
    public string SupplierLegalId { get; set; } = string.Empty;

    [Required, Display(Name = "Supplier Name"), MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [Display(Name = "Contact Name"), MaxLength(200)]
    public string? ContactName { get; set; }

    [Display(Name = "Email"), EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [Display(Name = "Phone"), Phone, MaxLength(50)]
    public string? Phone { get; set; }

    [Display(Name = "Location"), MaxLength(500)]
    public string? Location { get; set; }

    [Display(Name = "Has Electronic Invoice")]
    public bool HasElectronicInvoice { get; set; }

    [Display(Name = "Shipping Details"), MaxLength(1000)]
    public string? ShippingDetails { get; set; }

    [Display(Name = "Warranty Info"), MaxLength(1000)]
    public string? WarrantyInfo { get; set; }

    [Display(Name = "Compliance Status"), MaxLength(200)]
    public string? ComplianceStatus { get; set; }

    [Required, Display(Name = "Price")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Required, Display(Name = "Valid Until")]
    public DateOnly ValidUntil { get; set; }

    [Required, Display(Name = "Quotation File")]
    public IFormFile? QuotationFile { get; set; }
}
