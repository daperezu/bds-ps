namespace FundingPlatform.Web.ViewModels;

public class GenerateAgreementQueueViewModel
{
    public List<GenerateAgreementQueueItemViewModel> Applications { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class GenerateAgreementQueueItemViewModel
{
    public int ApplicationId { get; set; }
    public string ApplicantDisplayName { get; set; } = string.Empty;
    public DateTime ResponseFinalizedAtUtc { get; set; }
}
