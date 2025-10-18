# Tech Stack

- Backend
  - Framework: .NET 9 Minimal API (C# 14)
  - Purpose: LLM orchestration, device‑aware validation, feedback capture
  - Tests: xUnit (`dotnet test`)

- Frontend
  - React + TypeScript (Vite)
  - Styling: TailwindCSS
  - E2E: Playwright

- AI/LLM
  - Azure OpenAI `gpt-4.1`
  - References: JSON mapping from descriptors → parameter patterns with citations

- Data & Storage
  - Primary DB: PostgreSQL 17+ (prompts, suggestions, feedback, user profiles, sessions)
  - Document Store: PDFs/manuals in Azure Blob Storage (local folder in dev)

- Observability
  - OpenTelemetry tracing/metrics; Azure Monitor/Application Insights

- Tooling
  - Package manager: npm
  - Lint/format: ESLint + Prettier for frontend; dotnet format for backend

- Deployment & CI
  - Hosting: Azure App Service (backend); Azure Static Web Apps (frontend)
  - CI/CD: GitHub Actions (build, test, deploy)

- Security & Config
  - Secrets via environment variables; no secrets in VCS
  - Outbound network restricted to trusted sources for reference retrieval

