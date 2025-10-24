# Iteration 002 Specification — Structured Patch Suggestions

## 1. Goal & Scope
- Extend the iteration 001 prompt loop so Tone Druid returns structured Minilogue XD patch suggestions (5–10 controls) plus explanation.
- Keep Azure OpenAI (`gpt-4o`) as the only reasoning source; no Synth Secrets retrieval or user feedback loop yet.
- Preserve traceability by attaching reasoning metadata, control-level confidence, and correlatable IDs across frontend, backend, and telemetry.
- Out of scope: Cosmos DB persistence, Cognitive Search integration, automated patch refinement, or multi-turn feedback handling.

## 2. System Flow Overview
1. User submits a natural-language sound description via the existing prompt UI.
2. Frontend issues `POST /api/v1/patch-request` with `{ prompt: string }`, `x-client-request-id`, and new header `x-iteration` = `002`.
3. Backend controller validates the request, generates `requestId`, loads the control catalog (from `voice-parameters.json` cache), and orchestrates `PatchGenerationAgent`.
4. `PatchGenerationAgent` calls Azure OpenAI Responses API with a system prompt that:
   - States the Tone Druid role and iteration objectives.
   - Provides a pruned catalog of Minilogue XD controls (5–10 most relevant per category) sourced from the voice parameter reference.
   - Requires a JSON response that fits the `PatchSuggestion` schema (see §4).
5. Backend parses and validates the JSON, enriches with server metadata (timestamps, model info), and logs telemetry (latency, token usage, control count).
6. Frontend renders the structured patch suggestion, including control table and reasoning sections; errors surface user-friendly messages while logging telemetry.

## 3. Frontend Responsibilities (React + TypeScript)
- **Data Contracts**
  - Update shared types to model `PatchSuggestion`:
    ```ts
    export type PatchControl = {
      id: string;
      label: string;
      group: string;
      value: number | string;
      valueType: 'continuous' | 'enumeration' | 'boolean';
      range?: { min: number; max: number; unit?: string };
      allowedValues?: string[];
      explanation: string;
      confidence: 'low' | 'medium' | 'high';
    };

    export type PatchSuggestion = {
      prompt: string;
      summary: string;
      controls: PatchControl[]; // length 5-10
      reasoning: {
        intentSummary: string;
        soundDesignNotes: string[];
        assumptions: string[];
      };
      requestId: string;
      clientRequestId: string;
      generatedAtUtc: string;
      model: string;
    };
    ```
  - Adjust `usePatchRequest` hook return signature to expose the new schema.
- **UI & State**
  - Enhance `PromptConsole` to display:
    - A summary paragraph from `summary`.
    - A control table (id, label, value, range/allowed, confidence, explanation).
    - Reasoning lists (sound design notes, assumptions).
  - Provide simple badges for `confidence` and highlight values outside nominal ranges (based on optional range metadata).
- **Telemetry**
  - Reuse Application Insights instance; track `patch_request_succeeded`/`failed` with new dimensions: `controlCount`, `hasReasoning`, `model`.
  - Log client-side validation errors (e.g., prompt empty) with `severityLevel = Warning`.
- **Error Handling**
  - Display JSON parsing or schema mismatch errors as “Unable to interpret patch suggestion. Please try again.” while logging the raw payload to telemetry (no console dumping in production builds).

## 4. Backend Responsibilities (.NET 8 Web API)
- **Endpoint Contract**
  - Continue using `POST /api/v1/patch-request`; request stays `{ "prompt": "string" }`.
  - Response body (all strings ISO-8601/UUID):
    ```json
    {
      "prompt": "string",
      "summary": "string",
      "controls": [
        {
          "id": "vco1_wave",
          "label": "VCO 1 Wave",
          "group": "Oscillators",
          "value": "triangle",
          "valueType": "enumeration",
          "allowedValues": ["saw", "triangle", "square"],
          "range": null,
          "explanation": "Selected triangle wave for softer attack.",
          "confidence": "medium"
        }
      ],
      "reasoning": {
        "intentSummary": "Explain target timbre.",
        "soundDesignNotes": ["Array of short notes."],
        "assumptions": ["No external FX."]
      },
      "requestId": "guid",
      "clientRequestId": "string",
      "generatedAtUtc": "2024-01-01T00:00:00Z",
      "model": "gpt-4o"
    }
    ```
  - Enforce 5–10 controls at validation; return `502` if the LLM output falls outside bounds or fails schema validation.
- **PatchGenerationAgent**
  - Introduce an agent service that:
    - Loads and caches the control catalog (subsetted to fields suitable for the LLM prompt).
    - Builds the system + user prompt instructing the model to choose controls most relevant to the user intent, avoid guessing unavailable parameters, and always supply reasoning arrays.
    - Requests JSON output via `json_schema` or `response_format` (if available) to minimise parsing errors; otherwise enforce JSON-only instructions and post-validate with `System.Text.Json`.
  - Strip out manual references for now (no retrieval); mention that knowledge is limited to model training data plus control catalog.
- **Validation & Error Handling**
  - Implement schema validation layer (e.g., record struct + `JsonSerializerOptions` with `PropertyNameCaseInsensitive = false`) to detect missing fields.
  - If parsing fails, log the raw content to secure telemetry, emit `502` with sanitized message, and include `requestId` header.
- **Observability**
  - Log structured events including `controlCount`, `confidenceBreakdown`, `promptLength`, and Azure OpenAI usage.
  - Align with Application Insights correlation model from iteration 001 (client/server IDs).
- **Security & Config**
  - Reuse existing Azure OpenAI configuration; no new Azure resources required.
  - Guard against prompt injection by truncating prompt to 500 chars and removing control catalog entries that exceed 1 KB per item before sending to the LLM.

## 5. Data, Prompting, and Configuration Assets
- **Voice Parameter Reference**
  - Relocate `Project Files/voice-parameters.json` to `backend/Resources/voice-parameters.json`; mark as `Content` with `CopyToOutputDirectory = PreserveNewest` so the API can load it at startup without bespoke tooling.
  - Provide a lightweight loader utility (`IVoiceParameterCatalog`) that:
    - Parses metadata + control definitions into strongly typed models.
    - Exposes helper methods like `GetControlById`, `ListByGroup`, and `GetPromptSubset(limitPerGroup)`.
  - Rationale: keeping the JSON in backend resources keeps the canonical control definitions single-sourced and accessible without additional storage while we defer Cosmos DB or Search.
- **Prompt Template**
  - Store the system prompt template alongside the agent (e.g., `Resources/Prompts/PatchGenerationSystemPrompt.txt`) so updates remain versioned.
  - Ensure template references the control catalog fields (label, range, allowed values) and instructs the model to only emit listed IDs.
- **Frontend Awareness**
  - Ship a derived `control-metadata.json` (generated at build or committed snapshot) containing only the fields needed for rendering (id, label, group, range/allowed). This keeps UI logic synchronous and avoids shipping the full manual metadata to the client.
  - Document the generation step in iteration notes; automation can be deferred until later iterations.

## 6. Acceptance Criteria
- Submitting a prompt returns a `PatchSuggestion` with 5–10 controls, each drawn from the catalog and accompanied by explanation and confidence.
- UI renders the summary, control table, and reasoning arrays without manual refresh; empty/error states behave as per iteration 001.
- Telemetry events include `requestId`, `clientRequestId`, `controlCount`, and success/failure outcome; backend logs capture Azure OpenAI latency and token usage.
- Backend rejects malformed or control-deficient model responses with `502`, and the UI surfaces the friendly error message.
- Voice parameter catalog is accessible to the backend at runtime from the resources folder and can be unit-tested independently of Azure dependencies.
