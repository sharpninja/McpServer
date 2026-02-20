# Functional Requirements (MCP Server)

## FR-MCP-001 Configurable workspace root and paths

The server shall support configurable `RepoRoot`, `TodoFilePath`, `DataDirectory`, and index paths.

## FR-MCP-002 TODO management API

The server shall provide CRUD/query operations for TODO items over REST and STDIO.

## FR-MCP-003 Session log ingestion and query

The server shall ingest session logs and support searchable queries.

## FR-MCP-004 Hybrid context search

The server shall support FTS and vector search over indexed content.

## FR-MCP-005 GitHub issue sync

The server shall support GitHub issue lifecycle integration and ISSUE-* TODO synchronization.

## FR-MCP-006 Multi-source ingestion

The server shall ingest repository files, session logs, external docs, and issue content.

## FR-MCP-007 Dual transport

The server shall support HTTP and STDIO MCP transports.

## FR-MCP-008 Containerized deployment

The server shall support containerized deployment and packaged distribution.
