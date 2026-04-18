namespace FundingPlatform.Application.DTOs;

public record FundingAgreementItemRowDto(
    int ItemId,
    string ProductName,
    string CategoryName,
    string SupplierName,
    decimal UnitPrice,
    decimal LineTotal);
