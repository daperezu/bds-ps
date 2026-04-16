using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FundingPlatform.Web.ViewModels;

public class AddQuotationViewModel
{
    public int ApplicationId { get; set; }
    public int ItemId { get; set; }
    public int SupplierId { get; set; }

    [Display(Name = "Supplier")]
    public string SupplierName { get; set; } = string.Empty;

    [Required, Display(Name = "Price")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Required, Display(Name = "Valid Until")]
    public DateOnly ValidUntil { get; set; }

    [Required, Display(Name = "Quotation File")]
    public IFormFile? QuotationFile { get; set; }
}
