using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Domain.Interfaces;

public interface IApplicationRepository
{
    Task<Application?> GetByIdAsync(int id);
    Task<Application?> GetByIdWithDetailsAsync(int id);
    Task<Application?> GetByIdWithResponseAndAppealsAsync(int id);
    Task<List<Application>> GetByApplicantIdAsync(int applicantId);
    Task<(List<Application> Items, int TotalCount)> GetByStatePagedAsync(ApplicationState state, int page, int pageSize);
    Task<(List<Application> Items, int TotalCount)> GetPendingAgreementPagedAsync(int page, int pageSize);
    Task AddAsync(Application application);
    Task UpdateAsync(Application application);
    Task SaveChangesAsync();
}
