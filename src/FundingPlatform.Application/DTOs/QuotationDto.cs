namespace FundingPlatform.Application.DTOs;

public record QuotationDto(
    int Id,
    int SupplierId,
    string SupplierName,
    string SupplierLegalId,
    decimal Price,
    DateOnly ValidUntil,
    int DocumentId,
    string DocumentFileName);
