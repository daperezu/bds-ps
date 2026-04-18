using FundingPlatform.Domain.Entities;

namespace FundingPlatform.Domain.Interfaces;

public interface IFundingAgreementRepository
{
    Task<FundingAgreement?> GetByApplicationIdAsync(int applicationId);
    Task SaveChangesAsync();
}
