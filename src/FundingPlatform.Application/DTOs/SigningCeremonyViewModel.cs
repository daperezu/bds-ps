namespace FundingPlatform.Application.DTOs;

/// <summary>Spec 011 US3 — signing ceremony view model (data-model.md §4).</summary>
public sealed record SigningCeremonyViewModel(
    Guid ApplicationId,
    SigningCeremonyVariant Variant,
    bool IsFresh,
    string ApplicantFirstName,
    string ProjectName,
    decimal FundedAmount,
    string CurrencyCode,
    DateOnly DisbursementDate,
    string ViewFundingDetailsHref,
    string DashboardHref);

public enum SigningCeremonyVariant
{
    ApplicantOnlySigned,
    FunderOnlySigned,
    BothCompleteApplicantLast,
    BothCompleteFunderLast,
}
