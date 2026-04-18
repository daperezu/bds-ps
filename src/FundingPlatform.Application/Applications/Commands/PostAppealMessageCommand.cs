namespace FundingPlatform.Application.Applications.Commands;

public record PostAppealMessageCommand(int ApplicationId, string UserId, string Text);
