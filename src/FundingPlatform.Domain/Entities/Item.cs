namespace FundingPlatform.Domain.Entities;

public class Item
{
    private readonly List<Quotation> _quotations = [];

    public int Id { get; private set; }
    public int ApplicationId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public string TechnicalSpecifications { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Category Category { get; private set; } = null!;
    public Impact? Impact { get; private set; }

    public IReadOnlyList<Quotation> Quotations => _quotations.AsReadOnly();

    private Item() { }

    public Item(string productName, int categoryId, string technicalSpecifications)
    {
        ProductName = productName;
        CategoryId = categoryId;
        TechnicalSpecifications = technicalSpecifications;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the item's product name, category, and technical specifications.
    /// </summary>
    public void Update(string productName, int categoryId, string technicalSpecifications)
    {
        ProductName = productName;
        CategoryId = categoryId;
        TechnicalSpecifications = technicalSpecifications;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a quotation for the specified supplier. Prevents duplicate suppliers on the same item.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the supplier already has a quotation on this item.</exception>
    public void AddQuotation(Supplier supplier, Document document, decimal price, DateOnly validUntil)
    {
        if (_quotations.Any(q => q.SupplierId == supplier.Id))
        {
            throw new InvalidOperationException(
                $"Supplier '{supplier.Name}' already has a quotation on this item.");
        }

        var quotation = new Quotation(supplier.Id, document.Id, price, validUntil);
        _quotations.Add(quotation);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a quotation from this item by its identifier.
    /// </summary>
    public void RemoveQuotation(int quotationId)
    {
        var quotation = _quotations.FirstOrDefault(q => q.Id == quotationId);
        if (quotation is not null)
        {
            _quotations.Remove(quotation);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Sets the impact assessment for this item using the specified template and parameter values.
    /// </summary>
    public void SetImpact(ImpactTemplate template, List<ImpactParameterValue> parameterValues)
    {
        Impact = new Impact(template.Id, parameterValues);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Determines whether this item has at least the specified minimum number of quotations.
    /// </summary>
    public bool HasMinimumQuotations(int min)
    {
        return _quotations.Count >= min;
    }

    /// <summary>
    /// Determines whether this item has a complete impact assessment with parameter values.
    /// </summary>
    public bool HasCompleteImpact()
    {
        return Impact is not null && Impact.ParameterValues.Count > 0;
    }
}
