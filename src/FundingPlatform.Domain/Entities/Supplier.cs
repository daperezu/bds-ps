namespace FundingPlatform.Domain.Entities;

public class Supplier
{
    public int Id { get; private set; }
    public string LegalId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Location { get; private set; }
    public bool HasElectronicInvoice { get; private set; }
    public string? ShippingDetails { get; private set; }
    public string? WarrantyInfo { get; private set; }
    public bool IsCompliantCCSS { get; private set; }
    public bool IsCompliantHacienda { get; private set; }
    public bool IsCompliantSICOP { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Supplier() { }

    public Supplier(
        string legalId,
        string name,
        string? contactName,
        string? email,
        string? phone,
        string? location,
        bool hasElectronicInvoice,
        string? shippingDetails,
        string? warrantyInfo,
        bool isCompliantCCSS,
        bool isCompliantHacienda,
        bool isCompliantSICOP)
    {
        LegalId = legalId;
        Name = name;
        ContactName = contactName;
        Email = email;
        Phone = phone;
        Location = location;
        HasElectronicInvoice = hasElectronicInvoice;
        ShippingDetails = shippingDetails;
        WarrantyInfo = warrantyInfo;
        IsCompliantCCSS = isCompliantCCSS;
        IsCompliantHacienda = isCompliantHacienda;
        IsCompliantSICOP = isCompliantSICOP;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
