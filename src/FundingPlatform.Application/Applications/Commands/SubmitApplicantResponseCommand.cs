using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Application.Applications.Commands;

public record SubmitApplicantResponseCommand(
    int ApplicationId,
    string UserId,
    Dictionary<int, ItemResponseDecision> ItemDecisions);
