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
}
