using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Application.Applications.Commands;

public record ResolveAppealCommand(int ApplicationId, string UserId, AppealResolution Resolution);
