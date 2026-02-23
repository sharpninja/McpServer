# Testing Requirements (MCP Server)

- TEST-MCP-001: Given configurable RepoRoot/Todo paths, when service starts, then path resolution is correct.
- TEST-MCP-002: Given TODO API operations, when create/update/delete/query run, then contracts remain stable.
- TEST-MCP-003: Given multi-instance configuration, when two instances run, then ports and data roots remain isolated.
- TEST-MCP-004: Given vector + FTS data, when context search executes, then hybrid results are returned.
- TEST-MCP-005: Given GitHub sync enabled, when issue sync runs, then ISSUE-* mapping is consistent.
- TEST-MCP-006: Given STDIO mode, when tool requests are sent, then parity with HTTP behavior is preserved.
- TEST-MCP-007: Given workspace registration, when a workspace is created, then it receives a unique incremented port, its directory scaffold is created, its Kestrel host starts, and an `AGENTS-README-FIRST.yaml` marker file is written to its root.
- TEST-MCP-008: Given tool registry with tags, when keyword search runs with a singular or plural term, then matching tools from both global and workspace scopes are returned. Given default buckets in config, when the server starts for the first time, then buckets are seeded and idempotent on subsequent starts.
- TEST-MCP-009: Given per-workspace auth tokens, when a request to any `/mcp/*` endpoint lacks `X-Api-Key`, then the server returns 401 with an instruction to re-read the marker file. When a valid per-workspace token is provided, the request proceeds normally.
- TEST-MCP-010: Given valid pairing credentials, when the `/pair` login flow completes, then an HttpOnly session cookie is issued and the API key is returned. Given constant-time comparison, when two passwords of the same length differ by one character, then timing side-channel is not exploitable.
- TEST-MCP-011: Given a configured tunnel provider, when the hosted service starts, then the tunnel process launches and `GetStatusAsync` returns a public URL. When the service stops, the process is terminated within 5 s.
- TEST-MCP-012: Given an MCP client connecting to `/mcp-transport`, when a tool call is made, then the response is semantically equivalent to the corresponding REST endpoint result. Given a request without the required `Accept` header, then the endpoint returns 406.
- TEST-MCP-013: Given a workspace Kestrel host, when `StartAsync` completes, then `AGENTS-README-FIRST.yaml` exists at the workspace root with the correct port, endpoint paths, and auth token. When `StopAsync` completes, then the marker file is removed.
- TEST-MCP-014: Given a TODO item with a title and description, when `RequirementsService.AnalyzeAsync` is called, then `ExtractRequirementIds` correctly parses both JSON-block and regex-fallback response formats and returns distinct, non-empty FR/TR ID lists.
- TEST-MCP-015: Given a Markdown file with a `# Session Log – {title}` header, when `MarkdownSessionLogParser.TryParse` is called, then it returns a `UnifiedSessionLogDto` with matching title, model, status, and at least one entry. Given a file without the header, then `TryParse` returns null.
