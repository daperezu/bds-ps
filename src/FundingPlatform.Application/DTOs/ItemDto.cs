namespace FundingPlatform.Application.DTOs;

public record ItemDto(
    int Id,
    string ProductName,
    int CategoryId,
    string CategoryName,
    string TechnicalSpecifications,
    List<QuotationDto> Quotations,
    ImpactDto? Impact);
