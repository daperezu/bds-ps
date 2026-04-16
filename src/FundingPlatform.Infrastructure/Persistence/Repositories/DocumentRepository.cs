using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Interfaces;

namespace FundingPlatform.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;

    public DocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Document document)
    {
        await _context.Documents.AddAsync(document);
    }
}
