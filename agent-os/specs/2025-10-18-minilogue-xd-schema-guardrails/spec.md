# Specification: Minilogue XD Schema & Guardrails

## Goal
Deliver an authoritative, source-controlled schema for all Minilogue XD voice parameters and expose an API validator that blocks invalid patches before they reach downstream tooling.

## User Stories
- As an orchestration service, I want to submit generated Minilogue XD patches for validation so that only schema-compliant settings proceed to users.
- As a backend engineer, I want a versioned schema artifact in git so that I can audit and evolve parameter definitions alongside the codebase.
- As a QA engineer, I want structured error feedback for invalid patches so that I can troubleshoot failing generations quickly.

## Core Requirements
### Functional Requirements
- Provide a canonical schema artifact enumerating every Minilogue XD voice parameter (names, data types, ranges, enumerations, modulation sources) sourced from the official manual, excluding global system settings.
- Implement a POST API endpoint that accepts a complete patch payload, validates it against the schema, and returns either `valid: true` or `valid: false` with per-field error details (field, received value, guidance).
- Ensure the validator blocks downstream persistence or delivery whenever the payload fails schema checks.
- Document schema structure, validation rules, and API contract so other services can integrate without reverse-engineering.

### Non-Functional Requirements
- Maintain schema artifacts in source control with clear versioning (e.g., semantic version comments) to support diff reviews.
- Load and validate a single patch in well under one second under typical load; no strict SLO beyond responsiveness suitable for synchronous API usage.
- Follow REST and error-handling conventions (HTTP 200 for success, 400 for validation failures) while providing actionable, non-verbose messages.
- Ensure logging is ready for future telemetry (e.g., structured log entries for validation failures) without introducing new observability dependencies in this MVP.

## Visual Design
- No mockups provided; scope limited to API behavior and schema artifacts. Downstream consumers may implement text-based tooling separately.

## Reusable Components
### Existing Code to Leverage
- None identified in the current repository; this feature starts from a greenfield backend surface.

### New Components Required
- `schemas/minilogue-xd/voice-parameters.json` (or equivalent) to hold the canonical schema; required because no reusable schema artifact exists.
- Backend validator module (e.g., `MinilogueXdPatchValidator`) to load the schema, perform validations, and format error output.
- Minimal API endpoint (e.g., `POST /api/v1/minilogue-xd/patches/validate`) to expose validation functionality to other services.

## Technical Approach
- Database: No new tables or persistence required; schema lives as versioned static JSON/YAML in the repository. Consider including a checksum or version field within the artifact for consumers.
- API: Add a versioned REST endpoint under `/api/v1/minilogue-xd/patches/validate`. Request body should include full patch data (oscillators, envelopes, LFOs, effects, motion lanes). Responses return `{ "valid": true }` on success or `{ "valid": false, "errors": [ { "field": "oscillator1.wave", "value": "triangle", "message": "Allowed values: saw, square, noise" } ] }` on failure. Adhere to project error-handling conventions, using HTTP 200 for valid patches and 400 with structured payload for invalid ones.
- Frontend: No UI work in scope; document how future text-based tools can call the API.
- Testing: Add unit tests covering schema loading, happy-path validation, and representative invalid cases. Add integration tests for the API endpoint (e.g., using `dotnet test`) to verify HTTP status codes and payload formats. Include fixture patches sourced from manual examples to protect against regressions.

## Out of Scope
- Global system settings, multi-device support, batch validation pipelines, auto-correction, or auto-tuning enhancements.
- UI validators (web or CLI) beyond documenting the API usage pattern.
- Telemetry pipelines, rate limiting, or advanced performance guarantees.

## Success Criteria
- Schema artifact merged with clear versioning and covering all voice parameters referenced in the Minilogue XD manual.
- Validation API deployed (or ready for deployment) that consistently rejects out-of-schema inputs and returns actionable error details.
- Automated tests demonstrating both acceptance of valid patches and rejection of invalid parameter ranges or enum values.
