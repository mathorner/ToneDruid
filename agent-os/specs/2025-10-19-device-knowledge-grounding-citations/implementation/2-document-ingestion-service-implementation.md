### Summary
Implemented the document ingestion pipeline end to end: parsing uploaded PDFs into page-level chunks, uploading source/index artifacts to Azure Blob Storage, persisting metadata in `knowledge_documents`, and exposing an admin ingestion endpoint with targeted logging and validation.

### Files Modified
- `agent-os/specs/2025-10-19-device-knowledge-grounding-citations/tasks.md` – Marked Task Group 2 subtasks complete.
- `src/MinilogueXdValidation.Api/MinilogueXdValidation.Api.csproj` – Added Azure Storage, Npgsql, Dapper, and PdfPig package references.
- `src/MinilogueXdValidation.Api/appsettings.json`, `appsettings.Development.json` – Declared knowledge store connection string and document ingestion storage settings.
- `src/MinilogueXdValidation.Api/Persistence/Entities/KnowledgeDocument.cs` – Added `FromPersistence` factory for repository hydration.
- `src/MinilogueXdValidation.Api/Persistence/Repositories/IKnowledgeDocumentRepository.cs`, `KnowledgeDocumentRepository.cs` – Introduced Dapper-based repository that applies migrations, checks duplicates, and inserts documents.
- `src/MinilogueXdValidation.Api/Services/Knowledge/*` – Added ingestion options, request/result models, parser interface, Azure Blob storage adapter, custom exceptions, and the `DocumentIngestionService`.
- `src/MinilogueXdValidation.Api/Models/Knowledge/DocumentIngestionResponse.cs` – Response contract for the ingestion endpoint.
- `src/MinilogueXdValidation.Api/Program.cs` – Wired configuration, DI registrations, and the `POST /api/v1/knowledge/ingest` endpoint with error handling.
- `tests/KnowledgeStore.Tests/KnowledgeStore.Tests.csproj` – Pulled in logging abstractions for unit tests.
- `tests/KnowledgeStore.Tests/DocumentIngestionServiceTests.cs` – Added four targeted unit tests (happy path, duplicate checksum, invalid file type, zero-page parse).

## Implementation Details

### Overview
The ingestion workflow now validates upload metadata, parses PDFs into page-level text, stores both the raw file and a JSON index in Azure Blob Storage, and persists document metadata to Postgres through a lightweight repository. A minimal admin endpoint accepts multipart form uploads, emits descriptive errors, and logs progress in line with backend standards.

### Ingestion service & exceptions
**Location:** `src/MinilogueXdValidation.Api/Services/Knowledge/DocumentIngestionService.cs`, `DocumentIngestionException.cs`

Creates SHA-256 checksums, guards against duplicate uploads, delegates parsing/storage, persists metadata, and logs progress/failures. Custom `DuplicateDocumentException` and `InvalidDocumentException` surface friendly API errors.

**Rationale:** Centralises ingestion logic so both API and future CLI hooks can reuse the same validations and logging behaviour.

### Azure Blob storage integration
**Location:** `src/MinilogueXdValidation.Api/Services/Knowledge/AzureBlobDocumentIngestionStorage.cs`, `DocumentIngestionOptions.cs`

Uploads PDFs and generated JSON indexes into configurable containers, slugging paths by source and checksum. Serialises page-level payloads using System.Text.Json and annotates uploads with content-type headers.

**Rationale:** Keeps storage concerns encapsulated while supporting emulator/local development through configuration.

### PDF parsing abstraction
**Location:** `src/MinilogueXdValidation.Api/Services/Knowledge/PdfDocumentParser.cs`, `IDocumentParser.cs`, `DocumentParseResult.cs`

Wraps PdfPig to extract page text and counts, making it swappable in tests via the interface.

**Rationale:** Provides page-level artifacts needed by the upcoming retrieval helper without hard-coupling service logic to a specific parser.

### Repository & migration orchestration
**Location:** `src/MinilogueXdValidation.Api/Persistence/Repositories/KnowledgeDocumentRepository.cs`

Uses Dapper + Npgsql to query for existing documents (by checksum or source) and insert new rows after ensuring migrations run. Hydrates entities via the new `KnowledgeDocument.FromPersistence` factory.

**Rationale:** Avoids introducing an ORM while still enforcing database constraints added in Task Group 1.

### API endpoint and configuration
**Location:** `src/MinilogueXdValidation.Api/Program.cs`, `src/MinilogueXdValidation.Api/Models/Knowledge/DocumentIngestionResponse.cs`

Registers options, Blob service client, parser/storage/repository services, and exposes `POST /api/v1/knowledge/ingest`. The endpoint validates multipart form submissions, returns 201/400/409 responses, and shapes output via `DocumentIngestionResponse`.

**Rationale:** Provides the requested admin hook with consistent logging/error responses and makes ingestion accessible without spinning up separate tooling.

## Database Changes
- No schema changes; repository executes the existing migration to ensure tables are present before inserting.

## Dependencies

### New Dependencies Added
- `Azure.Storage.Blobs` – Upload PDFs and JSON indexes to Azure Blob Storage.
- `Dapper` – Lightweight data access for knowledge document persistence.
- `Npgsql` – PostgreSQL data source for the repository.
- `UglyToad.PdfPig` (pre-release 0.1.9-alpha001-patch1) – Extracts page text from PDFs. NuGet resolves to custom `PdfPig.*` dependencies with warning NU1603; functionality verified despite pre-release status and noted for follow-up if stable versions ship.
- `Microsoft.Extensions.Logging.Abstractions` (tests) – Provides `NullLogger` for unit tests.

### Configuration Changes
- Added `ConnectionStrings:KnowledgeStore` and `DocumentIngestion` settings (connection string + container names) to `appsettings.json` and `appsettings.Development.json`.

## Testing

### Tests Added
- `tests/KnowledgeStore.Tests/DocumentIngestionServiceTests.cs` – Covers happy path ingestion, duplicate checksum detection, invalid file extension rejection, and zero-page parse failure handling.

### Tests Executed
- `dotnet test tests/KnowledgeStore.Tests --filter DocumentIngestionServiceTests`

## User Standards & Preferences Compliance
- **Error Handling:** Endpoint returns clear error codes/messages; service logs duplicates and invalid inputs (ref: `agent-os/standards/global/error-handling.md`).
- **Validation:** Guard clauses enforce source name, file type, checksum uniqueness, and page count before persistence (ref: `agent-os/standards/global/validation.md`).
- **Backend API Standards:** Endpoint follows REST conventions with versioned path, appropriate verbs, and status codes (ref: `agent-os/standards/backend/api.md`).
- **Test Writing:** Added only the minimal high-value unit tests mandated by the spec (ref: `agent-os/standards/testing/test-writing.md`).

## Known Issues & Limitations
1. **PdfPig pre-release warnings:** NuGet currently resolves PdfPig via pre-release/custom dependencies (warning NU1603). Documented for future review once stable packages surface.
2. **Storage/DB credentials:** Configuration ships with development placeholders; deployment environments must override with secure values.

## Performance Considerations
- Blob uploads and JSON serialisation stream data to avoid multiple large allocations. Checksums computed via `SHA256.TryHashData` to reduce allocations when possible.
- Repository opens new scoped connections and relies on indexes introduced in Task Group 1 (`checksum`, `source`) for duplicate detection.

## Security Considerations
- Accepts only PDF extensions, trims inputs, and avoids writing files to disk.
- Uses parameterised SQL via Dapper to protect against injection.
- Logging avoids storing raw document contents, only metadata.

## Dependencies for Other Tasks
- Retrieval API (Task Group 3) can consume stored JSON indexes and metadata.
- Feedback API (Task Group 4) relies on the same repository/migrations already initialised here.
