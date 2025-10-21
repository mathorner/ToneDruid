### Summary
Introduced persistence primitives for knowledge documents, retrieval requests, and feedback, including SQL migrations, domain entities with validation, and dedicated fixtures/tests to validate core constraints.

### Files Modified
- `ToneDruid.sln` - Added `KnowledgeStore.Tests` project to the solution.
- `src/MinilogueXdValidation.Api/MinilogueXdValidation.Api.csproj` - Ensured SQL migrations copy to the build output.
- `src/MinilogueXdValidation.Api/Persistence/Entities/KnowledgeDocument.cs` - Added knowledge document entity with validation and timestamp helpers.
- `src/MinilogueXdValidation.Api/Persistence/Entities/RetrievalRequest.cs` - Added retrieval request entity capturing query metadata and timestamps.
- `src/MinilogueXdValidation.Api/Persistence/Entities/FeedbackRating.cs` - Added feedback enum plus parsing helpers for persisted values.
- `src/MinilogueXdValidation.Api/Persistence/Entities/Feedback.cs` - Added feedback entity with note limits and timestamp helpers.
- `src/MinilogueXdValidation.Api/Persistence/Migrations/001_create_knowledge_store.sql` - Created schema migration for knowledge_documents, retrieval_requests, and feedback tables with indexes.
- `src/MinilogueXdValidation.Api/Persistence/Migrations/KnowledgeStoreMigrator.cs` - Added migrator utility to apply SQL migrations programmatically.
- `agent-os/specs/2025-10-19-device-knowledge-grounding-citations/tasks.md` - Marked Task Group 1 items complete.
- `tests/KnowledgeStore.Tests/KnowledgeStore.Tests.csproj` - New test project for persistence entities.
- `tests/KnowledgeStore.Tests/KnowledgeStoreFixtures.cs` - Added fixtures for documents, retrieval requests, and feedback.
- `tests/KnowledgeStore.Tests/KnowledgeStoreEntityTests.cs` - Added four focused tests covering entity validation behaviors.

## Implementation Details

### Overview
Created a lightweight persistence layer outlining entities and SQL migrations for the knowledge grounding feature, together with fixtures and tests to validate required metadata, logging timestamps, and feedback rating handling.

### KnowledgeDocument entity
**Location:** `src/MinilogueXdValidation.Api/Persistence/Entities/KnowledgeDocument.cs`

Defines factory validation for required metadata (source, blob path, checksum, positive page count) and maintains UTC timestamps for auditing.

**Rationale:** Ensure ingested documents always provide canonical metadata and timestamps before connecting storage or retrieval logic.

### RetrievalRequest entity
**Location:** `src/MinilogueXdValidation.Api/Persistence/Entities/RetrievalRequest.cs`

Captures retrieval query text, optional best-match document links/snippets, and normalises timestamps to UTC.

**Rationale:** Persist query history consistently so later services can attach citations, analytics, and feedback.

### Feedback entity & rating helpers
**Location:** `src/MinilogueXdValidation.Api/Persistence/Entities/Feedback.cs`, `FeedbackRating.cs`

Introduces strongly typed feedback rating with parsing helpers, enforces note length, and ensures timestamps are tracked for updates.

**Rationale:** Guarantee downstream services only persist known rating values while allowing optional notes and future suggestion IDs.

### Knowledge store migration & migrator
**Location:** `src/MinilogueXdValidation.Api/Persistence/Migrations/001_create_knowledge_store.sql`, `KnowledgeStoreMigrator.cs`

SQL migration creates three core tables with primary keys, foreign keys, and supporting indexes. The migrator loads SQL files from output directories and executes them against a provided connection.

**Rationale:** Provide an initial, repeatable schema setup that future ingestion/retrieval work can build upon without bringing in an ORM yet.

### Test fixtures & unit tests
**Location:** `tests/KnowledgeStore.Tests/**/*`

Added new xUnit project with fixtures for sample domain entities and four focused unit tests ensuring metadata validation, timestamp logging, and rating parsing throw for invalid inputs.

**Rationale:** Align with the limited-test mandate while covering the most critical validation behaviours for the new persistence layer.

## Database Changes

### Migrations
- `001_create_knowledge_store.sql` - Creates `knowledge_documents`, `retrieval_requests`, and `grounding_feedback` tables with supporting indexes and constraints.
- Added tables: knowledge_documents, retrieval_requests, feedback
  - Modified tables: _(none)_
  - Added columns: As defined in migration (metadata, foreign keys, timestamps, rating)
  - Added indexes: source/blob unique combo, checksum unique, retrieval timestamp, retrieval doc reference, feedback request & rating indexes

### Schema Impact
Establishes baseline schema for document cataloguing, retrieval logging, and feedback capture, enabling future ingestion/retrieval flows to store metadata and feedback in a structured manner.

## Dependencies

### New Dependencies Added
None (migration executed via existing BCL ADO.NET primitives; no ORM introduced yet).

### Configuration Changes
- Ensured SQL migration files are copied to the build output for runtime execution by the migrator.

## Testing

### Test Files Created/Updated
- `tests/KnowledgeStore.Tests/KnowledgeStoreEntityTests.cs` - Validates document metadata requirements, retrieval timestamp logging, and feedback rating parsing.

### Test Coverage
- Unit tests: ✅ Complete (4 focused tests)
- Integration tests: ❌ None (deferred until API wiring tasks)
- Edge cases covered: Missing metadata, zero page count, timestamp normalisation, unsupported rating strings

### Manual Testing Performed
None (unit-level coverage sufficient for schema primitives).

## User Standards & Preferences Compliance

### Coding Style
**File Reference:** `agent-os/standards/global/coding-style.md`

**How Your Implementation Complies:** Adopted expressive method names, guard clauses, and immutable entity factories to maintain clarity and readability.

### Conventions
**File Reference:** `agent-os/standards/global/conventions.md`

**How Your Implementation Complies:** Followed clear project structure (`Persistence` folder), named migrations descriptively, and avoided extraneous dependencies to keep architecture predictable.

### Error Handling
**File Reference:** `agent-os/standards/global/error-handling.md`

**How Your Implementation Complies:** Entities throw targeted `ArgumentException`/`ArgumentOutOfRangeException` with actionable messages, enabling upstream callers to react gracefully.

### Validation
**File Reference:** `agent-os/standards/global/validation.md`

**How Your Implementation Complies:** Validation occurs at creation time with allowlist checks (for ratings) and explicit type/range enforcement before persisting entities.

### Backend Models
**File Reference:** `agent-os/standards/backend/models.md`

**How Your Implementation Complies:** Ensured entities expose clear names, enforce data constraints in code, and prepare for database-level enforcement through schema definitions.

### Backend Migrations
**File Reference:** `agent-os/standards/backend/migrations.md`

**How Your Implementation Complies:** Created a single focused migration with explicit indexes/constraints and provided a reversible SQL file suitable for version control.

### Backend Queries
**File Reference:** `agent-os/standards/backend/queries.md`

**How Your Implementation Complies:** Migration and entity design favour indexed lookups on common access paths (source/blob, retrieval timestamps, feedback by request) to support efficient querying later.

### Test Writing
**File Reference:** `agent-os/standards/testing/test-writing.md`

**How Your Implementation Complies:** Added four concise unit tests covering the most critical validation behaviors without over-expanding surface area, matching the limited-test guidance.

## Integration Points

### APIs/Endpoints
- _(None implemented yet; upcoming task groups will consume these entities.)_

### External Services
- _(None)_

### Internal Dependencies
- Entities intended for later use by ingestion/retrieval services; no runtime wiring yet.

## Known Issues & Limitations

### Issues
None identified at this stage.

### Limitations
1. **No runtime migration orchestration yet**
   - Reason: Future task groups will wire the migrator into startup/configuration.
   - Future Consideration: Integrate `KnowledgeStoreMigrator.ApplyAsync` into application startup with appropriate connection management.

## Performance Considerations
Indexes on key lookup columns (source/blob, checksum, retrieval timestamps, feedback retrieval id) anticipate common access paths once ingestion/retrieval flows are active.

## Security Considerations
Enforced strict validation on inputs prior to persistence to mitigate malformed document metadata or unbounded feedback notes.

## Dependencies for Other Tasks
- Document ingestion and retrieval API work (Task Groups 2 & 3) depend on these entities, schema, and fixtures.

## Notes
Fixtures in `KnowledgeStore.Tests` provide a baseline for future integration tests that will span ingestion → retrieval → feedback workflows.
- `001_create_knowledge_store.sql` - Creates `knowledge_documents`, `retrieval_requests`, and `feedback` tables with supporting indexes and constraints.
