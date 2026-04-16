using FundingPlatform.Domain.Entities;

namespace FundingPlatform.Domain.Interfaces;

public interface IImpactTemplateRepository
{
    Task<List<ImpactTemplate>> GetAllActiveAsync();
    Task<List<ImpactTemplate>> GetAllAsync();
    Task<ImpactTemplate?> GetByIdWithParametersAsync(int id);
    Task AddAsync(ImpactTemplate template);
    Task UpdateAsync(ImpactTemplate template);
    Task SaveChangesAsync();
}
