# Web UI Requirements

This document tracks functional and technical requirements for the browser-based interfaces of McpServer, including the Pairing UI and the Management Dashboard.

## Functional Requirements

### FR-MCP-014 Pairing Web UI

The server shall provide a browser-based login flow for authorized users to retrieve the server API key for MCP client configuration, backed by SHA-256 constant-time password verification and HttpOnly session cookies.

**Covered by:** `PairingHtml`, `PairingOptions`, `PairingSessionService`

### FR-MCP-031 McpServer Management Web UI

A web-based management UI for McpServer providing workspace management, agent configuration, session log viewing, todo management, and system health monitoring. Integrates with the platform-wide open-source .NET OIDC provider for authentication. *(Planned — tracked as high-priority TODO.)*
