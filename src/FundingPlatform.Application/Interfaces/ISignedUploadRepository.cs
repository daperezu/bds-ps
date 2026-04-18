using FundingPlatform.Application.DTOs;
using FundingPlatform.Domain.Entities;

namespace FundingPlatform.Application.Interfaces;

public interface ISignedUploadRepository
{
    /// <summary>
    /// Loads a SignedUpload with its FundingAgreement + Application + Applicant hydrated,
    /// for authorization and action routing.
    /// </summary>
    Task<SignedUpload?> GetByIdWithParentAsync(int signedUploadId);

    /// <summary>
    /// Paged reviewer-inbox projection: applications whose latest signed upload is Pending.
    /// Admins see all; plain reviewers see only their assigned applications.
    /// </summary>
    Task<(IReadOnlyList<SigningInboxRowDto> Rows, int TotalCount)> GetPendingInboxAsync(
        string? reviewerUserId,
        bool isAdmin,
        int page,
        int pageSize);

    Task SaveChangesAsync();
}
