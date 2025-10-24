# Iteration 001 Implementation Notes

This iteration delivers the "hello world" prompt loop defined in the specification. The resulting functionality is intentionally minimal while respecting the architectural overview of a React + .NET application augmented with Azure services.

## Frontend Highlights
- New Vite/React TypeScript application under `frontend/` with a `PromptConsole` component that captures prompts, disables submission while awaiting a response, and renders loading, success, and error states.
- A dedicated `usePatchRequest` hook issues `POST /api/v1/patch-request` calls, attaches the `x-client-request-id` header, and handles telemetry events for start, success, and failure flows using the Application Insights JavaScript SDK.
- Environment-driven configuration via `.env` (API base URL, Application Insights connection string) to keep deployment flexible.

## Backend Highlights
- New ASP.NET Core minimal API project (`backend/ToneDruid.Api`) exposing the required endpoint. Requests are validated, correlated request IDs are generated, and upstream Azure OpenAI calls are delegated to `PatchRequestRelayService`.
- Azure OpenAI integration uses deployment `gpt-4o`, includes the required system prompt, and emits telemetry + structured logging with latency and token usage when available.
- Application Insights is wired through the standard ASP.NET Core telemetry registration and events are emitted for each request phase.

## Configuration & Local Development
- Example configuration values reside in `frontend/.env.example` and `backend/ToneDruid.Api/appsettings.Development.json`. Populate these with your Azure OpenAI endpoint/key (or rely on `DefaultAzureCredential`) and Application Insights connection string before running locally.
- Start the backend via `dotnet run --project backend/ToneDruid.Api` and the frontend via `npm install && npm run dev` inside `frontend/`. The default URLs match those documented in the configuration templates.

## Observability
- Both tiers emit aligned telemetry events keyed on the generated request IDs. The backend also returns the `Request-Id` header for correlation and packages `requestId`/`clientRequestId` values within the JSON payload, enabling end-to-end traceability in Application Insights.

## Out of Scope
- No persistence, retrieval augmentation, or patch schema enforcement was introduced in this iteration; those will arrive in later milestones as described in the overarching roadmap.
