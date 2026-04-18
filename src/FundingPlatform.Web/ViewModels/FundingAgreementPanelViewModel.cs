namespace FundingPlatform.Web.ViewModels;

public class FundingAgreementPanelViewModel
{
    public int ApplicationId { get; set; }
    public bool AgreementExists { get; set; }
    public string? AgreementDownloadUrl { get; set; }
    public bool CanGenerate { get; set; }
    public bool CanRegenerate { get; set; }
    public string? DisabledReason { get; set; }
    public DateTime? GeneratedAtUtc { get; set; }
    public string? GeneratedByDisplayName { get; set; }
    public bool ShowActions { get; set; }
}
