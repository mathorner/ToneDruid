# Spec Requirements: minilogue-xd-schema-guardrails

## Initial Description
Minilogue XD Schema & Guardrails — Define the canonical schema for all Minilogue XD parameters (names, ranges, modulation sources) and provide a validation service with a minimal API validator; all generated settings must pass this gate.

## Requirements Discussion

### First Round Questions

**Q1:** I'm assuming the canonical schema should enumerate every Minilogue XD voice parameter (oscillators, envelopes, LFOs, effects, motion lanes) with min/max ranges and valid enums pulled from the official manual—do you also want to cover global system settings or is that out of scope?
**Answer:** please exclude global system settings

**Q2:** I'm thinking the guardrail service should expose both an API endpoint and a reusable library so other services can validate generated patches server-side before persistence—should we prioritize one integration surface over the other?
**Answer:** just an API please

**Q3:** For the “minimal UI validator,” is the goal a developer-facing diagnostic page embedded in the existing React admin shell, or do you prefer a standalone tool (e.g., a simple form with copy/paste JSON validation output)?
**Answer:** I thought I had changed this so it didn't say UI validator. I think an API validation service would be best

**Q4:** I’m assuming invalid input should yield structured error details (field name, invalid value, guidance) rather than a simple pass/fail flag—do you also need severity levels or auto-corrections when values land just outside bounds?
**Answer:** just error details please. no severity

**Q5:** Should the schema live in source control as a versioned JSON/YAML artifact that downstream tooling can import, or do you want a database-backed representation to allow runtime edits?
**Answer:** schema in source please

**Q6:** I’m planning to derive the schema from the manual plus any internal descriptor mappings—are there authoritative secondary sources (e.g., existing SysEx dumps, community parameter sheets) we should ingest as well?
**Answer:** i haven't been able to find other sources except for the midi implementation so probably best to go from manual

**Q7:** Are there compliance or performance guardrails for validation (e.g., must validate a full patch in under 50 ms, must log all failures with OpenTelemetry) that we should budget for now?
**Answer:** no

**Q8:** Is anything explicitly out of scope for this first iteration (e.g., multi-device support, auto-tuning suggestions, file import/export, batch validation pipelines)?
**Answer:** all those things are out of scope. i'm looking for MVP here

### Existing Code to Reference
No similar existing features identified for reference.

### Follow-up Questions
No follow-up questions were required.

## Visual Assets

### Files Provided:
No visual assets provided.

### Visual Insights:
- User plans to begin with a simple text-based interaction, but no designs or screenshots exist yet.

## Requirements Summary

### Functional Requirements
- Provide a canonical, source-controlled schema that enumerates all Minilogue XD voice parameters (names, ranges, modulation sources, enumerations) while explicitly excluding global system settings.
- Expose a validation API that accepts generated patch data, checks it against the schema, and blocks non-conforming values before downstream persistence or presentation.
- Return structured error details for invalid submissions, including the offending parameter, value, and guidance derived from the manual.
- Ensure the schema artifact is easily imported by other services or tools (e.g., JSON/YAML committed to the repository with clear versioning strategy).

### Reusability Opportunities
- None identified; implementation starts from a greenfield codebase for this feature.

### Scope Boundaries
**In Scope:**
- Voice-parameter schema definition grounded in the official Minilogue XD manual.
- API-only validation surface with structured error payloads.
- Documentation for how other services should integrate with the validation endpoint and schema artifact.

**Out of Scope:**
- Global system settings, multi-device support, auto-tuning, batch validation, file import/export, or other advanced guardrail features.
- UI layers (including graphical or command-line validators) beyond potential future consumption of the API.
- Automated error correction or severity ranking for invalid inputs.

### Technical Considerations
- Primary implementation should align with the backend stack (.NET 9 Minimal API) and existing validation standards documented in `agent-os/standards/global/validation.md`.
- Schema values should be sourced from the official Minilogue XD manual (and MIDI implementation notes when needed) to remain authoritative.
- Maintain schema artifacts in version control to support diffing, reviews, and reproducibility across environments.
- Plan for structured logging/telemetry to be layered later, but no specific performance or compliance targets are required for the MVP.
