using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Domain.Entities;

public class Application
{
    private readonly List<Item> _items = [];
    private readonly List<VersionHistory> _versionHistory = [];
    private readonly List<ApplicantResponse> _applicantResponses = [];
    private readonly List<Appeal> _appeals = [];

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
    public IReadOnlyList<ApplicantResponse> ApplicantResponses => _applicantResponses.AsReadOnly();
    public IReadOnlyList<Appeal> Appeals => _appeals.AsReadOnly();

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

    /// <summary>
    /// Submits the applicant's per-item response. Transitions the application from
    /// Resolved to ResponseFinalized. Requires a decision for every item on the application.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the application is not in the Resolved state, or when the decision map
    /// does not cover every item exactly once.
    /// </exception>
    public ApplicantResponse SubmitResponse(
        IReadOnlyDictionary<int, ItemResponseDecision> itemDecisions,
        string submittedByUserId)
    {
        if (State != ApplicationState.Resolved)
        {
            throw new InvalidOperationException(
                $"Cannot submit response: application is in '{State}' state, expected 'Resolved'.");
        }

        var cycleNumber = _applicantResponses.Count + 1;
        var itemIds = _items.Select(i => i.Id).ToList();

        var response = ApplicantResponse.Submit(
            Id,
            cycleNumber,
            submittedByUserId,
            itemIds,
            itemDecisions);

        _applicantResponses.Add(response);
        State = ApplicationState.ResponseFinalized;
        UpdatedAt = DateTime.UtcNow;

        return response;
    }

    /// <summary>
    /// Opens an appeal against the most recent applicant response. Freezes the application
    /// by transitioning to AppealOpen.
    /// </summary>
    public Appeal OpenAppeal(string openedByUserId, int maxAppeals)
    {
        if (State != ApplicationState.ResponseFinalized)
        {
            throw new InvalidOperationException(
                $"Cannot open appeal: application is in '{State}' state, expected 'ResponseFinalized'.");
        }

        if (_appeals.Count >= maxAppeals)
        {
            throw new InvalidOperationException(
                $"Cannot open appeal: maximum appeal count ({maxAppeals}) reached.");
        }

        var latestResponse = _applicantResponses
            .OrderByDescending(r => r.CycleNumber)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Cannot open appeal: no applicant response exists for this application.");

        if (!latestResponse.ItemResponses.Any(ir => ir.Decision == ItemResponseDecision.Reject))
        {
            throw new InvalidOperationException(
                "Cannot open appeal: the response does not include any rejected items.");
        }

        var appeal = Appeal.Open(Id, latestResponse.Id, openedByUserId);
        _appeals.Add(appeal);
        State = ApplicationState.AppealOpen;
        UpdatedAt = DateTime.UtcNow;

        return appeal;
    }

    /// <summary>
    /// Resolves the active appeal as Uphold. Application returns to ResponseFinalized.
    /// </summary>
    public void ResolveAppealAsUphold(string resolvedByUserId)
    {
        var appeal = GetActiveAppealOrThrow();
        appeal.Resolve(resolvedByUserId, AppealResolution.Uphold);

        State = ApplicationState.ResponseFinalized;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resolves the active appeal as Grant — Reopen to Draft. Application returns to Draft
    /// so the applicant can revise the submission.
    /// </summary>
    public void ResolveAppealAsGrantReopenToDraft(string resolvedByUserId)
    {
        var appeal = GetActiveAppealOrThrow();
        appeal.Resolve(resolvedByUserId, AppealResolution.GrantReopenToDraft);

        State = ApplicationState.Draft;
        SubmittedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resolves the active appeal as Grant — Reopen to Review. Application returns to
    /// UnderReview WITHOUT resetting item review statuses (unlike SendBack).
    /// </summary>
    public void ResolveAppealAsGrantReopenToReview(string resolvedByUserId)
    {
        var appeal = GetActiveAppealOrThrow();
        appeal.Resolve(resolvedByUserId, AppealResolution.GrantReopenToReview);

        State = ApplicationState.UnderReview;
        UpdatedAt = DateTime.UtcNow;
    }

    private Appeal GetActiveAppealOrThrow()
    {
        if (State != ApplicationState.AppealOpen)
        {
            throw new InvalidOperationException(
                $"Cannot resolve appeal: application is in '{State}' state, expected 'AppealOpen'.");
        }

        return _appeals
            .OrderByDescending(a => a.OpenedAt)
            .FirstOrDefault(a => a.Status == AppealStatus.Open)
            ?? throw new InvalidOperationException("Cannot resolve appeal: no open appeal found.");
    }
}
