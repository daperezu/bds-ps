using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FundingPlatform.Infrastructure.Persistence.Repositories;

public class SystemConfigurationRepository : ISystemConfigurationRepository
{
    private readonly AppDbContext _context;

    public SystemConfigurationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SystemConfiguration?> GetByKeyAsync(string key)
    {
        return await _context.SystemConfigurations.FirstOrDefaultAsync(s => s.Key == key);
    }

    public async Task<SystemConfiguration?> GetByIdAsync(int id)
    {
        return await _context.SystemConfigurations.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<SystemConfiguration>> GetAllAsync()
    {
        return await _context.SystemConfigurations.OrderBy(s => s.Key).ToListAsync();
    }

    public Task UpdateAsync(SystemConfiguration configuration)
    {
        _context.SystemConfigurations.Update(configuration);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
