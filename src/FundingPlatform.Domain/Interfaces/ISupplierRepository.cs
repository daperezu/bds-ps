using FundingPlatform.Domain.Entities;

namespace FundingPlatform.Domain.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByLegalIdAsync(string legalId);
    Task AddAsync(Supplier supplier);
    Task<Supplier?> GetByIdAsync(int id);
}
