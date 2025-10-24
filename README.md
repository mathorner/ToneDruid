# ToneDruid

Experimentation for an AI agentic coding course. Tone Druid is an AI-powered agent that suggests Minilogue XD synthesiser patch settings based on descriptive or emotional input. It interprets mood, atmosphere, or intent and translates that into practical sound design parameters — from filter shapes and modulation depth to oscillator types and effects.

## Iteration 001 — Prompt Loop
The first implementation milestone wires together the frontend, backend, and Azure OpenAI so that a free-form prompt can be sent from the browser, relayed by the API, and answered conversationally by the `gpt-4o` deployment.

### Prerequisites
- Node.js 18+
- .NET 8 SDK
- Azure OpenAI resource with a `gpt-4o` deployment
- Azure Application Insights (connection string is optional locally but required for telemetry)

### Configuration
1. Copy `frontend/.env.example` to `frontend/.env` and adjust the values for your environment.
2. Populate `backend/ToneDruid.Api/appsettings.Development.json` with the Azure OpenAI endpoint and either an API key or rely on `DefaultAzureCredential`. Provide the Application Insights connection string if available.

### Run Locally
1. **Backend**
   ```bash
   dotnet restore
   dotnet run --project backend/ToneDruid.Api
   ```
2. **Frontend**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
3. Open the Vite dev server URL (default `http://localhost:5173`) and send a prompt. The response panel will display the model output together with the correlated request identifiers.

### Telemetry
Both tiers emit Application Insights events named `patch_request_started`, `patch_request_succeeded`, and `patch_request_failed`. Each event contains the generated `clientRequestId` so traces can be correlated end-to-end across the frontend, backend, and Azure OpenAI.
