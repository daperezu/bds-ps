using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Tests.Unit.Domain;

[TestFixture]
public class ApplicantResponseTests
{
    [Test]
    public void Submit_WithAllAcceptDecisions_CreatesResponseWithOneItemResponsePerItem()
    {
        var itemIds = new[] { 1, 2, 3 };
        var decisions = new Dictionary<int, ItemResponseDecision>
        {
            [1] = ItemResponseDecision.Accept,
            [2] = ItemResponseDecision.Accept,
            [3] = ItemResponseDecision.Accept
        };

        var response = ApplicantResponse.Submit(
            applicationId: 10,
            cycleNumber: 1,
            submittedByUserId: "user-1",
            applicationItemIds: itemIds,
            itemDecisions: decisions);

        Assert.That(response.ApplicationId, Is.EqualTo(10));
        Assert.That(response.CycleNumber, Is.EqualTo(1));
        Assert.That(response.SubmittedByUserId, Is.EqualTo("user-1"));
        Assert.That(response.SubmittedAt, Is.GreaterThan(DateTime.UtcNow.AddMinutes(-1)));
        Assert.That(response.ItemResponses, Has.Count.EqualTo(3));
        Assert.That(response.ItemResponses.All(ir => ir.Decision == ItemResponseDecision.Accept), Is.True);
    }

    [Test]
    public void Submit_WithMixedDecisions_PreservesPerItemChoice()
    {
        var itemIds = new[] { 1, 2 };
        var decisions = new Dictionary<int, ItemResponseDecision>
        {
            [1] = ItemResponseDecision.Accept,
            [2] = ItemResponseDecision.Reject
        };

        var response = ApplicantResponse.Submit(1, 1, "u", itemIds, decisions);

        Assert.That(response.ItemResponses.First(ir => ir.ItemId == 1).Decision, Is.EqualTo(ItemResponseDecision.Accept));
        Assert.That(response.ItemResponses.First(ir => ir.ItemId == 2).Decision, Is.EqualTo(ItemResponseDecision.Reject));
    }

    [Test]
    public void Submit_MissingDecisionForItem_Throws()
    {
        var itemIds = new[] { 1, 2, 3 };
        var decisions = new Dictionary<int, ItemResponseDecision>
        {
            [1] = ItemResponseDecision.Accept
        };

        Assert.Throws<InvalidOperationException>(() =>
            ApplicantResponse.Submit(1, 1, "u", itemIds, decisions));
    }

    [Test]
    public void Submit_DecisionForUnknownItem_Throws()
    {
        var itemIds = new[] { 1, 2 };
        var decisions = new Dictionary<int, ItemResponseDecision>
        {
            [1] = ItemResponseDecision.Accept,
            [2] = ItemResponseDecision.Accept,
            [99] = ItemResponseDecision.Reject
        };

        Assert.Throws<InvalidOperationException>(() =>
            ApplicantResponse.Submit(1, 1, "u", itemIds, decisions));
    }

    [Test]
    public void Submit_NoItemsOnApplication_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ApplicantResponse.Submit(1, 1, "u", Array.Empty<int>(), new Dictionary<int, ItemResponseDecision>()));
    }

    [Test]
    public void ItemResponses_AreExposedAsReadOnly()
    {
        var response = ApplicantResponse.Submit(
            1,
            1,
            "u",
            new[] { 1 },
            new Dictionary<int, ItemResponseDecision> { [1] = ItemResponseDecision.Accept });

        Assert.That(response.ItemResponses, Is.InstanceOf<IReadOnlyList<ItemResponse>>());
    }
}
