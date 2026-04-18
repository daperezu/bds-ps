using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Domain.Entities;

public class ItemResponse
{
    public int Id { get; private set; }
    public int ApplicantResponseId { get; private set; }
    public int ItemId { get; private set; }
    public ItemResponseDecision Decision { get; private set; }

    private ItemResponse() { }

    internal ItemResponse(int itemId, ItemResponseDecision decision)
    {
        ItemId = itemId;
        Decision = decision;
    }
}
