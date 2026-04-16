using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Domain.Entities;

public class Application
{
    private readonly List<Item> _items = [];
    private readonly List<VersionHistory> _versionHistory = [];

    public int Id { get; private set; }
    public int ApplicantId { get; private set; }
    public ApplicationState State { get; private set; } = ApplicationState.Draft;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public Applicant Applicant { get; private set; } = null!;

    public IReadOnlyList<Item> Items => _items.AsReadOnly();
    public IReadOnlyList<VersionHistory> VersionHistory => _versionHistory.AsReadOnly();

    private Application() { }

    public Application(int applicantId)
    {
        ApplicantId = applicantId;
        State = ApplicationState.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds an item to the application.
    /// </summary>
    public void AddItem(Item item)
    {
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes an item from the application by its identifier.
    /// </summary>
    public void RemoveItem(int itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is not null)
        {
            _items.Remove(item);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Validates the application and transitions its state to Submitted.
    /// Throws <see cref="InvalidOperationException"/> if any validation errors are found.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the application fails validation.</exception>
    public void Submit(int minQuotations)
    {
        var errors = Validate(minQuotations);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot submit application: {string.Join("; ", errors)}");
        }

        State = ApplicationState.Submitted;
        SubmittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a version history entry for this application.
    /// </summary>
    public void AddVersionHistory(VersionHistory entry)
    {
        _versionHistory.Add(entry);
    }

    /// <summary>
    /// Validates the application and returns a list of validation error messages.
    /// Checks that the application has at least one item, each item meets the minimum
    /// quotation requirement, and each item has a complete impact assessment.
    /// </summary>
    public List<string> Validate(int minQuotations)
    {
        var errors = new List<string>();

        if (_items.Count == 0)
        {
            errors.Add("Application must have at least one item.");
        }

        foreach (var item in _items)
        {
            if (!item.HasMinimumQuotations(minQuotations))
            {
                errors.Add(
                    $"Item '{item.ProductName}' must have at least {minQuotations} quotation(s).");
            }

            if (!item.HasCompleteImpact())
            {
                errors.Add(
                    $"Item '{item.ProductName}' must have a complete impact assessment.");
            }
        }

        return errors;
    }

    /// <summary>
    /// Transitions the application from Submitted to Under Review.
    /// Idempotent — no-op if already Under Review.
    /// </summary>
    public void StartReview()
    {
        if (State == ApplicationState.UnderReview)
            return;

        if (State != ApplicationState.Submitted)
        {
            throw new InvalidOperationException(
                $"Cannot start review: application is in '{State}' state, expected 'Submitted'.");
        }

        State = ApplicationState.UnderReview;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sends the application back to Draft. Resets all item review statuses to Pending.
    /// Preserves item review comments.
    /// </summary>
    public void SendBack()
    {
        if (State != ApplicationState.UnderReview)
        {
            throw new InvalidOperationException(
                $"Cannot send back: application is in '{State}' state, expected 'UnderReview'.");
        }

        State = ApplicationState.Draft;
        SubmittedAt = null;
        UpdatedAt = DateTime.UtcNow;

        foreach (var item in _items)
        {
            item.ResetReviewStatus();
        }
    }

    /// <summary>
    /// Finalizes the review, transitioning the application to Resolved.
    /// If force is false and there are unresolved items (Pending or NeedsInfo), throws an exception.
    /// If force is true, unresolved items are implicitly rejected.
    /// </summary>
    public void Finalize(bool force)
    {
        if (State != ApplicationState.UnderReview)
        {
            throw new InvalidOperationException(
                $"Cannot finalize: application is in '{State}' state, expected 'UnderReview'.");
        }

        var unresolvedItems = _items
            .Where(i => i.ReviewStatus == Enums.ItemReviewStatus.Pending
                     || i.ReviewStatus == Enums.ItemReviewStatus.NeedsInfo)
            .ToList();

        if (unresolvedItems.Count > 0 && !force)
        {
            var itemNames = string.Join(", ", unresolvedItems.Select(i => $"'{i.ProductName}'"));
            throw new InvalidOperationException(
                $"Cannot finalize: the following items are unresolved: {itemNames}. Use force to implicitly reject them.");
        }

        if (force)
        {
            foreach (var item in unresolvedItems)
            {
                item.Reject("Implicitly rejected during finalization");
            }
        }

        State = ApplicationState.Resolved;
        UpdatedAt = DateTime.UtcNow;
    }
}
