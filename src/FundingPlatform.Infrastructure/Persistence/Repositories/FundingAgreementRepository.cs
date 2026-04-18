using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FundingPlatform.Infrastructure.Persistence.Repositories;

public class FundingAgreementRepository : IFundingAgreementRepository
{
    private readonly AppDbContext _context;

    public FundingAgreementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FundingAgreement?> GetByApplicationIdAsync(int applicationId)
    {
        return await _context.FundingAgreements
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ApplicationId == applicationId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
