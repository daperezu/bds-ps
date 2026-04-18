using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Web.ViewModels;

public class ItemResponseViewModel
{
    public int ItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public ItemReviewStatus ReviewStatus { get; set; }
    public string? SelectedSupplierName { get; set; }
    public decimal? Amount { get; set; }
    public string? ReviewComment { get; set; }
    public ItemResponseDecision? Decision { get; set; }
}
