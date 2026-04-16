namespace FundingPlatform.Web.ViewModels;

public class ImpactViewModel
{
    public int ApplicationId { get; set; }
    public int ItemId { get; set; }
    public string ItemProductName { get; set; } = string.Empty;
    public int? SelectedTemplateId { get; set; }
    public List<ImpactTemplateOptionViewModel> Templates { get; set; } = new();
    public List<ImpactParameterInputViewModel> Parameters { get; set; } = new();
}

public class ImpactTemplateOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ImpactParameterInputViewModel
{
    public int ParameterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;  // Text, Decimal, Integer, Date
    public bool IsRequired { get; set; }
    public string? Value { get; set; }
}
