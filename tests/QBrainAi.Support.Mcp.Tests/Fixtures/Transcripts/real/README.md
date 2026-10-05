# Real Transcript Fixtures

These fixtures are sanitized derivatives of real local agent transcript files captured for `MCP-TRANSCRIPT-001` integration coverage.

Private prompts, assistant content, access tokens, opaque reasoning payloads, absolute user paths, request identifiers, and workspace details are replaced with deterministic fixture values. Provider-specific field names, event ordering, timestamps, status codes, token/cost containers, and native storage shapes are retained so parser tests exercise real transcript contracts rather than invented shapes.

Fresh smoke sessions were run for Copilot and OpenCode on 2026-07-10. Cline produced a real session artifact but the model request was rejected by the provider because the account had insufficient Cline Credits. Codex, Claude, and Grok fixtures are sanitized from existing real local session logs.