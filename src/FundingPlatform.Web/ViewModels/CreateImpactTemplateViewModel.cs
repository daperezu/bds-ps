using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels;

public class CreateImpactTemplateViewModel
{
    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public List<ParameterDefinitionViewModel> Parameters { get; set; } = new();
}

public class ParameterDefinitionViewModel
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string DisplayLabel { get; set; } = string.Empty;

    [Required]
    public string DataType { get; set; } = "Text";

    public bool IsRequired { get; set; }

    public string? ValidationRules { get; set; }

    public int SortOrder { get; set; }
}
