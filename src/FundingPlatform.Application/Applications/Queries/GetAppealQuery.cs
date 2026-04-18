namespace FundingPlatform.Application.Applications.Queries;

public record GetAppealQuery(int ApplicationId, string UserId, bool IsReviewer);
