using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FundingPlatform.Web.ViewModels;

public class AddItemViewModel
{
    public int ApplicationId { get; set; }

    [Required, Display(Name = "Product Name"), MaxLength(500)]
    public string ProductName { get; set; } = string.Empty;

    [Required, Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required, Display(Name = "Technical Specifications")]
    public string TechnicalSpecifications { get; set; } = string.Empty;

    public List<SelectListItem> Categories { get; set; } = new();
}
