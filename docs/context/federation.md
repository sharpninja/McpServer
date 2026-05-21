# Federation Hub-Spoke Reference

Load this file when implementing, testing, or diagnosing MCP server federation.

## Roles

- `Standalone`: serve local workspaces only.
- `DirectProxy`: existing point-to-point proxy mode using configured targets and workspace routes.
- `Hub`: authoritative server for enrolled proxies, global workspace inventory, operation intake, sync fanout, queue status, and conflicts.
- `LocalProxy`: local server that forwards `/mcpserver/*` and `/mcp-transport` requests to `HubBaseUrl`, excluding local health/readiness/marker/federation diagnostics. Mutating requests are queued when the hub is unavailable.

## Required Status Surface

Use `GET /mcpserver/federation/status` or the corresponding client/plugin wrapper. Agents should report these fields when topology matters:

- `role`
- `configuredRole`
- `hubBaseUrl`
- `proxyId`
- `proxyCount`
- `hostedWorkspaceCount`
- `queueDepth`
- `conflictCount`

## Hub Endpoints

- `POST /mcpserver/federation/proxies/enroll`: enroll or update a LocalProxy and its hosted workspaces.
- `POST /mcpserver/federation/proxies/{proxyId}/heartbeat`: update proxy liveness and workspace inventory.
- `GET /mcpserver/federation/proxies`: list enrolled proxies.
- `POST /mcpserver/federation/proxies/{proxyId}/workspaces`: register one hosted workspace.
- `GET /mcpserver/federation/workspaces`: list global or per-proxy workspaces.
- `POST /mcpserver/federation/operations`: accept or idempotently replay a proxy operation.
- `POST /mcpserver/federation/operations/{operationId}/ack`: acknowledge replay or fanout.
- `GET /mcpserver/federation/queue`: inspect queued operation, fanout, and conflict counts.
- `GET /mcpserver/federation/conflicts`: list open or historical conflicts.
- `POST /mcpserver/federation/conflicts/{conflictId}/resolve`: resolve a conflict, defaulting to hub-wins.
- `GET /mcpserver/federation/sync`: stream hub fanout rows for a proxy after a sequence.
- `GET /mcpserver/federation/adapters`: inspect mutable state adapter coverage.

## Headers

- `X-Mcp-Proxy-Id`: originating proxy.
- `X-Mcp-Global-Workspace-Id`: hub-wide workspace id or proxy workspace path before enrollment maps it.
- `X-Mcp-Operation-Id`: idempotent operation id.
- `X-Mcp-Source-Operation-Id`: source operation id for echo suppression.
- `X-Mcp-Federation-Hop`: loop protection hop count.
- `X-Mcp-Queued`: `true` when a LocalProxy accepted a write into its durable queue.
- `X-Mcp-Stale-Read`: reserved for stale read diagnostics.

## Queue Semantics

LocalProxy writes are optimistic until acknowledged by the hub. If the hub is unreachable and queueing is enabled, a mutating request returns `202 Accepted`, `X-Mcp-Queued: true`, and `X-Mcp-Operation-Id`. The replay worker submits queued operations to the hub intake endpoint and marks the local row acknowledged, failed, blocked, or conflicted.

Conflict handling is hub-authoritative by default. When an adapter exposes a current hub version and a proxy operation supplies a different base version, the hub records a conflict and does not fan out that operation.
