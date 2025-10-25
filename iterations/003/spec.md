# Iteration 003 Specification — Init Baseline Differentials

## 1. Goal & Scope
- Use the Minilogue XD init program as the canonical baseline for all patch generation.
- Ensure Tone Druid only surfaces controls whose values differ from the init program; remove the 5–10 control cap so every deviation returns to the client.
- Preserve existing reasoning metadata (summary, notes, assumptions, confidences) while enriching controls with baseline context.
- Out of scope: changing prompt flow/UI layout beyond differential display affordances, adding Cosmos DB persistence, or introducing retrieval-augmented prompting.

## 2. System Flow Overview
1. User submits a natural-language sound description through the existing prompt UI.
2. Frontend issues `POST /api/v1/patch-request` (same payload as iteration 002) and supplies `x-iteration = 003`.
3. Backend controller loads the init program snapshot (cached in-memory) alongside the control catalog before invoking `PatchGenerationAgent`.
4. `PatchGenerationAgent` composes a system prompt that:
   - States init program values as the starting point.
   - Instructs the model to return JSON describing only controls that deviate from the baseline, with explicit baseline value references for traceability.
   - Removes the max control constraint while discouraging unnecessary changes.
5. Backend validates the JSON response, merges it with the baseline, and enforces that all returned controls differ from init values. Non-deviating entries trigger a `502` with telemetry.
6. Successful responses include additional metadata (baseline value, deviation type) and log telemetry covering deviation counts and token usage.
7. Frontend renders the summary, reasoning, and a differential control table showing current vs. baseline values; telemetry captures deviation metrics.

## 3. Frontend Responsibilities (React + TypeScript)
- **Data Contracts**
  - Extend `PatchControl` to include baseline context:
    ```ts
    export type PatchControl = {
      id: string;
      label: string;
      group: string;
      value: number | string | boolean;
      baselineValue: number | string | boolean;
      deviationType: 'increase' | 'decrease' | 'changed' | 'toggled';
      valueType: 'continuous' | 'enumeration' | 'boolean';
      range?: { min: number; max: number; unit?: string };
      allowedValues?: string[];
      explanation: string;
      confidence: 'low' | 'medium' | 'high';
    };

    export type PatchSuggestion = {
      prompt: string;
      summary: string;
      controls: PatchControl[]; // all deviations (may exceed 10)
      reasoning: {
        intentSummary: string;
        soundDesignNotes: string[];
        assumptions: string[];
      };
      requestId: string;
      clientRequestId: string;
      generatedAtUtc: string;
      model: string;
      baselineProgramId: string;
    };
    ```
  - Update related hooks/services (`usePatchRequest`, response models) and tighten client-side schema guards for unlimited control arrays.
- **UI & State**
  - Enhance the control table to show current vs. baseline values, deviation badges, and highlight magnitude/direction for continuous controls.
  - Remove any UI-level cap on rendered controls; ensure virtualisation or scrolling handles larger datasets gracefully.
  - Add optional grouping by `group` to keep long lists scannable.
- **Telemetry**
  - Log `deviationCount`, `baselineProgramId`, and `hasNonInitDeviation = true` with existing success/failure events.
  - Flag empty results (no deviations) as warnings in Application Insights and surface “Tone Druid kept the init program unchanged.”
- **Error Handling**
  - Differentiate backend `502` due to non-deviating controls; display “Suggestion matches the init program. Try a more specific prompt.” while logging details.
  - Maintain existing friendly error messaging for schema/parsing issues.

## 4. Backend Responsibilities (.NET 9 Web API)
- **Baseline Management**
  - Promote `backend/ToneDruid.Api/Resources/init-program.json` to a strongly typed model (`InitProgramSnapshot`) loaded via `IInitProgramProvider` with caching and change detection (file timestamp).
  - Expose lookup helpers (`GetValue(string controlId)`, `GetAll()`) for agents and validators.
- **PatchGenerationAgent**
  - Augment the system prompt with init program values (summarised or referenced) and instruct the model to respond with:
    ```json
    {
      "summary": "string",
      "controls": [
        {
          "id": "vco1_wave",
          "value": "triangle",
          "baselineValue": "saw",
          "deviationType": "changed",
          "explanation": "reasoning",
          "confidence": "medium"
        }
      ],
      "reasoning": { ... }
    }
    ```
  - Prefer `json_schema` / `response_format` to enforce baseline fields; fall back to strict JSON instructions when necessary.
  - Enforce token guards by truncating prompt input and limiting baseline context to essential controls to avoid runaway payload size.
- **Validation & Transformation**
  - Upon receiving the model output, validate against the baseline snapshot:
    - Reject controls whose `value` equals `baselineValue`.
    - Reject duplicate control IDs.
    - Compute `deviationType` when absent (e.g., numeric comparison -> `increase/decrease`, enum -> `changed`, boolean -> `toggled`).
  - Merge baseline metadata (label, group, ranges) before returning to the client.
- **Telemetry & Metrics**
  - Emit structured logs containing `deviationCount`, `continuousDeviationCount`, `largestDeviationMagnitude`, `baselineProgramId`, and OpenAI latency/token usage.
  - Continue correlating with `requestId`/`clientRequestId`.
- **Error Handling**
  - Differentiate between parsing errors and invalid deviations; return `502` with sanitized messages and include `x-request-id`.
  - Capture raw invalid payloads in secure telemetry channels.

## 5. Data, Prompting, and Configuration Assets
- **Init Program Snapshot**
  - Treat `init-program.json` as the single source of truth; add documentation in iteration notes on updating it when firmware revisions occur.
  - Include schema validation to detect missing controls or malformed values during startup (fail fast).
- **Prompt Template**
  - Version the new system prompt under `Resources/Prompts/PatchGenerationSystemPrompt.iteration003.txt` (or similar) with explicit instructions to:
    - Describe the init program baseline.
    - Only return deviations and include `baselineValue` for each.
    - Avoid proposing controls not listed in the catalog.
- **Shared Control Metadata**
  - If the frontend consumes a pruned `control-metadata.json`, update its generation script to include init baseline values and share the `baselineProgramId`.
- **Documentation**
  - Record the differential approach, assumptions, and telemetry additions in `iterations/003/notes.md` (implementation team to populate during development).

## 6. Acceptance Criteria
- Patch suggestions contain only controls whose values differ from the init program; responses include `baselineValue` and `deviationType`.
- UI renders all returned controls (no upper bound) with clear diff visualisation; empty deviation scenarios show an informative message.
- Telemetry captures deviation metrics on both client and server, including baseline identifiers.
- Backend rejects any response containing non-deviating or missing baseline-linked controls with a `502`.
- Existing reasoning metadata (summary, notes, assumptions) remains present and traceable.

## 7. Testing Strategy
- **Unit Tests**
  - `InitProgramProvider`: loads and validates the init snapshot, covers missing/invalid entries.
  - `PatchGenerationAgent`: prompt composition, schema validation with deviations only, deviation-type calculation logic.
  - `PatchSuggestionValidator`: ensures all controls deviate, handles numeric tolerance for floats.
- **Integration Tests**
  - Happy path verifying multiple deviations (>10) flow through to the frontend contract.
  - Model response containing baseline-matching control (expect `502`).
  - Response missing `baselineValue` (expect transformation or validation failure).
  - Empty deviation list (expect warning and friendly message).
- **End-to-End Smoke**
  - UI submission resulting in rich differential table.
  - Scenario where suggestion equals init program to confirm user messaging and telemetry.
- **Test Data**
  - Snapshot of `init-program.json` for deterministic assertions.
  - Mock Azure OpenAI responses covering deviations across continuous, enum, and boolean controls.

