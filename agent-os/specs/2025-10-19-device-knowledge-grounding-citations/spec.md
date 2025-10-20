# Specification: Device Knowledge Grounding + Citations

## Goal
Deliver a self-contained knowledge ingestion, retrieval, and feedback capability so future prompt-to-parameter flows can attach citations without additional groundwork.

## User Stories
- As the Tone Druid builder, I want to ingest trusted Minilogue XD references so the app can ground future guidance in authoritative material.
- As an orchestration engineer, I want an API that returns the best-matching citation for a prompt fragment so I can plug it into the upcoming Prompt → Parameters flow.
- As the synth owner, I want to review retrieved citations and signal whether they helped, so the system can improve grounding quality over time.

## Core Requirements
### Functional Requirements
- Ingest the Minilogue XD manual PDF and Sound-on-Sound Synth Secrets PDF into a knowledge store with straightforward local setup.
- Provide a retrieval endpoint that accepts free-form queries (e.g., partial prompt text or parameter names) and returns top passages with document links and snippet context.
- Expose a lightweight UI console for manual retrieval experiments that shows citation links and allows thumbs up/down plus optional feedback text.
- Persist user feedback entries tied to retrieval sessions so future orchestration work can learn from the signals.

### Non-Functional Requirements
- Keep ingestion and retrieval workflows simple enough for single-user development (Azure Blob Storage with local emulator or dev connection string).
- Maintain clear validation and error messaging per backend standards; ensure endpoints fail fast on invalid inputs.
- Responses should arrive within a few seconds for typical queries; log latency and failures for troubleshooting.
- Avoid adding dependencies beyond storage, parsing, and a basic retrieval/indexing library suitable for PDFs.

## Visual Design
- No dedicated mockups; create a simple “Knowledge Grounding Console” view for querying documents and viewing results.
- Display returned passages with a “View source” link that opens the relevant PDF page/anchor in a new tab.
- Include thumbs up/down controls with an optional note field adjacent to each retrieval result.

## Reusable Components
### Existing Code to Leverage
- Leverage any shared backend utilities for storage access, configuration, or logging once identified during implementation (no spec-level dependencies today).

### New Components Required
- Document ingestion script/service that uploads PDFs to Azure Blob Storage (or emulator) and captures metadata.
- Retrieval service that builds a searchable index (page-level chunks) and exposes query operations.
- Citation formatting helper that assembles document URL, page indicator, and snippet text for clients.
- Feedback API and persistence layer capturing retrieval outcome, rating, optional note, and timestamps.
- Frontend console module to issue retrieval queries and submit feedback.

## Technical Approach
- Database: Create `knowledge_documents` (id, source name, blob path, checksum, page count, timestamps) and `grounding_feedback` (id, retrieval_request_id, rating enum, note, optional future suggestion_id, timestamps) tables per model/migration standards; add indexes on document source and retrieval linkage fields.
- API: Implement REST endpoints such as `POST /api/knowledge/ingest` (admin upload), `POST /api/knowledge/query` (returns top passages, citation details), and `POST /api/knowledge/feedback` (records thumbs up/down). Enforce input validation and consistent status codes.
- Frontend: Add a console view in the existing app shell with a query input, results list showing snippet + citation link, and inline feedback controls; reuse shared components where practical.
- Testing: Provide unit tests for ingestion parsing, retrieval scoring/fallbacks, citation formatting, and feedback validation; add integration tests covering end-to-end ingestion → query → feedback using the console.

## Out of Scope
- Wiring citations directly into the prompt-to-parameter pipeline (handled in Spec 3).
- Adding additional document sources or automated refresh scheduling.
- Advanced citation UX (multi-reference aggregation, rich previews) or analytics dashboards.
- Multi-user access control or collaborative feedback features.

## Success Criteria
- Both PDFs ingest successfully with metadata persisted and accessible via the query endpoint.
- The retrieval API returns passages with working document links for representative queries, falling back gracefully when no match exists.
- Console UI allows manual queries and records feedback entries viewable through the API or database.
- Latency stays acceptable for single-user development, and logs capture ingestion/retrieval/feedback errors for debugging.
