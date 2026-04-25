namespace FundingPlatform.Web.ViewModels;

public class SigningInboxRowViewModel
{
    public int ApplicationId { get; set; }
    public string ApplicantDisplayName { get; set; } = "";
    public int SignedUploadId { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public int GeneratedVersionAtUpload { get; set; }
    public bool VersionMatchesCurrent { get; set; }
}
