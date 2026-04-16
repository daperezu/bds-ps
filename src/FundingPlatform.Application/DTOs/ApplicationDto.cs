using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Application.DTOs;

public record ApplicationDto(
    int Id,
    int ApplicantId,
    ApplicationState State,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SubmittedAt,
    List<ItemDto> Items);

public record ApplicationSummaryDto(
    int Id,
    ApplicationState State,
    int ItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SubmittedAt);
