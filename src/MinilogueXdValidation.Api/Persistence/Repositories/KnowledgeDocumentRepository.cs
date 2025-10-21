using System.Data.Common;
using Dapper;
using MinilogueXdValidation.Api.Persistence.Entities;
using MinilogueXdValidation.Api.Persistence.Migrations;
using Npgsql;

namespace MinilogueXdValidation.Api.Persistence.Repositories;

public sealed class KnowledgeDocumentRepository : IKnowledgeDocumentRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public KnowledgeDocumentRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<KnowledgeDocument?> FindByChecksumAsync(string checksum, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(checksum))
        {
            throw new ArgumentException("Checksum is required.", nameof(checksum));
        }

        const string sql = @"
SELECT
    id,
    source_name AS SourceName,
    blob_path AS BlobPath,
    checksum,
    page_count AS PageCount,
    created_at AS CreatedAt,
    updated_at AS UpdatedAt
FROM knowledge_documents
WHERE checksum = @Checksum
LIMIT 1;";

        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<KnowledgeDocumentRow>(
            new CommandDefinition(sql, new { Checksum = checksum }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        return row?.ToEntity();
    }

    public async Task<bool> ExistsForSourceAsync(string sourceName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            throw new ArgumentException("Source name is required.", nameof(sourceName));
        }

        const string sql = @"SELECT EXISTS(SELECT 1 FROM knowledge_documents WHERE source_name = @SourceName);";

        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { SourceName = sourceName.Trim() }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        return exists;
    }

    public async Task CreateAsync(KnowledgeDocument document, CancellationToken cancellationToken)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        const string sql = @"
INSERT INTO knowledge_documents (id, source_name, blob_path, checksum, page_count, created_at, updated_at)
VALUES (@Id, @SourceName, @BlobPath, @Checksum, @PageCount, @CreatedAt, @UpdatedAt);";

        var parameters = new
        {
            Id = document.Id,
            SourceName = document.SourceName,
            BlobPath = document.BlobPath,
            Checksum = document.Checksum,
            PageCount = document.PageCount,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };

        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await KnowledgeStoreMigrator.ApplyAsync(connection, cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private sealed record KnowledgeDocumentRow(
        Guid Id,
        string SourceName,
        string BlobPath,
        string Checksum,
        int PageCount,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public KnowledgeDocument ToEntity() =>
            KnowledgeDocument.FromPersistence(Id, SourceName, BlobPath, Checksum, PageCount, CreatedAt, UpdatedAt);
    }
}
