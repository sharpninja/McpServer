# Testing Requirements (MCP Server)

- TEST-MCP-001: Given configurable RepoRoot/Todo paths, when service starts, then path resolution is correct.
- TEST-MCP-002: Given TODO API operations, when create/update/delete/query run, then contracts remain stable.
- TEST-MCP-003: Given multi-instance configuration, when two instances run, then ports and data roots remain isolated.
- TEST-MCP-004: Given vector + FTS data, when context search executes, then hybrid results are returned.
- TEST-MCP-005: Given GitHub sync enabled, when issue sync runs, then ISSUE-* mapping is consistent.
- TEST-MCP-006: Given STDIO mode, when tool requests are sent, then parity with HTTP behavior is preserved.
