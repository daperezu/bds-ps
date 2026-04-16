namespace FundingPlatform.Application.DTOs;

public record ReviewQueueItemDto(
    int ApplicationId,
    string ApplicantName,
    decimal? ApplicantPerformanceScore,
    DateTime SubmittedAt,
    int ItemCount);
