using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Application.DTOs;

public record ApplicantResponseDto(
    int ApplicationId,
    int? CycleNumber,
    DateTime? SubmittedAt,
    bool IsSubmitted,
    ApplicationState State,
    List<ItemResponseDto> Items);
