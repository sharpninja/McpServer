# FR-MCP-USECASE

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-USECASE-001 CRUD workspace-scoped use cases

The server shall support create, read, update, and soft-delete of workspace-scoped use cases with header fields: title, brief description, precondition, postcondition, scope, and priority. Default queries exclude soft-deleted rows. Multi-tenant isolation via WorkspaceId is required.
Scope: layer-1+

## FR-MCP-USECASE-002 Actors, flows, and ordered steps

Use cases support actors (Primary, Secondary, System, External), flows (Basic, Alternative, Exception) with sequence numbers, and ordered steps with action text and optional system response and data entities.
Scope: layer-1+

## FR-MCP-USECASE-003 Bidirectional UC to FR links with Realizes default

Use cases link bidirectionally to functional requirements using string FR ids. Default link type is Realizes. Active links are unique per workspace, use case, and FR. Unlink is soft-delete of the link row.
Scope: layer-1+

## FR-MCP-USECASE-004 Create use case from FR

Operators and agents can create a shell use case from an existing FR title/body with an automatic Realizes link.
Scope: layer-1+

## FR-MCP-USECASE-005 Diagram generation Mermaid primary

Two diagram kinds: (a) sequence from flows/steps; (b) UML use-case from persisted graph. Mermaid primary; PlantUML supported. Sequence is not a substitute for the use-case canvas editor.
Scope: layer-1+

## FR-MCP-USECASE-006 UC FR Realizes coverage API

A runtime coverage API reports use cases without Realizes FR links and FRs without Realizes use case links for the active workspace.
Scope: layer-1+

## FR-MCP-USECASE-007 First-party Use Case UI

First-party UI via REST. Primary diagram UI is UML use-case drag-and-drop canvas (FR-011). Structure forms secondary. Sequence render separate.
Scope: layer-1+

## FR-MCP-USECASE-008 Use case versioning and approval

Draft/Submitted/Approved/Rejected; version increments on Approve.
Scope: layer-1+
Acceptance: Approve increments VersionNumber; invalid status rejected.
Scope: layer-1+

## FR-MCP-USECASE-009 Product membership hooks

Optional ProductKey and list-by-product (hooks only).
Scope: layer-1+
Acceptance: set/clear ProductKey; list-by-product returns matches.
Scope: layer-1+

## FR-MCP-USECASE-010 Traceability integration for UC-FR Realizes

Shared Realizes algorithm; USECASE FR/TR/TEST IDs in docs matrix.
Scope: layer-1+
Acceptance: Shared algorithm tests; ValidateTraceability green for USECASE IDs.
Scope: layer-1+

## FR-MCP-USECASE-011 UML use-case canvas editor

Palette + free canvas + drag-and-drop UML use-case diagram editor (actors, use-case ovals, system boundary, association, include, extend). Classic UML editor interaction; not form-only.
ACs: AC-011-1 palette; AC-011-2 place shapes; AC-011-3 association; AC-011-4 include/extend; AC-011-5 rename; AC-011-6 move+layout persist; AC-011-7 boundary; AC-011-8 canvas primary UI; AC-011-9 REST only.
Scope: layer-1+

## FR-MCP-USECASE-012 Persist use-case diagram graph

Workspace-scoped graph (nodes, edges, layout) saved and loaded for a use case; soft-delete and validation. ACs AC-012-1 through AC-012-6 including audit on put.
Scope: layer-1+

## FR-MCP-USECASE-013 Export diagram to Mermaid

Persisted graph exports deterministic Mermaid per mcp-usecase-diagram-schema:1. ACs AC-013-1 through AC-013-4.
Scope: layer-1+

## FR-MCP-USECASE-014 Export diagram to PlantUML

Same graph exports PlantUML use-case syntax. ACs AC-014-1 through AC-014-3.
Scope: layer-1+

## FR-MCP-USECASE-015 BUG-TRIAGE-139 fourteenth-review safe durable remediation

Use-case creation and its supporting workspace, federation, requirements-export, timeout, and repository-evidence paths shall preserve one canonical ASCII-normalized durable identity, resist filesystem races through handle-relative contained mutation, remain physically bounded and cancellable, preserve exact provider state and real failures, reject blocking special files, and produce hermetic truthful evidence without regressing previously approved controls.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Persisted workspace, federation, and creation-receipt identifiers normalize Unicode dash variants to canonical ASCII before hashing, and replay is idempotent on SQLite, SQL Server, and PostgreSQL.
- [ ] Requirements rollback deletion, restoration writes, replacement, and directory creation are relative to pinned contained directory handles so concurrent junction or reparse swaps cannot affect objects outside the workspace.
- [ ] Mapped-drive and UNC physical identity resolution used by saves and federation upserts is asynchronous, cancellable, finite, and leaves no abandoned workers or repeated uncancellable probes.
- [ ] Requirements snapshots reject or nonblockingly inspect FIFO, device, socket, and other non-regular entries while preserving explicit file, byte, aggregate, depth, and cancellation limits.
- [ ] SQLite federation work restores exact managed, EF, connection-string, native busy-timeout, and busy-handler state across success, failure, cancellation, disposal, pooling modes, and repeated calls.
- [ ] SQLite connection opening is bounded before and after native-handle creation, cancel-before-handle and close/dispose races do not leak workers or handles, and native interruption is scoped to the active operation.
- [ ] SQLite, SQL Server, and PostgreSQL preserve genuine provider exceptions that coincide with cancellation and convert only genuine provider cancellation to OperationCanceledException.
- [ ] Repository evidence tests discover the repository from true external artifact directories without environment injection and bound stdout/stderr drain while killing descendant process trees that inherit pipes.
- [ ] Fourteenth receipts map exact commands to outputs, distinguish completed failures from aborted or inconclusive runs, supersede inaccurate twelfth claims explicitly, and carry an independently verified SHA-256 inventory.
- [ ] All previously approved controls remain covered and complete current-plus-prior provider, repository, release-build, compile, test, and traceability gates finish with zero failures and zero skips.

## FR-MCP-USECASE-018 Edit existing use-case actors, flows, and steps in place

Operators and agents shall edit actor, flow, and step records through supported authenticated operations while preserving the existing component identities and parent relationships.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A supported authenticated operation updates an actor already associated with a use case, preserves ActorId and UseCaseId, and returns the changed name, description, type, and IsPrimary values.
- [ ] A supported authenticated operation updates an existing flow in its parent use case, preserves FlowId and UseCaseId, and returns the changed FlowType, Name, and SequenceNumber values.
- [ ] A supported authenticated operation updates an existing step in its parent flow and use case, preserves StepId and FlowId, and returns the changed StepNumber, ActorId, Action, SystemResponse, and DataEntities values.
- [ ] Component updates reject missing, soft-deleted, cross-workspace, and parent-mismatched IDs without changing any row or creating duplicates.
- [ ] Updating actors, flows, or steps leaves the use case approval status, version, links, unrelated actors, unrelated flows, and unrelated steps unchanged while advancing only the aggregate update timestamp.

