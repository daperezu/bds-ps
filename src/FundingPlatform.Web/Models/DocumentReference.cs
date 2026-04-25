namespace FundingPlatform.Web.Models;

public sealed record DocumentReference(
    string Filename,
    long SizeBytes,
    string DownloadUrl,
    string? Signer = null,
    DateTimeOffset? GeneratedAt = null,
    DateTimeOffset? SignedAt = null,
    string? IconOverride = null);
