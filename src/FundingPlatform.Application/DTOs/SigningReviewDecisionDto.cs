using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Application.DTOs;

public record SigningReviewDecisionDto(
    SigningDecisionOutcome Outcome,
    string? Comment,
    DateTime DecidedAtUtc,
    string ReviewerUserId,
    string? ReviewerDisplayName);
