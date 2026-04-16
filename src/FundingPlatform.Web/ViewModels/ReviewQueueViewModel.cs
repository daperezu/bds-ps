namespace FundingPlatform.Web.ViewModels;

public class ReviewQueueViewModel
{
    public List<ReviewQueueItemViewModel> Applications { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class ReviewQueueItemViewModel
{
    public int ApplicationId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public decimal? ApplicantPerformanceScore { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int ItemCount { get; set; }
}
