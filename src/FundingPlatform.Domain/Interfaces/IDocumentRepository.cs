using FundingPlatform.Domain.Entities;

namespace FundingPlatform.Domain.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(Document document);
}
