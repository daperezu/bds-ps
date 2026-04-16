namespace FundingPlatform.Domain.Entities;

public class Quotation
{
    public int Id { get; private set; }
    public int ItemId { get; private set; }
    public int SupplierId { get; private set; }
    public decimal Price { get; private set; }
    public DateOnly ValidUntil { get; private set; }
    public int DocumentId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Supplier Supplier { get; private set; } = null!;
    public Document Document { get; private set; } = null!;

    private Quotation() { }

    public Quotation(int supplierId, int documentId, decimal price, DateOnly validUntil)
    {
        SupplierId = supplierId;
        DocumentId = documentId;
        Price = price;
        ValidUntil = validUntil;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Replaces the document associated with this quotation and returns the old document ID.
    /// </summary>
    public int ReplaceDocument(int newDocumentId)
    {
        var oldDocumentId = DocumentId;
        DocumentId = newDocumentId;
        return oldDocumentId;
    }
}
