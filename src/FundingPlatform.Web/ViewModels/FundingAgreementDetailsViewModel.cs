namespace FundingPlatform.Web.ViewModels;

public class FundingAgreementDetailsViewModel
{
    public SigningStagePanelViewModel Panel { get; set; } = new();
    public FundingAgreementDocumentViewModel? Preview { get; set; }
    public bool HasApplicantResponse { get; set; }
}
