using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Application.Admin.Reports.DTOs;

public sealed record FundedItemRowDto(
    int AppId,
    string ApplicantFullName,
    string ItemProductName,
    string CategoryName,
    string SupplierName,
    string? SupplierLegalId,
    decimal Price,
    string Currency,
    ApplicationState AppState,
    DateTime? AppSubmittedAt,
    DateTime? ApprovedAt,
    bool HasAgreement,
    bool Executed);
