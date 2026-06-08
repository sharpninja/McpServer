# Federation Hub-Spoke Reference

Load this file when implementing, testing, or diagnosing MCP server federation.

## Roles

- `Standalone`: serve local workspaces only.
- `DirectProxy`: existing point-to-point proxy mode using configured targets and workspace routes.
- `Hub`: authoritative server for enrolled proxies, global workspace inventory, operation intake, sync fanout, queue status, and conflicts.
- `LocalProxy`: local server that forwards `/mcpserver/*` and `/mcp-transport` requests to `HubBaseUrl`, excluding local health/readiness/marker/federation diagnostics. Supported mutating REST requests are queued when the hub is unavailable; raw `/mcp-transport` streams are not accepted into the offline queue.

For implementation and validation, `PAYTON-DESKTOP` is the default hub machine and `PAYTON-LEGION2` is the default local proxy machine. Treat these as environment defaults, not hard-coded product constants.

## Required Status Surface

Use `GET /mcpserver/federation/status` or the corresponding client/plugin wrapper. Agents should report these fields when topology matters:

- `role`
- `configuredRole`
- `hubBaseUrl`
- `proxyId`
- `proxyCount`
- `hostedWorkspaceCount`
- `queueDepth`
- `fanoutDepth`
- `conflictCount`
- `staleReadStatus`

If `queueDepth`, `fanoutDepth`, `conflictCount`, or `staleReadStatus` is non-zero or non-clear, do not claim federation synchronization is complete. Record the status in the session log and include whether the current read may be stale.

## Agent And Plugin Usage

Agents should retrieve federation status through their required plugin or typed client wrapper after marker trust is verified. In plugin-required workspaces, do not use raw REST or another agent's plugin for normal federation status, queue inspection, or conflict-resolution workflow. Direct REST checks are acceptable only for implementation diagnostics after the plugin path has been ruled out.

## Hub Endpoints

- `POST /mcpserver/federation/proxies/enroll`: enroll or update a LocalProxy and its hosted workspaces.
- `POST /mcpserver/federation/proxies/{proxyId}/heartbeat`: update proxy liveness and workspace inventory.
- `GET /mcpserver/federation/proxies`: list enrolled proxies.
- `POST /mcpserver/federation/proxies/{proxyId}/workspaces`: register one hosted workspace.
- `GET /mcpserver/federation/workspaces`: list global or per-proxy workspaces.
- `POST /mcpserver/federation/operations`: accept or idempotently replay a proxy operation.
- `POST /mcpserver/federation/envelopes`: accept a signed proxy operation envelope.
- `POST /mcpserver/federation/operations/{operationId}/ack`: acknowledge replay or fanout.
- `GET /mcpserver/federation/queue`: inspect queued operation, fanout, and conflict counts.
- `GET /mcpserver/federation/conflicts`: list open or historical conflicts.
- `POST /mcpserver/federation/conflicts/{conflictId}/resolve`: resolve a conflict, defaulting to hub-wins.
- `GET /mcpserver/federation/sync`: stream hub fanout rows for a proxy after a sequence.
- `POST /mcpserver/federation/sync/{sequence}/ack`: acknowledge one recipient-specific fanout row.
- `GET /mcpserver/federation/adapters`: inspect mutable state adapter coverage, local-only exemptions, and whether signed apply is supported for each domain.

## Headers

- `X-Mcp-Proxy-Id`: originating proxy.
- `X-Mcp-Global-Workspace-Id`: hub-wide workspace id or proxy workspace path before enrollment maps it.
- `X-Mcp-Operation-Id`: idempotent operation id.
- `X-Mcp-Source-Operation-Id`: source operation id for echo suppression.
- `X-Mcp-Federation-Hop`: loop protection hop count.
- `X-Mcp-Queued`: `true` when a LocalProxy accepted a write into its durable queue.
- `X-Mcp-Stale-Read`: reserved for stale read diagnostics.
- `X-Mcp-Stale-Read-Status`: stale-read status detail.

## Queue Semantics

LocalProxy writes are optimistic until acknowledged by the hub. If the hub is unreachable and queueing is enabled, a queueable mutating request returns `202 Accepted`, `X-Mcp-Queued: true`, and `X-Mcp-Operation-Id`. The replay worker submits queued operations to the hub signed-envelope endpoint when signing is configured, otherwise to operation intake, and marks the local row acknowledged, failed, blocked, or conflicted.

Memory writes are queueable only for deterministic operations:

- `POST /mcpserver/memory` queues only when the JSON body supplies an explicit valid `MEMORY-*` id. Creates without an explicit id are forwarded live when the hub is reachable but are not accepted into the offline queue.
- `PUT /mcpserver/memory/{id}`, `PATCH /mcpserver/memory/{id}`, and `DELETE /mcpserver/memory/{id}` queue with domain `memory` and `{id}` as the resource id.

Queue-exempt domains include `context_metadata`, `github_metadata`, `repo_file_changes`, `marker_state`, `mcp_transport`, and `unknown`. These are either derived/local-only, externally sourced, security-sensitive, or too broad to replay safely from an opaque offline operation body.

Hub fanout uses recipient-specific outbox rows. A proxy acknowledges `sync/{sequence}/ack`; this must not drain other proxies' pending rows for the same operation.

Conflict handling is hub-authoritative by default. When an adapter exposes a current hub version and a proxy operation supplies a different base version, the hub records a conflict and does not fan out that operation.

## Adapter Coverage

Adapter diagnostics expose `covered`, `localOnly`, and `applySupported` per domain. `covered` means the domain can be snapshotted or explicitly exempted. `applySupported` means the LocalProxy can apply a signed operation for that domain during hub fanout. Do not treat a snapshot-only adapter as full replication support.

Adapter-backed replicated domains include `workspace`, `memory`, `todo`, `session_log`, `requirements`, `tools_buckets`, and `agents`. The `memory` adapter reads by globally unique memory id, uses `MemoryEntity.Version` as its version token, preserves scope/category/raw text/timestamps, enforces workspace ownership for Workspace-scoped rows, and applies deletes as idempotent soft deletes.

## Local Execution

Hub-origin operations that must run on a LocalProxy use a signed `FederationExecutionEnvelope` with `applyMode: local_execution`. The LocalProxy verifies the envelope target and signature before consulting `Mcp:Federation:LocalExecution`.

Initial supported method:

- `desktop_launch`: body is a `FederationLocalExecutionRequest` containing `workspacePath`, `executablePath`, arguments, window settings, and wait/timeout settings. The request still passes through `DesktopLaunchService`, so `Mcp:DesktopLaunch:Enabled`, access configuration, and the desktop executable allowlist remain authoritative.

If local execution is disabled, the method is not allowlisted, the envelope is invalid, or the desktop launch policy rejects the request, the proxy acknowledges the sync row with a non-success status and diagnostic error text.
