using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels;

public class EditImpactTemplateViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public List<ParameterDefinitionViewModel> Parameters { get; set; } = new();
}
