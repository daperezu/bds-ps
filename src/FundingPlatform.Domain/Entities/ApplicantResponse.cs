using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Domain.Entities;

public class ApplicantResponse
{
    private readonly List<ItemResponse> _itemResponses = [];

    public int Id { get; private set; }
    public int ApplicationId { get; private set; }
    public int CycleNumber { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public string SubmittedByUserId { get; private set; } = string.Empty;

    public IReadOnlyList<ItemResponse> ItemResponses => _itemResponses.AsReadOnly();

    private ApplicantResponse() { }

    public static ApplicantResponse Submit(
        int applicationId,
        int cycleNumber,
        string submittedByUserId,
        IReadOnlyCollection<int> applicationItemIds,
        IReadOnlyDictionary<int, ItemResponseDecision> itemDecisions)
    {
        if (itemDecisions is null)
            throw new ArgumentNullException(nameof(itemDecisions));

        if (applicationItemIds.Count == 0)
            throw new InvalidOperationException("Cannot submit a response for an application with no items.");

        var missing = applicationItemIds.Where(id => !itemDecisions.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Response is missing decisions for item(s): {string.Join(", ", missing)}.");
        }

        var extra = itemDecisions.Keys.Where(id => !applicationItemIds.Contains(id)).ToList();
        if (extra.Count > 0)
        {
            throw new InvalidOperationException(
                $"Response contains decisions for item(s) not on the application: {string.Join(", ", extra)}.");
        }

        var response = new ApplicantResponse
        {
            ApplicationId = applicationId,
            CycleNumber = cycleNumber,
            SubmittedByUserId = submittedByUserId,
            SubmittedAt = DateTime.UtcNow
        };

        foreach (var itemId in applicationItemIds)
        {
            response._itemResponses.Add(new ItemResponse(itemId, itemDecisions[itemId]));
        }

        return response;
    }
}
