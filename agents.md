# agents.md

## Project Purpose
This repository implements the **AI Patch Assistant for Minilogue XD** — an Azure-hosted web application that uses natural language prompts to generate and refine synthesizer patches.  
It leverages **Azure OpenAI** for reasoning and **Azure Cognitive Search** for contextual knowledge retrieval (e.g., *Synth Secrets* and the *Minilogue XD manual*).

---

## Agent Objectives
When assisting with code, documentation, or design in this project, you (the coding agent) should:

1. **Understand the System Context**
   - Core stack: **React + TypeScript** (frontend), **C# .NET Core** (backend).
   - Azure services: OpenAI, Cognitive Search, Blob Storage, Application Insights, and Cosmos DB.
   - Goal: enable a user to describe a sound (e.g. “sinister bell tone”) and get back Minilogue XD patch parameters with reasoning.

2. **Maintain Alignment with Architecture**
   - Reference the document `Architectural Overview.md` for the authoritative design.
   - Respect the modular boundaries:
     - **Frontend:** prompt/feedback UI and display of generated patches.
     - **Backend:** orchestration, LLM integration, telemetry, and data retrieval.

3. **Preserve Traceability and Explainability**
   - All LLM outputs must be accompanied by reasoning metadata.
   - Ensure telemetry hooks are implemented across modules for metrics such as latency, confidence, and success rate.

---

## Coding Conventions
- **Language:** TypeScript (frontend), C# (backend).
- **Frameworks:** React, .NET 8 Web API, Azure SDKs.
- **Style:**
  - Use async/await patterns.
  - Keep functions pure where practical.
  - Write self-documenting code; concise XML or JSDoc comments where necessary.
- **Testing:**
  - Jest for frontend.
  - xUnit for backend.
- **Linting/Formatting:**
  - ESLint + Prettier for TypeScript.
  - .editorconfig for consistent spacing and newline handling.

---

## File and Folder Structure

/frontend
  /src
    components/
    services/
    hooks/
  package.json
/backend
  /Agents
    PatchGenerationAgent.cs
    KnowledgeRetrievalAgent.cs
    FeedbackLearningAgent.cs
  /Models
  /Controllers
  /Services
/Project Files
  Architectural Overview.md

---

## Behavioural Instructions for AI Coding Assistants
When generating or editing code:

- ✅ Use **accurate Azure service patterns** (no hypothetical SDK calls).  
- ✅ Keep implementation practical and deployable.  
- ✅ Suggest dependency installation commands when introducing a new library.  
- ✅ Default to **secure**, **scalable**, and **testable** patterns.  
- ✅ When uncertain, propose two or three concrete implementation options.  
- ❌ Do not invent new technologies or re-architect core flows.  
- ❌ Avoid placeholders like “TODO implement logic”; include usable stubs.

---

## Example Prompts for You (the Coding Agent)
- “Implement an endpoint in .NET that accepts a user prompt and queries Azure OpenAI for patch suggestions.”  
- “Add telemetry middleware to capture latency and token usage.”  
- “Create a React component to display patch JSON and reasoning side-by-side.”  
- “Add unit tests for PatchGenerationAgent.”

---