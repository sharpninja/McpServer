# TR-MCP-HTTP

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-HTTP-001

**MCP Streamable HTTP Endpoint** — `app.MapMcp("/mcp-transport")` maps the native MCP protocol handler at a path separate from the REST routes (`/mcpserver/*`). The endpoint requires an `Accept: application/json, text/event-stream` header and returns HTTP 406 without it. Uses `ModelContextProtocol.AspNetCore` 0.9.0-preview.1.
**Covered by:** FR: FR-MCP-016, FR-MCP-042
**Status:** pending
Scope: layer-1+

## TR-MCP-HTTP-002

**Detailed and Sanitized HTTP 500 Error Contract** — All HTTP endpoints that return status code 500 SHALL emit a structured response body containing a non-empty human-readable error description that identifies the failing operation and provides actionable diagnostic context for the caller. The contract SHALL be applied centrally so endpoint implementations do not duplicate exception-to-response formatting. Response detail SHALL be sanitized to avoid leaking secrets, tokens, connection strings, or raw stack traces, while server-side logs SHALL retain the full exception detail needed for root-cause analysis.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Support.Mcp/Program.cs` `InvalidModelStateResponseFactory` (centralized RFC 7807 ProblemDetails emission for binder/validation failures, paired with `ValidationProblem` / `Problem` controller helpers for domain errors); `SessionLogController.SubmitAsync` and `GetByIdAsync` route through the centralized path. Sanitization defers to ASP.NET Core's default ProblemDetails serialization, which omits stack traces outside the Development environment.
Scope: layer-1+

