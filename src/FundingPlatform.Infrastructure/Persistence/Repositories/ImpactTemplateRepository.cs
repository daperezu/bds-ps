using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FundingPlatform.Infrastructure.Persistence.Repositories;

public class ImpactTemplateRepository : IImpactTemplateRepository
{
    private readonly AppDbContext _context;

    public ImpactTemplateRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ImpactTemplate>> GetAllActiveAsync()
    {
        return await _context.ImpactTemplates
            .Include(t => t.Parameters.OrderBy(p => p.SortOrder))
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<ImpactTemplate>> GetAllAsync()
    {
        return await _context.ImpactTemplates
            .Include(t => t.Parameters.OrderBy(p => p.SortOrder))
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<ImpactTemplate?> GetByIdWithParametersAsync(int id)
    {
        return await _context.ImpactTemplates
            .Include(t => t.Parameters.OrderBy(p => p.SortOrder))
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddAsync(ImpactTemplate template)
    {
        await _context.ImpactTemplates.AddAsync(template);
    }

    public Task UpdateAsync(ImpactTemplate template)
    {
        _context.ImpactTemplates.Update(template);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
