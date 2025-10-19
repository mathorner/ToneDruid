# Product Roadmap

1. [x] Minilogue XD Schema & Guardrails — Define the canonical schema for all Minilogue XD parameters (names, ranges, modulation sources) and provide a validation service with a minimal validator; all generated settings must pass this gate.
2. [ ] Device Knowledge Grounding + Citations — Ingest the Minilogue XD manual and curated resources into a document store and integrate retrieval so suggestions cite relevant sources the user can inspect.
3. [ ] Prompt → Parameters MVP — A single‑screen flow where users describe a sound and receive validated Minilogue XD settings plus a concise rationale, powered by Azure OpenAI gpt‑4.1. Depends on: Minilogue schema & guardrails.
4. [ ] Feedback Capture & Preference Profile — Let users rate, tweak, and accept suggestions; store outcomes to form a lightweight profile that nudges future generations toward personal tastes.
5. [ ] Session History — Persist prompt, versioned suggestions, and accepted patches.
6. [ ] Vocabulary Expansion Engine — Learn new descriptors from user feedback and curate mappings so colloquial phrases reliably translate into parameter patterns over time.
7. [ ] Observability & Metrics — Instrument end‑to‑end with OpenTelemetry and Azure tools to track suggestion success/failure, validation errors, latency, and perceived alignment KPIs with lightweight dashboards.
