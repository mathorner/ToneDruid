using System.IO;
using System.Text.Json;
using MinilogueXdValidation.Api.Services;
using Microsoft.AspNetCore.Http;

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

builder.Services.AddSingleton<SchemaProvider>();
builder.Services.AddSingleton<MinilogueXdPatchValidator>();

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

app.Run();

public partial class Program;
