# Technical Requirements (MCP Server)

## TR-MCP-ARCH-001

ASP.NET Core 9 server with HTTP and STDIO MCP transport.

## TR-MCP-DATA-001

SQLite persistence for MCP metadata and optional TODO backend.

## TR-MCP-DATA-002

HNSW vector index with ONNX embeddings.

## TR-MCP-DATA-003

SQLite FTS5 full-text search support and hybrid ranking.

## TR-MCP-CFG-001

IOptions-based configuration for all filesystem and runtime settings.

## TR-MCP-CFG-002

Port selection from `Mcp:Port` with `PORT` env override.

## TR-MCP-INGEST-001

Pluggable ingestors for repo/session/external/github/issues.

## TR-MCP-API-001

REST routes for todo/session/context/repo/github/sync with OpenAPI.

## TR-MCP-OPS-001

Operational scripts for startup, health checks, packaging, config validation, and migration.
