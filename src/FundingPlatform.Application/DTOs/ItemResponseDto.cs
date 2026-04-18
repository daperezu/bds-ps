using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Application.DTOs;

public record ItemResponseDto(
    int ItemId,
    string ProductName,
    ItemReviewStatus ReviewStatus,
    string? SelectedSupplierName,
    decimal? Amount,
    string? ReviewComment,
    ItemResponseDecision? Decision);
