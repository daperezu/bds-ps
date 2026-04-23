using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Web.ViewModels;

public class ApplicantResponseViewModel
{
    public int ApplicationId { get; set; }
    public bool IsSubmitted { get; set; }
    public ApplicationState State { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public List<ItemResponseViewModel> Items { get; set; } = [];
    public bool CanOpenAppeal { get; set; }
    public bool HasOpenAppeal { get; set; }
    public string? AppealBlockedReason { get; set; }
    public bool HasFundingAgreement { get; set; }
}
