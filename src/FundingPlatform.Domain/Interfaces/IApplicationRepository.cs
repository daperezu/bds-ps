using FundingPlatform.Domain.Entities;

namespace FundingPlatform.Domain.Interfaces;

public interface IApplicationRepository
{
    Task<Application?> GetByIdAsync(int id);
    Task<Application?> GetByIdWithDetailsAsync(int id);
    Task<List<Application>> GetByApplicantIdAsync(int applicantId);
    Task AddAsync(Application application);
    Task UpdateAsync(Application application);
    Task SaveChangesAsync();
}
