We're continuing our implementation of Minilogue XD Schema & Guardrails by implementing task group number 2:

## Implement this task and its sub-tasks:

### Validation Service

#### Task Group 2: Patch Validation API
**Assigned implementer:** api-engineer  
**Dependencies:** Task Group 1

- [ ] 2.0 Implement validator service
  - [ ] 2.1 Write 3-6 focused tests covering: valid patch returns `valid: true`; invalid enum returns specific error; out-of-range numeric triggers error; missing section reports field-specific guidance.
  - [ ] 2.2 Create a schema loading utility (e.g., `SchemaProvider`) to read the artifact and expose lookup helpers with caching.
  - [ ] 2.3 Implement validation logic (`MinilogueXdPatchValidator`) leveraging the schema for type/range/enum checks and assembling structured error responses.
  - [ ] 2.4 Add `POST /api/v1/minilogue-xd/patches/validate` endpoint that invokes the validator and returns `200` with `{ "valid": true }` or `400` with `{ "valid": false, "errors": [...] }`.
  - [ ] 2.5 Document the endpoint contract (input shape, error format, versioning) in API docs or inline README.
  - [ ] 2.6 Run only the tests from 2.1 to confirm endpoint and validator behavior.

**Acceptance Criteria:**
- Endpoint responds per specification with actionable error payloads.
- Validator blocks invalid patches and allows compliant ones.
- Tests from 2.1 pass consistently.
- API documentation reflects request/response schema and dependency on the canonical artifact.

## Understand the context

Read @agent-os/specs/2025-10-18-minilogue-xd-schema-guardrails/spec.md to understand the context for this spec and where the current task fits into it.

## Perform the implementation

Implement all tasks assigned to you in your task group.

Focus ONLY on implementing the areas that align with **areas of specialization** (your "areas of specialization" are defined above).

Guide your implementation using:
- **The existing patterns** that you've found and analyzed.
- **User Standards & Preferences** which are defined below.

Self-verify and test your work by:
- Running ONLY the tests you've written (if any) and ensuring those tests pass.
- IF your task involves user-facing UI, and IF you have access to browser testing tools, open a browser and use the feature you've implemented as if you are a user to ensure a user can use the feature in the intended way.


## Update tasks.md task status

In the current spec's `tasks.md` find YOUR task group that's been assigned to YOU and update this task group's parent task and sub-task(s) checked statuses to complete for the specific task(s) that you've implemented.

Mark your task group's parent task and sub-task as complete by changing its checkbox to `- [x]`.

DO NOT update task checkboxes for other task groups that were NOT assigned to you for implementation.


## Document your implementation

Using the task number and task title that's been assigned to you, create a file in the current spec's `implementation` folder called `[task-number]-[task-title]-implementation.md`.

For example, if you've been assigned implement the 3rd task from `tasks.md` and that task's title is "Commenting System", then you must create the file: `agent-os/specs/2025-10-18-minilogue-xd-schema-guardrails/implementation/3-commenting-system-implementation.md`.

Use the following structure for the content of your implementation documentation:

```markdown
# Task [number]: [Task Title]

## Overview
**Task Reference:** Task #[number] from `agent-os/specs/[this-spec]/tasks.md`
**Implemented By:** [Agent Role/Name]
**Date:** [Implementation Date]
**Status:** ✅ Complete | ⚠️ Partial | 🔄 In Progress

### Task Description
[Brief description of what this task was supposed to accomplish]

## Implementation Summary
[High-level overview of the solution implemented - 2-3 short paragraphs explaining the approach taken and why]

## Files Changed/Created

### New Files
- `path/to/file.ext` - [1 short sentence description of purpose]
- `path/to/another/file.ext` - [1 short sentence description of purpose]

### Modified Files
- `path/to/existing/file.ext` - [1 short sentence on what was changed and why]
- `path/to/another/existing/file.ext` - [1 short sentence on what was changed and why]

### Deleted Files
- `path/to/removed/file.ext` - [1 short sentence on why it was removed]

## Key Implementation Details

### [Component/Feature 1]
**Location:** `path/to/file.ext`

[Detailed explanation of this implementation aspect]

**Rationale:** [Why this approach was chosen]

### [Component/Feature 2]
**Location:** `path/to/file.ext`

[Detailed explanation of this implementation aspect]

**Rationale:** [Why this approach was chosen]

## Database Changes (if applicable)

### Migrations
- `[timestamp]_[migration_name].rb` - [What it does]
  - Added tables: [list]
  - Modified tables: [list]
  - Added columns: [list]
  - Added indexes: [list]

### Schema Impact
[Description of how the schema changed and any data implications]

## Dependencies (if applicable)

### New Dependencies Added
- `package-name` (version) - [Purpose/reason for adding]
- `another-package` (version) - [Purpose/reason for adding]

### Configuration Changes
- [Any environment variables, config files, or settings that changed]

## Testing

### Test Files Created/Updated
- `path/to/test/file_spec.rb` - [What is being tested]
- `path/to/feature/test_spec.rb` - [What is being tested]

### Test Coverage
- Unit tests: [✅ Complete | ⚠️ Partial | ❌ None]
- Integration tests: [✅ Complete | ⚠️ Partial | ❌ None]
- Edge cases covered: [List key edge cases tested]

### Manual Testing Performed
[Description of any manual testing done, including steps to verify the implementation]

## User Standards & Preferences Compliance

In your instructions, you were provided with specific user standards and preferences files under the "User Standards & Preferences Compliance" section. Document how your implementation complies with those standards.

Keep it brief and focus only on the specific standards files that were applicable to your implementation tasks.

For each RELEVANT standards file you were instructed to follow:

### [Standard/Preference File Name]
**File Reference:** `path/to/standards/file.md`

**How Your Implementation Complies:**
[1-2 Sentences to explain specifically how your implementation adheres to the guidelines, patterns, or preferences outlined in this standards file. Include concrete examples from your code.]

**Deviations (if any):**
[If you deviated from any standards in this file, explain what, why, and what the trade-offs were]

---

*Repeat the above structure for each RELEVANT standards file you were instructed to follow*

## Integration Points (if applicable)

### APIs/Endpoints
- `[HTTP Method] /path/to/endpoint` - [Purpose]
  - Request format: [Description]
  - Response format: [Description]

### External Services
- [Any external services or APIs integrated]

### Internal Dependencies
- [Other components/modules this implementation depends on or interacts with]

## Known Issues & Limitations

### Issues
1. **[Issue Title]**
   - Description: [What the issue is]
   - Impact: [How significant/what it affects]
   - Workaround: [If any]
   - Tracking: [Link to issue/ticket if applicable]

### Limitations
1. **[Limitation Title]**
   - Description: [What the limitation is]
   - Reason: [Why this limitation exists]
   - Future Consideration: [How this might be addressed later]

## Performance Considerations
[Any performance implications, optimizations made, or areas that might need optimization]

## Security Considerations
[Any security measures implemented, potential vulnerabilities addressed, or security notes]

## Dependencies for Other Tasks
[List any other tasks from the spec that depend on this implementation]

## Notes
[Any additional notes, observations, or context that might be helpful for future reference]
```


## User Standards & Preferences Compliance

IMPORTANT: Ensure that your implementation work is ALIGNED and DOES NOT CONFLICT with the user's preferences and standards as detailed in the following files:

@agent-os/standards/global/coding-style.md
@agent-os/standards/global/commenting.md
@agent-os/standards/global/conventions.md
@agent-os/standards/global/error-handling.md
@agent-os/standards/global/tech-stack.md
@agent-os/standards/global/validation.md
@agent-os/standards/backend/api.md
@agent-os/standards/backend/migrations.md
@agent-os/standards/backend/models.md
@agent-os/standards/backend/queries.md
@agent-os/standards/testing/test-writing.md
