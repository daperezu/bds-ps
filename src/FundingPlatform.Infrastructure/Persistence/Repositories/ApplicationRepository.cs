using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FundingPlatform.Infrastructure.Persistence.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly AppDbContext _context;

    public ApplicationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AppEntity?> GetByIdAsync(int id)
    {
        return await _context.Applications.FindAsync(id);
    }

    public async Task<AppEntity?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Applications
            .Include(a => a.Items)
                .ThenInclude(i => i.Category)
            .Include(a => a.Items)
                .ThenInclude(i => i.Quotations)
                    .ThenInclude(q => q.Supplier)
            .Include(a => a.Items)
                .ThenInclude(i => i.Quotations)
                    .ThenInclude(q => q.Document)
            .Include(a => a.Items)
                .ThenInclude(i => i.Impact)
                    .ThenInclude(imp => imp!.ImpactTemplate)
            .Include(a => a.Items)
                .ThenInclude(i => i.Impact)
                    .ThenInclude(imp => imp!.ParameterValues)
                        .ThenInclude(pv => pv.ImpactTemplateParameter)
            .Include(a => a.Applicant)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<AppEntity>> GetByApplicantIdAsync(int applicantId)
    {
        return await _context.Applications
            .Include(a => a.Items)
            .Where(a => a.ApplicantId == applicantId)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(AppEntity application)
    {
        await _context.Applications.AddAsync(application);
    }

    public Task UpdateAsync(AppEntity application)
    {
        _context.Applications.Update(application);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
