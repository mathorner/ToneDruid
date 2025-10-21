using System.IO;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MinilogueXdValidation.Api.Models.Knowledge;
using MinilogueXdValidation.Api.Persistence.Repositories;
using MinilogueXdValidation.Api.Services;
using MinilogueXdValidation.Api.Services.Knowledge;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SchemaOptions>(builder.Configuration.GetSection("MinilogueXdSchema"));
builder.Services.PostConfigure<SchemaOptions>(options =>
{
    if (!string.IsNullOrWhiteSpace(options.SchemaPath))
    {
        return;
    }

    // Check both the project root and the compiled output folder so test hosts can resolve the schema.
    var candidatePaths = new[]
    {
        builder.Environment.ContentRootPath,
        AppContext.BaseDirectory
    };

    foreach (var basePath in candidatePaths)
    {
        var resolved = ResolveSchemaPath(basePath);
        if (resolved is not null)
        {
            options.SchemaPath = resolved;
            return;
        }
    }

    throw new InvalidOperationException(
        "Unable to locate voice-parameters.json. Configure MinilogueXdSchema:SchemaPath or ensure the schemas directory is checked out.");

    static string? ResolveSchemaPath(string startingDirectory)
    {
        if (string.IsNullOrWhiteSpace(startingDirectory))
        {
            return null;
        }

        var directory = new DirectoryInfo(Path.GetFullPath(startingDirectory));

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "schemas", "minilogue-xd", "voice-parameters.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
});

builder.Services.Configure<DocumentIngestionOptions>(builder.Configuration.GetSection(DocumentIngestionOptions.ConfigurationSectionName));
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IOptions<DocumentIngestionOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.StorageConnectionString))
    {
        throw new InvalidOperationException("DocumentIngestion:StorageConnectionString must be configured.");
    }

    return new BlobServiceClient(options.StorageConnectionString);
});

var knowledgeStoreConnection = builder.Configuration.GetConnectionString("KnowledgeStore");
if (string.IsNullOrWhiteSpace(knowledgeStoreConnection))
{
    throw new InvalidOperationException("Connection string 'KnowledgeStore' must be configured.");
}

builder.Services.AddSingleton(_ =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(knowledgeStoreConnection);
    return dataSourceBuilder.Build();
});

builder.Services.AddSingleton<SchemaProvider>();
builder.Services.AddSingleton<MinilogueXdPatchValidator>();
builder.Services.AddSingleton<IDocumentParser, PdfDocumentParser>();
builder.Services.AddSingleton<IDocumentIngestionStorage, AzureBlobDocumentIngestionStorage>();
builder.Services.AddScoped<IKnowledgeDocumentRepository, KnowledgeDocumentRepository>();
builder.Services.AddScoped<DocumentIngestionService>();

var app = builder.Build();

app.MapPost("/api/v1/minilogue-xd/patches/validate", (JsonElement patch, MinilogueXdPatchValidator validator) =>
    {
        var result = validator.Validate(patch);
        return result.IsValid
            ? Results.Ok(new { valid = true })
            : Results.BadRequest(new { valid = false, errors = result.Errors });
    })
    .WithName("ValidateMinilogueXdPatch")
    .Produces(StatusCodes.Status200OK, typeof(object))
    .Produces(StatusCodes.Status400BadRequest, typeof(object));

app.MapPost("/api/v1/knowledge/ingest", async (
        [FromForm] KnowledgeIngestionForm form,
        DocumentIngestionService ingestionService,
        ILogger<DocumentIngestionService> logger,
        CancellationToken cancellationToken) =>
    {
        if (form.File is null || form.File.Length == 0)
        {
            return Results.BadRequest(new
            {
                error = "file_missing",
                message = "A PDF file is required for ingestion."
            });
        }

        if (string.IsNullOrWhiteSpace(form.SourceName))
        {
            return Results.BadRequest(new
            {
                error = "source_missing",
                message = "Source name is required."
            });
        }

        await using var buffer = new MemoryStream();
        await form.File.CopyToAsync(buffer, cancellationToken);
        var request = new DocumentIngestionRequest(form.SourceName, form.File.FileName ?? "document.pdf", buffer.ToArray());

        try
        {
            var result = await ingestionService.IngestAsync(request, cancellationToken);
            var response = new DocumentIngestionResponse(
                result.Document.Id,
                result.Document.SourceName,
                result.BlobLocation,
                result.IndexLocation,
                result.Document.Checksum,
                result.Document.PageCount,
                result.Document.CreatedAt);

            return Results.Created($"/api/v1/knowledge/ingest/{result.Document.Id}", response);
        }
        catch (DuplicateDocumentException ex)
        {
            logger.LogWarning(ex, "Duplicate knowledge document ingestion attempt for {SourceName}.", form.SourceName);
            return Results.Conflict(new
            {
                error = "duplicate_document",
                message = ex.Message,
                documentId = ex.ExistingDocument.Id,
                checksum = ex.ExistingDocument.Checksum
            });
        }
        catch (InvalidDocumentException ex)
        {
            logger.LogWarning(ex, "Invalid knowledge document ingestion request for {SourceName}.", form.SourceName);
            return Results.BadRequest(new
            {
                error = "invalid_document",
                message = ex.Message
            });
        }
    })
    .WithName("IngestKnowledgeDocument")
    .Produces(StatusCodes.Status201Created, typeof(DocumentIngestionResponse))
    .Produces(StatusCodes.Status400BadRequest, typeof(object))
    .Produces(StatusCodes.Status409Conflict, typeof(object));

app.Run();

public partial class Program;

internal sealed class KnowledgeIngestionForm
{
    [FromForm(Name = "sourceName")]
    public string? SourceName { get; init; }

    [FromForm(Name = "file")]
    public IFormFile? File { get; init; }
}
