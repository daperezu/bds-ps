using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels;

public class SystemConfigurationViewModel
{
    public List<SystemConfigurationEntryViewModel> Configurations { get; set; } = new();
}

public class SystemConfigurationEntryViewModel
{
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    public string? Description { get; set; }
}
