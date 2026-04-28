using FundingPlatform.Domain.ValueObjects;

namespace FundingPlatform.Application.Admin.Reports.DTOs;

public sealed record ApplicantRowDto(
    string FullName,
    string LegalId,
    string Email,
    int TotalApps,
    int ResolvedCount,
    int ResponseFinalizedCount,
    int AgreementExecutedCount,
    decimal? ApprovalRate,
    IReadOnlyList<CurrencyAmount> TotalApproved,
    IReadOnlyList<CurrencyAmount> TotalExecuted,
    DateTime? LastActivity);
