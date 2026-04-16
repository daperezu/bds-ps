namespace FundingPlatform.Application.Applications.Commands;

public class AddSupplierQuotationCommand
{
    public int ApplicationId { get; set; }
    public int ItemId { get; set; }
    public string SupplierLegalId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public bool HasElectronicInvoice { get; set; }
    public string? ShippingDetails { get; set; }
    public string? WarrantyInfo { get; set; }
    public string? ComplianceStatus { get; set; }
    public decimal Price { get; set; }
    public DateOnly ValidUntil { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
