using System.Reflection;
using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Tests.Unit.Domain;

[TestFixture]
public class SigningReviewDecisionTests
{
    [Test]
    public void Rejection_WithoutComment_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Invoke(outcome: SigningDecisionOutcome.Rejected, comment: null));

        Assert.Throws<InvalidOperationException>(() =>
            Invoke(outcome: SigningDecisionOutcome.Rejected, comment: "  "));
    }

    [Test]
    public void Approval_WithoutComment_Succeeds()
    {
        var decision = Invoke(outcome: SigningDecisionOutcome.Approved, comment: null);

        Assert.That(decision.Outcome, Is.EqualTo(SigningDecisionOutcome.Approved));
        Assert.That(decision.Comment, Is.Null);
    }

    [Test]
    public void EmptyReviewerId_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Invoke(reviewerUserId: ""));

        Assert.Throws<InvalidOperationException>(() =>
            Invoke(reviewerUserId: "  "));
    }

    [Test]
    public void DecidedAtUtc_IsUtc()
    {
        var decision = Invoke();

        Assert.That(decision.DecidedAtUtc.Kind,
            Is.EqualTo(DateTimeKind.Utc).Or.EqualTo(DateTimeKind.Unspecified));
        Assert.That(decision.DecidedAtUtc, Is.LessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1)));
    }

    private static SigningReviewDecision Invoke(
        int signedUploadId = 1,
        SigningDecisionOutcome outcome = SigningDecisionOutcome.Approved,
        string reviewerUserId = "reviewer-1",
        string? comment = "ok")
    {
        var ctor = typeof(SigningReviewDecision).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(int), typeof(SigningDecisionOutcome), typeof(string), typeof(string) },
            modifiers: null)!;

        try
        {
            return (SigningReviewDecision)ctor.Invoke(new object?[]
            {
                signedUploadId, outcome, reviewerUserId, comment
            });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
