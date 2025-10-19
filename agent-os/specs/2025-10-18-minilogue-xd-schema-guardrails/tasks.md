# Task Breakdown: Minilogue XD Schema & Guardrails

## Overview
Total Tasks: 11  
Assigned roles: database-engineer, api-engineer, testing-engineer

## Task List

### Schema Artifact

#### Task Group 1: Canonical Voice Parameter Schema
**Assigned implementer:** database-engineer  
**Dependencies:** None

- [x] 1.0 Author schema groundwork
  - [x] 1.1 Write 2-4 focused unit tests that load the schema artifact (JSON/YAML) and assert required top-level sections (oscillators, envelopes, LFOs, motion lanes, effects) exist and contain expected keys.
  - [x] 1.2 Extract authoritative parameter details from the Minilogue XD manual and MIDI implementation notes.
  - [x] 1.3 Create `schemas/minilogue-xd/voice-parameters.json` (or equivalent) capturing every voice parameter (name, type, allowed range/enum, modulation sources, documentation reference).
  - [x] 1.4 Add metadata fields (e.g., schema version, checksum placeholder) to support future auditing.
  - [x] 1.5 Ensure schema artifact passes JSON validation/linting and the tests from 1.1.

**Acceptance Criteria:**
- Schema artifact lives in source control with manual-aligned parameter coverage, excluding global system settings.
- Tests from 1.1 pass and confirm schema structure.
- Documentation within the artifact makes integration straightforward (e.g., inline comments or README snippet).

### Validation Service

#### Task Group 2: Patch Validation API
**Assigned implementer:** api-engineer  
**Dependencies:** Task Group 1

- [x] 2.0 Implement validator service
  - [x] 2.1 Write 3-6 focused tests covering: valid patch returns `valid: true`; invalid enum returns specific error; out-of-range numeric triggers error; missing section reports field-specific guidance.
  - [x] 2.2 Create a schema loading utility (e.g., `SchemaProvider`) to read the artifact and expose lookup helpers with caching.
  - [x] 2.3 Implement validation logic (`MinilogueXdPatchValidator`) leveraging the schema for type/range/enum checks and assembling structured error responses.
  - [x] 2.4 Add `POST /api/v1/minilogue-xd/patches/validate` endpoint that invokes the validator and returns `200` with `{ "valid": true }` or `400` with `{ "valid": false, "errors": [...] }`.
  - [x] 2.5 Document the endpoint contract (input shape, error format, versioning) in API docs or inline README.
  - [x] 2.6 Run only the tests from 2.1 to confirm endpoint and validator behavior.

**Acceptance Criteria:**
- Endpoint responds per specification with actionable error payloads.
- Validator blocks invalid patches and allows compliant ones.
- Tests from 2.1 pass consistently.
- API documentation reflects request/response schema and dependency on the canonical artifact.

### Quality Assurance

#### Task Group 3: Feature Test Consolidation
**Assigned implementer:** testing-engineer  
**Dependencies:** Task Groups 1 & 2

- [ ] 3.0 Consolidate validation test coverage
  - [ ] 3.1 Review tests written in 1.1 and 2.1, noting covered behaviors.
  - [ ] 3.2 Identify any remaining critical gaps (e.g., combined error reporting, malformed payload handling) specific to this feature.
  - [ ] 3.3 Add up to 4 supplemental integration tests targeting the highest-risk gaps only.
  - [ ] 3.4 Run feature-specific test suite (tests from 1.1, 2.1, and 3.3) and record results.

**Acceptance Criteria:**
- No more than 4 additional tests added; total feature tests remain within guidelines.
- Critical workflows (valid passes, invalid fails with clear messaging, schema load) are exercised end-to-end.
- All feature-specific tests pass.

## Execution Order
1. Canonical Voice Parameter Schema (Task Group 1)  
2. Patch Validation API (Task Group 2)  
3. Feature Test Consolidation (Task Group 3)
