using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Application.DTOs;

public record SignedUploadSummaryDto(
    int SignedUploadId,
    DateTime UploadedAtUtc,
    string UploaderUserId,
    string? UploaderDisplayName,
    string FileName,
    long Size,
    int GeneratedVersionAtUpload,
    SignedUploadStatus Status);
