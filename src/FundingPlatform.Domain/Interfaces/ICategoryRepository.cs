using FundingPlatform.Domain.Entities;

namespace FundingPlatform.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllActiveAsync();
    Task<Category?> GetByIdAsync(int id);
}
