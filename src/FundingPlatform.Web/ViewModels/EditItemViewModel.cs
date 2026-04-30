using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FundingPlatform.Web.ViewModels;

public class EditItemViewModel
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }

    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [Display(Name = "Nombre del producto")]
    [MaxLength(500, ErrorMessage = "El nombre del producto debe tener máximo {1} caracteres.")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [Display(Name = "Categoría")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Las especificaciones técnicas son obligatorias.")]
    [Display(Name = "Especificaciones técnicas")]
    public string TechnicalSpecifications { get; set; } = string.Empty;

    public List<SelectListItem> Categories { get; set; } = new();
}
