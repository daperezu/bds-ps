using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Domain.Entities;

public class SigningReviewDecision
{
    public int Id { get; private set; }
    public int SignedUploadId { get; private set; }
    public SigningDecisionOutcome Outcome { get; private set; }
    public string ReviewerUserId { get; private set; } = string.Empty;
    public string? Comment { get; private set; }
    public DateTime DecidedAtUtc { get; private set; }

    private SigningReviewDecision() { }

    internal SigningReviewDecision(
        int signedUploadId,
        SigningDecisionOutcome outcome,
        string reviewerUserId,
        string? comment)
    {
        if (string.IsNullOrWhiteSpace(reviewerUserId))
            throw new InvalidOperationException("SigningReviewDecision requires a non-empty reviewer user id.");
        if (outcome == SigningDecisionOutcome.Rejected && string.IsNullOrWhiteSpace(comment))
            throw new InvalidOperationException("Rejection requires a non-empty comment.");

        SignedUploadId = signedUploadId;
        Outcome = outcome;
        ReviewerUserId = reviewerUserId;
        Comment = comment;
        DecidedAtUtc = DateTime.UtcNow;
    }
}
