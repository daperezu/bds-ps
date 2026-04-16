namespace FundingPlatform.Web.ViewModels;

public class ApplicationViewModel
{
    public int Id { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public List<ItemViewModel> Items { get; set; } = new();
}

public class ItemViewModel
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int QuotationCount { get; set; }
    public bool HasImpact { get; set; }
    public string? ReviewComment { get; set; }
}
