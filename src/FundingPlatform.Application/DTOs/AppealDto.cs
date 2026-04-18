using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Application.DTOs;

public record AppealDto(
    int Id,
    int ApplicationId,
    DateTime OpenedAt,
    string OpenedByUserId,
    AppealStatus Status,
    AppealResolution? Resolution,
    DateTime? ResolvedAt,
    string? ResolvedByUserId,
    List<AppealMessageDto> Messages);
