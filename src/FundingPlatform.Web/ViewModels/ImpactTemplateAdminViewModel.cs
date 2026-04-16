namespace FundingPlatform.Web.ViewModels;

public class ImpactTemplateAdminViewModel
{
    public List<ImpactTemplateListItemViewModel> Templates { get; set; } = new();
}

public class ImpactTemplateListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ParameterCount { get; set; }
}
