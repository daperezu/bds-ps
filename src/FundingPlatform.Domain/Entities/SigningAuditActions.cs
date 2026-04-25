namespace FundingPlatform.Domain.Entities;

public static class SigningAuditActions
{
    public const string AgreementDownloaded = "AgreementDownloaded";
    public const string SignedAgreementUploaded = "SignedAgreementUploaded";
    public const string SignedUploadReplaced = "SignedUploadReplaced";
    public const string SignedUploadWithdrawn = "SignedUploadWithdrawn";
    public const string SignedUploadApproved = "SignedUploadApproved";
    public const string SignedUploadRejected = "SignedUploadRejected";
    public const string FundingAgreementRegenerationBlocked = "FundingAgreementRegenerationBlocked";
}
