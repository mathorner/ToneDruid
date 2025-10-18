# Product Overview
Tone Druid – an autonomous synth-patch helper that converts musical descriptions into actionable parameter suggestions for the Korg Minilogue XD.

# Key capabilities
Natural Language-to-Patch Translation
Converts descriptive sound requests (e.g. “haunting pad like Thom Yorke”) into concrete synthesiser parameter settings, grounded in the Minilogue XD's real-world controls.

Device-Aware Patch Generation
Uses a knowledge base of the Minilogue XD such as the manual and selected internet resources to ensure suggestions reference only valid parameters, ranges, and modulation sources specific to the instrument.

Explainable Sound Design Guidance
Provides clear rationales (“low filter cutoff and long attack create a darker, evolving timbre”) using insights from the Sound on Sound Synth Secrets series so that users learn synthesis principles while experimenting with patches. Further reference material can be added.

Adaptive Vocabulary Expansion – Learns new descriptors and user preferences over time through feedback loops.

Evaluation & Traceability – Built-in metrics for parameter accuracy, tool-call success, and perceived sound alignment.

# Target Users:
• Electronic musicians / sound designers who own a Minilogue.
• Skill level: beginner to intermediate (comfortable with patch editing).

# User Goals
• Describe a desired sound in plain language.
• Receive specific parameter suggestions for their Minilogue XD.
• Learn why those settings produce that sound (educational feedback).

# Technology Stack
• LLM capabilites provided by Azure OpenAI with the gpt-4.1 model
• References JSON containing mappings from natural language phrases such as "Soft brass swell" to a technical mapping such as "Slower attack and filter opening; gentle envelope slope to simulate controlled breath; low to medium resonance LPF.". References source material where mapping originated e.g. "Synth Secrets.pdf L74-L80"
• Minimal C# .NET stack for backend facilitating feedback loops and tool/reference material selection
• Initial simple text-based front-end based on React. Expanding into more feature-rich mobile-first web application
• Observability based on open telemetery and Azure tools. MVP beginning measure factors such as suggestion success and failures
• xUnit for unit-testing, Playwright for end-to-end testing
• document store for pdf manuals and reference documents. Outbound internet access, restricted to trusted sources.