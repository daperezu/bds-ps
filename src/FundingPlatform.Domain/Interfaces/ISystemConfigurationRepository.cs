using FundingPlatform.Domain.Entities;

namespace FundingPlatform.Domain.Interfaces;

public interface ISystemConfigurationRepository
{
    Task<SystemConfiguration?> GetByKeyAsync(string key);
    Task<SystemConfiguration?> GetByIdAsync(int id);
    Task<List<SystemConfiguration>> GetAllAsync();
    Task UpdateAsync(SystemConfiguration configuration);
    Task SaveChangesAsync();
}
