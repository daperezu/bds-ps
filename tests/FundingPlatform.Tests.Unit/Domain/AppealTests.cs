using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Tests.Unit.Domain;

[TestFixture]
public class AppealTests
{
    [Test]
    public void Open_FactoryStampsTimestampAndStatusOpen()
    {
        var appeal = Appeal.Open(applicationId: 1, applicantResponseId: 2, openedByUserId: "user-1");

        Assert.That(appeal.ApplicationId, Is.EqualTo(1));
        Assert.That(appeal.ApplicantResponseId, Is.EqualTo(2));
        Assert.That(appeal.OpenedByUserId, Is.EqualTo("user-1"));
        Assert.That(appeal.Status, Is.EqualTo(AppealStatus.Open));
        Assert.That(appeal.OpenedAt, Is.GreaterThan(DateTime.UtcNow.AddMinutes(-1)));
        Assert.That(appeal.Resolution, Is.Null);
        Assert.That(appeal.ResolvedAt, Is.Null);
        Assert.That(appeal.ResolvedByUserId, Is.Null);
    }

    [Test]
    public void PostMessage_AppendsMessageInOrder()
    {
        var appeal = Appeal.Open(1, 2, "user-1");

        appeal.PostMessage("user-1", "first");
        appeal.PostMessage("user-2", "second");

        Assert.That(appeal.Messages, Has.Count.EqualTo(2));
        Assert.That(appeal.Messages[0].Text, Is.EqualTo("first"));
        Assert.That(appeal.Messages[1].Text, Is.EqualTo("second"));
    }

    [Test]
    public void PostMessage_EmptyText_Throws()
    {
        var appeal = Appeal.Open(1, 2, "user-1");
        Assert.Throws<InvalidOperationException>(() => appeal.PostMessage("user-1", ""));
        Assert.Throws<InvalidOperationException>(() => appeal.PostMessage("user-1", "   "));
    }

    [Test]
    public void PostMessage_TextOver4000Chars_Throws()
    {
        var appeal = Appeal.Open(1, 2, "user-1");
        var tooLong = new string('x', 4001);
        Assert.Throws<InvalidOperationException>(() => appeal.PostMessage("user-1", tooLong));
    }

    [Test]
    public void PostMessage_ThrowsAfterResolve()
    {
        var appeal = Appeal.Open(1, 2, "user-1");
        appeal.Resolve("reviewer-1", AppealResolution.Uphold);

        Assert.Throws<InvalidOperationException>(() => appeal.PostMessage("user-1", "too late"));
    }

    [Test]
    public void Resolve_SetsResolvedState()
    {
        var appeal = Appeal.Open(1, 2, "user-1");
        appeal.Resolve("reviewer-1", AppealResolution.GrantReopenToDraft);

        Assert.That(appeal.Status, Is.EqualTo(AppealStatus.Resolved));
        Assert.That(appeal.Resolution, Is.EqualTo(AppealResolution.GrantReopenToDraft));
        Assert.That(appeal.ResolvedByUserId, Is.EqualTo("reviewer-1"));
        Assert.That(appeal.ResolvedAt, Is.Not.Null);
    }

    [Test]
    public void Resolve_AlreadyResolved_Throws()
    {
        var appeal = Appeal.Open(1, 2, "user-1");
        appeal.Resolve("reviewer-1", AppealResolution.Uphold);
        Assert.Throws<InvalidOperationException>(() => appeal.Resolve("reviewer-2", AppealResolution.Uphold));
    }
}
