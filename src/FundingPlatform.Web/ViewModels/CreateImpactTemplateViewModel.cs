using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels;

public class CreateImpactTemplateViewModel
{
    [Required(ErrorMessage = "El nombre de la plantilla es obligatorio.")]
    [Display(Name = "Nombre")]
    [MaxLength(300, ErrorMessage = "El nombre debe tener máximo {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [MaxLength(1000, ErrorMessage = "La descripción debe tener máximo {1} caracteres.")]
    public string? Description { get; set; }

    public List<ParameterDefinitionViewModel> Parameters { get; set; } = new();
}

public class ParameterDefinitionViewModel
{
    [Required(ErrorMessage = "El nombre del parámetro es obligatorio.")]
    [Display(Name = "Nombre")]
    [MaxLength(200, ErrorMessage = "El nombre debe tener máximo {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La etiqueta a mostrar es obligatoria.")]
    [Display(Name = "Etiqueta")]
    [MaxLength(300, ErrorMessage = "La etiqueta debe tener máximo {1} caracteres.")]
    public string DisplayLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de dato es obligatorio.")]
    [Display(Name = "Tipo de dato")]
    public string DataType { get; set; } = "Text";

    [Display(Name = "Obligatorio")]
    public bool IsRequired { get; set; }

    [Display(Name = "Reglas de validación")]
    public string? ValidationRules { get; set; }

    [Display(Name = "Orden")]
    public int SortOrder { get; set; }
}
