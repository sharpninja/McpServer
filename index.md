# MCP Server Documentation

Welcome to the MCP Server documentation. MCP Server is a .NET 9/ASP.NET Core application providing workspace management, tool registry, API key authentication, pluggable tunnel providers, MCP Streamable HTTP transport, and Windows service support.

## Quick Links

- [FAQ](docs/FAQ.md) — Frequently asked questions
- [Requirements](docs/REQUIREMENTS.md) — Functional and technical requirements

## Features

| Feature | Description |
|---------|-------------|
| **Workspace Management** | Dynamic CRUD, init scaffolding, child process orchestration |
| **Tool Registry** | Keyword search, GitHub-backed bucket repositories |
| **Context Search** | Hybrid FTS5 + HNSW vector search with BM25 scoring |
| **TODO Management** | YAML and SQLite backends with GitHub Issue sync |
| **MCP Transport** | STDIO and Streamable HTTP (`/mcp-transport`) |
| **Tunnel Providers** | ngrok, Cloudflare, FRP — strategy pattern |
| **Security** | API key auth, pairing UI, constant-time password comparison |
| **Windows Service** | Auto-start, failure recovery, gsudo management |

## Getting Started

1. **Build**: `dotnet build src\McpServer.Support.Mcp\McpServer.Support.Mcp.csproj`
2. **Run**: `dotnet run --project src\McpServer.Support.Mcp`
3. **Install as service**: `.\scripts\Manage-McpService.ps1 -Action Install`

See the [FAQ](docs/FAQ.md) for detailed setup instructions.
