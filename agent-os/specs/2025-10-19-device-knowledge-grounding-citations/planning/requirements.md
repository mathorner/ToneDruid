# Research Summary

## User Q&A
**Q1:** I’m assuming we’ll ingest the official Minilogue XD manual PDF plus the curated articles you already trust, and stop there for v1—should we also pull in community or forum content, or keep those out for now?  
**Answer:** Let's just use the XD manual and the Sound-on-Sound Synth Serets PDF for this first iteration

**Q2:** I’m thinking we store parsed documents in Azure Blob Storage with an embeddings index (Cognitive Search or a local vector store) so retrieval stays device-aware—does that match your expectations, or do you prefer another storage/retrieval stack?  
**Answer:** Azure blob storage is fine but we need to make sure of a frictionless dev approach. I don't know what you mean by "retrieval stays device-aware" but it sounds unneccessary. We're going for MVP here. There will only be one user of the app/agent

**Q3:** I assume the existing suggestion pipeline will call retrieval before finalizing a patch so every response can cite its sources—should the lookup happen in the backend orchestration layer, or somewhere else?  
**Answer:** backend orchestration sounds sensible

**Q4:** For citations, I’m leaning toward short inline references (e.g., “[Manual §3.2]”) plus a details panel—would you rather see full URLs, footnotes, or another presentation?  
**Answer:** keep it simple. it is just to provide a link for further information

**Q5:** I’m planning to preprocess documents into semantic chunks by parameter/topic to tighten recall—should we instead stick to simple page-level chunks to speed up implementation?  
**Answer:** simple implementation please

**Q6:** To keep content current, I’m assuming we’ll support a manual re-ingest trigger and log document versions—do you want automated change detection or scheduled refreshes in this iteration?  
**Answer:** no, the documents are rarely updated. It's for a hardware so rarely changes

**Q7:** I’m expecting we’ll evaluate grounding by tracking citation coverage and doing spot checks against the source text—is there another success metric we should bake into the plan (e.g., user trust surveys, retrieval latency targets)?  
**Answer:** just a feedback mechanism from the user (which is me). e.g. thumbs up/down, feedback field to say how it can be improved or what other descriptions might better fit

**Q8:** Are there any parts of the Minilogue XD documentation or related resources that we explicitly must exclude from ingestion or citation?  
**Answer:** No

## Existing Code to Reference
No similar existing features identified for reference.

## Visual Assets
No visual assets provided.

## Requirements Summary

### Functional Requirements
- Ingest the Minilogue XD manual and the Sound-on-Sound Synth Secrets PDF into the knowledge store.
- Offer a retrieval API that accepts simple queries and returns matched passages with links for manual inspection.
- Provide a lightweight console so the user can try retrieval queries, see citation links, and capture feedback inline.
- Capture user feedback on retrieved citations via thumbs up/down and an optional text field for future tuning of grounding quality.

### Reusability Opportunities
- None identified; MVP assumes fresh implementation tailored to this feature.

### Scope Boundaries
**In Scope:**  
- Minimal ingestion tooling and storage configuration using Azure Blob Storage suitable for single-user development.
- Baseline retrieval approach sufficient to look up passages from the ingested documents.
- Basic feedback collection on retrieval quality tied to individual query sessions.

**Out of Scope:**  
- Additional document sources beyond the two specified references.
- Automated document refresh, change detection, or advanced versioning workflows.
- Direct integration with the Prompt → Parameters flow or other orchestration features (handled in later specs).

### Technical Considerations
- Optimize for a lightweight, developer-friendly setup when interacting with Azure Blob Storage (e.g., local emulation or simple scripts).
- Keep retrieval implementation straightforward without additional device-aware heuristics; focus on reliable lookup from the ingested sources.
- Log retrieval requests so feedback entries can be tied to the specific query that produced a citation.
- Return results quickly enough for a single-user console, logging any failures for follow-up.
- Align feedback capture with existing backend stack (.NET minimal API) so future features can reuse the same endpoints and storage conventions.
