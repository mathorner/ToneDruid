# Iteration 001 Specification — Hello World Prompt Loop

## 1. Goal & Scope
- Deliver the simplest possible end-to-end loop: the React UI captures a text prompt, the .NET backend forwards it to Azure OpenAI (deployment: `gpt-4o`), and the model response text is rendered back in the UI.
- No patch schema, retrieval, or persistence is required in this iteration; we only verify wiring between components and services.
- Establish minimal observability so requests can be traced from UI to backend to Azure OpenAI.

## 2. System Flow Overview
1. User types a prompt into the web UI and presses **Send**.
2. Frontend issues `POST /api/v1/patch-request` carrying `{ prompt: string }` and a generated `clientRequestId` header.
3. Backend controller validates the prompt, adds a `serverRequestId`, and invokes the Azure OpenAI Responses API using the `gpt-4o` deployment.
4. Backend returns a JSON payload with the echoed prompt, the raw model response text, and both request IDs.
5. UI renders the response text in a simple panel and shows errors when calls fail.
6. Application Insights receives a basic trace for the round trip (latency + success/failure).

## 3. Frontend Responsibilities (React + TypeScript)
- **Component**: `PromptConsole`
  - Textarea input bound to component state, submit button disabled while awaiting a response.
  - Uses a lightweight `usePatchRequest` hook to call the backend.
  - Displays three states: idle placeholder, loading spinner, and response/error output.
- **Hook**: `usePatchRequest(prompt: string)`
  - Calls backend with `fetch` (or existing HTTP helper) and attaches `x-client-request-id` header (UUID).
  - Returns `{ data?: PatchRequestResult, isLoading: boolean, error?: string }` where `PatchRequestResult` is `{ prompt: string; response: string; requestId: string; clientRequestId: string; generatedAtUtc: string; }`.
- **Telemetry**
  - Initialize Application Insights JS SDK once (connection string via `VITE_APPINSIGHTS_CONNECTION_STRING`).
  - Track custom events: `patch_request_started`, `patch_request_succeeded`, `patch_request_failed` with relevant IDs for correlation.
- **Configuration**
  - `.env`: `VITE_API_BASE_URL`, `VITE_APPINSIGHTS_CONNECTION_STRING`.

## 4. Backend Responsibilities (.NET 8 Minimal API or Controller)
- **Endpoint**: `POST /api/v1/patch-request`
  - Request body: `{ "prompt": "string" }` (reject null/empty or >500 chars).
  - Response body:
    ```json
    {
      "prompt": "string",
      "response": "string",
      "requestId": "guid",
      "clientRequestId": "string",
      "generatedAtUtc": "2024-01-01T00:00:00Z"
    }
    ```
- **Service**: `PatchRequestRelayService`
  - Uses `Azure.AI.OpenAI` `OpenAIClient` with deployment name `gpt-4o`.
  - Sends system message: "You are Tone Druid. Reply conversationally to the given prompt." (no patch schema enforcement).
  - Returns the first choice message content as plain text; no JSON parsing required.
- **Observability**
  - Register Application Insights via `AddApplicationInsightsTelemetry`.
  - Log start/end using `ILogger` with `requestId`, `clientRequestId`, latency, and Azure OpenAI `usage` tokens when available.
  - Include `requestId` as `Request-Id` header in HTTP response for correlation.
- **Error Handling**
  - Surface Azure OpenAI errors as `502` with sanitized message; unknown errors as `500`.

## 5. Azure Resources for Iteration 001
Minimal provisioning using Azure CLI (after `az login` and selecting subscription):
```bash
#!/usr/bin/env bash
set -euo pipefail

LOCATION="eastus"
RESOURCE_GROUP="<rg-name>"
OPENAI_NAME="<openai-name>"
APPINSIGHTS_NAME="<appinsights-name>"

az group create --name "$RESOURCE_GROUP" --location "$LOCATION"

az cognitiveservices account create \
  --name "$OPENAI_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --kind OpenAI \
  --sku s0 \
  --location "$LOCATION" \
  --yes

az cognitiveservices account deployment create \
  --name "$OPENAI_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --deployment-name "gpt-4o" \
  --model-name "gpt-4o" \
  --model-version "2024-05-13" \
  --model-format OpenAI \
  --scale-settings-scale-type "standard"

az monitor app-insights component create \
  --app "$APPINSIGHTS_NAME" \
  --location "$LOCATION" \
  --resource-group "$RESOURCE_GROUP" \
  --application-type web
```
- Store the Azure OpenAI endpoint, deployment name, and App Insights connection string in secure config (`appsettings.Development.json`, `.env`).
- Backend should authenticate with `DefaultAzureCredential` (managed identity in cloud, Azure CLI locally) or fall back to API key for local testing if necessary.

## 6. Acceptance Criteria
- Prompt submission from the UI triggers a backend patch request call and displays the raw `gpt-4o` response text.
- Both frontend and backend emit telemetry entries containing matching request IDs for the patch request flow.
- Errors (e.g., empty prompt, Azure failure) surface user-friendly messages and log telemetry.
- No additional services (Cosmos DB, Search, Blob) are required in this iteration; they remain out of scope until later.
