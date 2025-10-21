using MinilogueXdValidation.Api.Persistence.Entities;

namespace MinilogueXdValidation.Api.Persistence.Repositories;

public interface IKnowledgeDocumentRepository
{
    Task<KnowledgeDocument?> FindByChecksumAsync(string checksum, CancellationToken cancellationToken);
    Task<bool> ExistsForSourceAsync(string sourceName, CancellationToken cancellationToken);
    Task CreateAsync(KnowledgeDocument document, CancellationToken cancellationToken);
}
