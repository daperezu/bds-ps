using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels;

public class SystemConfigurationViewModel
{
    public List<SystemConfigurationEntryViewModel> Configurations { get; set; } = new();
}

public class SystemConfigurationEntryViewModel
{
    public int Id { get; set; }

    [Display(Name = "Clave")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "El valor es obligatorio.")]
    [Display(Name = "Valor")]
    public string Value { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    public string? Description { get; set; }
}
