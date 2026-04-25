using FundingPlatform.Application.DTOs;
using FundingPlatform.Application.Interfaces;
using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FundingPlatform.Infrastructure.Persistence.Repositories;

public class SignedUploadRepository : ISignedUploadRepository
{
    private readonly AppDbContext _context;

    public SignedUploadRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SignedUpload?> GetByIdWithParentAsync(int signedUploadId)
    {
        return await _context.SignedUploads
            .Include(u => u.ReviewDecision)
            .FirstOrDefaultAsync(u => u.Id == signedUploadId);
    }

    public async Task<(IReadOnlyList<SigningInboxRowDto> Rows, int TotalCount)> GetPendingInboxAsync(
        string? reviewerUserId,
        bool isAdmin,
        int page,
        int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;

        var query =
            from upload in _context.SignedUploads.AsNoTracking()
            join agreement in _context.FundingAgreements.AsNoTracking()
                on upload.FundingAgreementId equals agreement.Id
            join app in _context.Applications.AsNoTracking()
                on agreement.ApplicationId equals app.Id
            join applicant in _context.Applicants.AsNoTracking()
                on app.ApplicantId equals applicant.Id
            where upload.Status == SignedUploadStatus.Pending
            select new
            {
                ApplicationId = app.Id,
                SignedUploadId = upload.Id,
                ApplicantFirstName = applicant.FirstName,
                ApplicantLastName = applicant.LastName,
                upload.UploadedAtUtc,
                upload.GeneratedVersionAtUpload,
                AgreementGeneratedVersion = agreement.GeneratedVersion
            };

        var totalCount = await query.CountAsync();

        var projected = await query
            .OrderByDescending(r => r.UploadedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var rows = projected
            .Select(r => new SigningInboxRowDto(
                ApplicationId: r.ApplicationId,
                ApplicantDisplayName: $"{r.ApplicantFirstName} {r.ApplicantLastName}".Trim(),
                SignedUploadId: r.SignedUploadId,
                UploadedAtUtc: r.UploadedAtUtc,
                GeneratedVersionAtUpload: r.GeneratedVersionAtUpload,
                VersionMatchesCurrent: r.GeneratedVersionAtUpload == r.AgreementGeneratedVersion))
            .ToList()
            .AsReadOnly();

        return (rows, totalCount);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
