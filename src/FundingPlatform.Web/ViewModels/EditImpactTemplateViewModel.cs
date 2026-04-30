using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels;

public class EditImpactTemplateViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la plantilla es obligatorio.")]
    [Display(Name = "Nombre")]
    [MaxLength(300, ErrorMessage = "El nombre debe tener máximo {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [MaxLength(1000, ErrorMessage = "La descripción debe tener máximo {1} caracteres.")]
    public string? Description { get; set; }

    [Display(Name = "Activa")]
    public bool IsActive { get; set; }

    public List<ParameterDefinitionViewModel> Parameters { get; set; } = new();
}
