namespace FundingPlatform.Web.ViewModels;

public class ApplicationListViewModel
{
    public List<ApplicationListItemViewModel> Applications { get; set; } = new();
}

public class ApplicationListItemViewModel
{
    public int Id { get; set; }
    public string State { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
