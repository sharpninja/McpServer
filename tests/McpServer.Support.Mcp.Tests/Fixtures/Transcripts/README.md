# Transcript Fixtures

These fixtures are sanitized Phase 0 inputs for `MCP-TRANSCRIPT-001`. They intentionally use fake session IDs, fake timestamps, placeholder model names, and toy workspace paths. They are not generated outputs and should remain stable across parser changes.

The fixture set covers Claude, Codex, Grok, Cline, Copilot, and OpenCode native shapes. Parser tests should treat malformed or unknown records as diagnostics, not silent drops. Compatibility profile output tests may derive Claude, Codex, or Grok JSONL from any source, but canonical Session Log YAML must be produced directly from the neutral event model.
