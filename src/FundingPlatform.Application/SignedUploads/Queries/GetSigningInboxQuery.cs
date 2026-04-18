namespace FundingPlatform.Application.SignedUploads.Queries;

public record GetSigningInboxQuery(
    string CurrentUserId,
    bool IsAdministrator,
    int Page,
    int PageSize);
