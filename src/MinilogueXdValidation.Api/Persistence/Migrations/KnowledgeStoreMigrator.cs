using System.Data;
using System.Data.Common;

namespace MinilogueXdValidation.Api.Persistence.Migrations;

public static class KnowledgeStoreMigrator
{
    private static readonly string[] MigrationFiles =
    {
        "001_create_knowledge_store.sql"
    };

    public static async Task ApplyAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        if (connection is null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var migration in MigrationFiles)
        {
            var sql = await LoadSqlAsync(migration, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(sql))
            {
                continue;
            }

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<string> LoadSqlAsync(string fileName, CancellationToken cancellationToken)
    {
        var baseDirectories = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        foreach (var baseDirectory in baseDirectories)
        {
            var candidate = Path.Combine(baseDirectory, "Persistence", "Migrations", fileName);
            if (File.Exists(candidate))
            {
                return await File.ReadAllTextAsync(candidate, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new FileNotFoundException($"Unable to locate migration SQL file '{fileName}'.", fileName);
    }
}
