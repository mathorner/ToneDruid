# Task Breakdown: Device Knowledge Grounding + Citations

## Overview
Total Tasks: 18  
Assigned roles: database-engineer, api-engineer, ui-designer, testing-engineer

## Task List

### Database Layer

#### Task Group 1: Knowledge Storage Schema
**Assigned implementer:** database-engineer  
**Dependencies:** None

- [x] 1.0 Define persistence for documents, retrieval requests, and feedback
  - [x] 1.1 Write 4 focused tests covering required document metadata, retrieval request logging, and feedback rating validation
- [x] 1.2 Create migrations for `knowledge_documents` (source, blob path, checksum, page count), `retrieval_requests` (query text, best_match_document_id, created_at), and `feedback` (retrieval_request_id, optional suggestion_id, rating enum, note, timestamps)
- [x] 1.3 Implement corresponding models with constraints (presence checks, enum validation, foreign keys with cascading behavior)
- [x] 1.4 Add indexes on `knowledge_documents.source`, `retrieval_requests.created_at`, and `feedback.retrieval_request_id`
  - [x] 1.5 Seed fixtures or factory helpers that create sample document and retrieval records for manual testing
  - [x] 1.6 Run only the tests from 1.1 to validate schema behavior

**Acceptance Criteria:**
- Migrations apply and roll back cleanly
- Models enforce validations and associations
- Tests from 1.1 pass without running the full suite

### Backend Services & API

#### Task Group 2: Document Ingestion Service
**Assigned implementer:** api-engineer  
**Dependencies:** Task Group 1

- [x] 2.0 Implement ingestion pipeline
  - [x] 2.1 Write 3-5 targeted tests covering ingestion happy path, duplicate upload detection, and invalid file handling
  - [x] 2.2 Implement service/worker to upload PDFs to Azure Blob Storage (or emulator) and register metadata in `knowledge_documents`
  - [x] 2.3 Parse PDFs into page-level chunks or embeddings and persist index artifacts needed by the retrieval helper
  - [x] 2.4 Expose minimal admin endpoint/CLI hook (e.g., `POST /api/knowledge/ingest`) for loading the two seed documents with validation and clear error responses
  - [x] 2.5 Log ingestion progress and failures following existing error-handling standards
  - [x] 2.6 Run only the tests from 2.1 to confirm ingestion behavior

**Acceptance Criteria:**
- Uploads store blobs and metadata correctly
- Duplicate or invalid inputs produce clear error responses
- Tests from 2.1 pass

#### Task Group 3: Retrieval API & Citation Formatting
**Assigned implementer:** api-engineer  
**Dependencies:** Task Group 2

- [ ] 3.0 Deliver query endpoint returning citation-ready results
  - [ ] 3.1 Write 3-5 tests for retrieval helper covering best-match selection, no-match fallback, and malformed request validation
  - [ ] 3.2 Implement retrieval service (embedding or keyword index) that returns top passages with document id, page number, snippet, and storage link
  - [ ] 3.3 Persist each query in `retrieval_requests`, capturing the chosen document reference and timestamp
  - [ ] 3.4 Add `POST /api/knowledge/query` endpoint that accepts query text, invokes retrieval service, formats citation links, and returns structured results
  - [ ] 3.5 Run only the tests from 3.1

**Acceptance Criteria:**
- Endpoint returns citations when matches exist and clear messaging when none found
- Retrieval requests are logged for every query
- Tests from 3.1 pass

#### Task Group 4: Feedback Capture API
**Assigned implementer:** api-engineer  
**Dependencies:** Task Group 3

- [ ] 4.0 Implement feedback workflow
  - [ ] 4.1 Write 3-5 tests covering thumbs up/down submissions, optional note validation, and error cases (missing retrieval request, invalid rating)
  - [ ] 4.2 Create `POST /api/knowledge/feedback` endpoint that records feedback tied to `retrieval_request_id` (with optional future `suggestion_id`)
  - [ ] 4.3 Provide lightweight review endpoint or query for aggregating feedback during manual QA
  - [ ] 4.4 Run only the tests from 4.1

**Acceptance Criteria:**
- Endpoint accepts rating + optional note and persists to `grounding_feedback`
- Invalid inputs return descriptive errors
- Tests from 4.1 pass

### Frontend Components

#### Task Group 5: Knowledge Grounding Console UI
**Assigned implementer:** ui-designer  
**Dependencies:** Task Group 3, Task Group 4

- [ ] 5.0 Build console for manual retrieval and feedback
  - [ ] 5.1 Write 3-5 focused component tests verifying query submission, citation rendering, and feedback interaction behavior
  - [ ] 5.2 Create console view with query input, result list showing snippet + “View source” link, and status states (loading, empty, error)
  - [ ] 5.3 Add thumbs up/down controls with mutually exclusive selection and optional note field per result
  - [ ] 5.4 Wire submission to `POST /api/knowledge/feedback`, handling optimistic UI or loading state
  - [ ] 5.5 Ensure responsive layout per frontend standards
  - [ ] 5.6 Run only the tests from 5.1

**Acceptance Criteria:**
- Console displays citations when available and opens links in new tab/window
- Feedback controls enforce rating selection before submit
- Component tests from 5.1 pass

### Testing & Verification

#### Task Group 6: Integrated QA Sweep
**Assigned implementer:** testing-engineer  
**Dependencies:** Task Groups 1-5

- [ ] 6.0 Consolidate coverage
  - [ ] 6.1 Review tests from Task Groups 1-5 for coverage gaps specific to ingestion → query → feedback workflow
  - [ ] 6.2 Add up to 6 targeted integration/E2E tests ensuring the flow: ingest PDFs → submit query → receive citation → record feedback
  - [ ] 6.3 Validate error handling by simulating missing document index, retrieval failure, and invalid feedback submission
  - [ ] 6.4 Run feature-specific suite (tests from 1.1, 2.1, 3.1, 4.1, 5.1, and new tests in 6.2) without executing the entire project suite

**Acceptance Criteria:**
- Feature-specific tests all pass
- End-to-end ingestion-to-feedback workflow covered
- Logged issues or gaps are documented for follow-up

## Execution Order
1. Task Group 1: Knowledge Storage Schema
2. Task Group 2: Document Ingestion Service
3. Task Group 3: Retrieval API & Citation Formatting
4. Task Group 4: Feedback Capture API
5. Task Group 5: Knowledge Grounding Console UI
6. Task Group 6: Integrated QA Sweep
