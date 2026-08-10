---
title: McpServer Use Case Extension – Full Design & Implementation Package
version: 2.0
date: 2026-08-07
author: GrokCode (v2 TR-compliance rewrite of v1.0)
status: Ready for implementation planning (Byrd gates required)
supersedes: docs/McpServer-UseCase-Extension-Design-v1.0.md
audit_basis: docs/receipts/usecase-design-tr-audit-20260807T141204Z.md
---

# McpServer Use Case Extension – Design Package v2.0

## 0. Document control

| Item | Value |
|------|--------|
| Supersedes | `docs/McpServer-UseCase-Extension-Design-v1.0.md` (2026-07-10) |
| Why v2 | v1 failed TR audit: numeric FrId, SQL Server-only schema, missing soft-delete/audit/workspace FK, CQRS named but not designed, wrong ValidateTraceability seam |
| Process | Byrd Development Process v4 (tests first, zero skips in gate scope) |
| Breaking to existing FR/TR/TEST APIs | **No** (additive domain + additive FR projection field) |
| Breaking vs v1 design sketch | **Yes** (storage keys, lifecycle, CQRS surface, multi-provider) |

v1 is retained for history. Implement **only** from this v2 document.

---

## 1. Executive summary (product intent preserved)

| Preference | Value (locked) |
|------------|----------------|
| Default `LinkType` when linking/creating from FR | `Realizes` |
| Diagram format | **Mermaid only** (v1) |
| Design surface priority | Pure API + external editors (no Blazor / no in-server designer in v1 scope) |
| Naming | `UseCase*`, route `/mcpserver/usecases`, MCP tools `usecase_*` |
| FR from Use Case (reverse flow) | **Yes** (fully supported) |
| Application pattern | **CQRS required** (`McpServer.Cqrs`: commands/queries/handlers/`Dispatcher`/`Result<T>`) |

This extension adds structured Use Case modeling with live Mermaid diagrams, 4NF storage, and bidirectional traceability to the existing FR surface, with **zero breaking changes** to existing requirements APIs.

### 1.1 Architecture alignment (non-negotiable)

Grounded in live technical requirements:

- **Platform:** ASP.NET Core 9, HTTP + STDIO MCP (`TR-MCP-ARCH-001`, `FR-MCP-007`, `FR-MCP-016`)
- **Multi-tenant single host:** `WorkspaceContext` + resolution middleware; all entities carry `WorkspaceId` + global query filter (`FR-MCP-043`, `FR-MCP-044`, `TR-MCP-MT-001`…`003`)
- **Auth:** `/mcpserver/*` via `WorkspaceAuthMiddleware` (API key / JWT policies as today) (`FR-MCP-013`, `TR-MCP-AUTH-010`)
- **CQRS:** `ICommand`/`IQuery` + handlers + `Dispatcher` + correlation + `Result<T>` (`FR-MCP-029`, `TR-MCP-CQRS-001`…`005`)
- **DI ownership:** stateful services DI-owned; single owner; peers pull state (`TR-MCP-ARCH-002`)
- **Storage:** EF Core on `McpDbContext`; providers SQLite / PostgreSQL / SQL Server (`TR-MCP-CFG-007`)
- **Lifecycle:** soft delete + Restrict/NoAction for durable domain (`TR-MCP-DB-003`); workspace FK (`TR-MCP-DB-002`); append-only audit for mutable entities (`TR-MCP-DB-004`)
- **FR identity:** string ids (`FR-MCP-…`) on `RequirementEntity` (`Kind = fr`), not `long` (`TR-MCP-REQ-*`, source of truth entity)
- **Surfaces:** REST controller + MCP tools + typed client (`TR-MCP-API-001` pattern, `TR-MCP-REQ-003` dual surface, `TR-MCP-CLIENT-001`)
- **UI:** no Blazor; external tools consume REST (`TR-MCP-WEB-001`)

---

## 2. Requirements to create before implementation

Create via MCP requirements workflow (authoritative store). Suggested IDs (adjust if collisions):

### Functional

- **FR-MCP-USECASE-001** — CRUD workspace-scoped use cases (header fields)
- **FR-MCP-USECASE-002** — Actors, flows (Basic/Alternative/Exception), ordered steps
- **FR-MCP-USECASE-003** — Bidirectional UC↔FR links; default `Realizes`
- **FR-MCP-USECASE-004** — Create use case from FR with auto `Realizes` link
- **FR-MCP-USECASE-005** — Generate Mermaid sequence diagram from stored structure
- **FR-MCP-USECASE-006** — Report UC↔FR Realizes coverage gaps (runtime API)

### Technical

- **TR-MCP-USECASE-001** — EF 4NF entities, soft-delete, workspace FK, multi-provider migrations, query filters
- **TR-MCP-USECASE-002** — CQRS commands/queries/handlers registered with `Dispatcher`
- **TR-MCP-USECASE-003** — REST `/mcpserver/usecases` thin controller dispatching CQRS
- **TR-MCP-USECASE-004** — Mermaid generator (deterministic, DI-owned)
- **TR-MCP-USECASE-005** — MCP tools `usecase_*` + typed `UseCaseClient` + JsonContext
- **TR-MCP-USECASE-006** — FR get projection `linkedUseCases`; coverage query; audit emission

### Testing

- **TEST-MCP-USECASE-001** — Service/handler unit tests (CRUD, isolation, soft delete, FR link)
- **TEST-MCP-USECASE-002** — Controller unit tests
- **TEST-MCP-USECASE-003** — Mermaid golden tests
- **TEST-MCP-USECASE-004** — Client unit tests
- **TEST-MCP-USECASE-005** — Coverage query tests

Map FR→TR→TEST and matrix rows so `./build.ps1 ValidateTraceability` passes for the **new requirement IDs** (docs matrix). UC↔FR **runtime** coverage is separate (section 8).

---

## 3. Data model (4NF, multi-tenant, soft-delete)

### 3.1 Conventions

| Concern | Rule |
|---------|------|
| `WorkspaceId` | `string` max 1024; normalized workspace path; required; **FK to Workspaces** |
| Soft delete | `IsDeleted` bool + `DeletedAtUtc` + `DeletedBy` (nullable); default queries exclude deleted |
| Relationships | `Restrict` / `NoAction` on durable FKs; no cascade physical delete of domain rows |
| Timestamps | `CreatedAtUtc` / `UpdatedAtUtc` as `DateTimeOffset` (or project-standard UTC string if entity family requires; prefer `DateTimeOffset` for new Use Case entities) |
| Surrogate keys | `long` identity/serial/autoincrement **per provider** for Use Case aggregate rows |
| FR keys | `FrId` is **string(128)** matching `RequirementEntity.Id` where `Kind = 'fr'` |
| Audit | Every mutable Use Case entity mutation emits audit ledger rows (`TR-MCP-DB-004`) |
| Query filter | All Use Case entities participate in `McpDbContext` workspace filter (`TR-MCP-MT-003`) |

### 3.2 Logical schema (provider-neutral)

Primary tables (names illustrative; EF maps per provider):

**UseCases**

- `UseCaseId` long PK (identity)
- `WorkspaceId` string(1024) NOT NULL FK → Workspaces
- `Title` string(200) NOT NULL
- `BriefDescription`, `Precondition`, `Postcondition` text nullable
- `Scope` string(50) nullable
- `Priority` int NOT NULL default 0
- `CreatedAtUtc`, `UpdatedAtUtc` DateTimeOffset NOT NULL
- Soft-delete columns
- Indexes: `(WorkspaceId)`, `(WorkspaceId, Title)`

**Actors**

- `ActorId` long PK
- `WorkspaceId` string(1024) NOT NULL FK → Workspaces
- `Name` string(100) NOT NULL
- `Description` text nullable
- `Type` string(20) CHECK in (`Primary`,`Secondary`,`System`,`External`)
- Soft-delete columns
- Unique optional: `(WorkspaceId, Name)` among non-deleted

**UseCaseActors**

- `WorkspaceId` string(1024) NOT NULL
- `UseCaseId` long NOT NULL
- `ActorId` long NOT NULL
- `IsPrimary` bool NOT NULL default false
- PK: `(WorkspaceId, UseCaseId, ActorId)`
- FKs to UseCases and Actors (same workspace enforced in handlers)

**UseCaseFlows**

- `FlowId` long PK
- `WorkspaceId` string(1024) NOT NULL
- `UseCaseId` long NOT NULL
- `FlowType` in (`Basic`,`Alternative`,`Exception`)
- `Name` string(100) nullable
- `SequenceNumber` int NOT NULL
- Soft-delete columns

**UseCaseSteps**

- `StepId` long PK
- `WorkspaceId` string(1024) NOT NULL
- `FlowId` long NOT NULL
- `StepNumber` int NOT NULL
- `ActorId` long nullable
- `Action` text NOT NULL
- `SystemResponse`, `DataEntities` text nullable
- Soft-delete columns

**UseCaseSpecialRequirements**

- `SpecialReqId` long PK
- `WorkspaceId` string(1024) NOT NULL
- `UseCaseId` long NOT NULL
- `Category` string(50) nullable
- `RequirementText` text NOT NULL
- `Priority` int nullable
- Soft-delete columns

**UseCaseExtensionPoints**

- `ExtensionPointId` long PK
- `WorkspaceId` string(1024) NOT NULL
- `UseCaseId` long NOT NULL
- `Name` string(100) NOT NULL
- `Description` text nullable
- Soft-delete columns

**UseCaseFrLinks** (key integration)

- `LinkId` long PK
- `WorkspaceId` string(1024) NOT NULL FK → Workspaces
- `UseCaseId` long NOT NULL
- `FrId` string(128) NOT NULL  (**not** bigint)
- `FrKind` string(16) NOT NULL default `fr` (fixed for FK clarity)
- `LinkType` string(20) NOT NULL default `Realizes`
- `LinkOrder` int NOT NULL default 0
- `Notes` text nullable
- `CreatedAtUtc` DateTimeOffset NOT NULL
- Soft-delete columns (unlink = soft delete)
- Unique among non-deleted: `(WorkspaceId, UseCaseId, FrId)`
- FK UseCases: `(WorkspaceId, UseCaseId)` as modeled
- FK Requirements: `(WorkspaceId, FrKind, FrId)` → `RequirementEntity` where kind is `fr`

### 3.3 EF entity sketch (illustrative)

```csharp
// Namespace: McpServer.Support.Mcp.Storage.Entities
// All public members require XML docs citing FR-MCP-USECASE-* / TR-MCP-USECASE-*.

public sealed class UseCaseEntity
{
    public long UseCaseId { get; set; }
    [StringLength(1024)] public string WorkspaceId { get; set; } = string.Empty;
    [StringLength(200)] public string Title { get; set; } = string.Empty;
    public string? BriefDescription { get; set; }
    public string? Precondition { get; set; }
    public string? Postcondition { get; set; }
    [StringLength(50)] public string? Scope { get; set; }
    public int Priority { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    [StringLength(256)] public string? DeletedBy { get; set; }

    public ICollection<UseCaseActorEntity> UseCaseActors { get; set; } = new List<UseCaseActorEntity>();
    public ICollection<UseCaseFlowEntity> Flows { get; set; } = new List<UseCaseFlowEntity>();
    public ICollection<UseCaseSpecialRequirementEntity> SpecialRequirements { get; set; } = new List<UseCaseSpecialRequirementEntity>();
    public ICollection<UseCaseExtensionPointEntity> ExtensionPoints { get; set; } = new List<UseCaseExtensionPointEntity>();
    public ICollection<UseCaseFrLinkEntity> FrLinks { get; set; } = new List<UseCaseFrLinkEntity>();
}

public sealed class UseCaseFrLinkEntity
{
    public long LinkId { get; set; }
    [StringLength(1024)] public string WorkspaceId { get; set; } = string.Empty;
    public long UseCaseId { get; set; }
    [StringLength(128)] public string FrId { get; set; } = string.Empty;
    [StringLength(16)] public string FrKind { get; set; } = "fr";
    [StringLength(20)] public string LinkType { get; set; } = "Realizes";
    public int LinkOrder { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    [StringLength(256)] public string? DeletedBy { get; set; }

    public UseCaseEntity UseCase { get; set; } = null!;
    public RequirementEntity? FunctionalRequirement { get; set; }
}
```

**v1 error corrected:** navigation is to `RequirementEntity` (kind `fr`), not a non-existent `FunctionalRequirement` type. `FrId` is **string**.

### 3.4 Migrations

Name: `AddUseCaseSupport` (timestamped per project convention).

Must land in **all three**:

- `src/McpServer.Storage.SqliteMigrations`
- `src/McpServer.Storage.PostgreSqlMigrations`
- `src/McpServer.Storage.SqlServerMigrations`

Register `DbSet<>` on `McpDbContext`, fluent config, workspace query filters, and soft-delete filters consistent with sibling entities (Memory / Requirements patterns).

---

## 4. CQRS design (required)

### 4.1 Placement

| Artifact | Location |
|----------|----------|
| Commands / Queries | e.g. `src/McpServer.Services/UseCases/Commands`, `.../Queries` (or dedicated project if size warrants; GraphRAG-style colocation is acceptable) |
| Handlers | same assembly; implement `ICommandHandler<,>` / `IQueryHandler<,>` |
| DI registration | `AddUseCaseCqrs(IServiceCollection)` registers handlers; host calls `AddCqrsHandlers` / existing `AddCqrs` for the assembly in **both** `Program.cs` and `McpStdioHost.cs` |
| Persistence helper | DI-owned `IUseCaseStore` or handler-local `McpDbContext` access; **no** `new` of stateful services outside composition root (`TR-MCP-ARCH-002`) |
| Diagram | DI-owned `IUseCaseDiagramService` (stateless pure builder preferred; data loaded by query handler then passed in) |

Controllers and MCP tools **only** build command/query messages and call `IDispatcher.SendAsync` / `QueryAsync`. They do not open DbContext for domain logic.

### 4.2 Commands (mutations) — minimum set

All return `Result<T>` via handlers.

| Command | Result | Notes |
|---------|--------|-------|
| `CreateUseCaseCommand` | `UseCaseDetailDto` | Optional `FrId` + `LinkType` (default Realizes); optional initial basic flow |
| `UpdateUseCaseCommand` | `UseCaseDetailDto` | Header fields only |
| `DeleteUseCaseCommand` | `bool` | Soft-delete aggregate + children + links |
| `AddUseCaseFlowCommand` | `UseCaseFlowDto` | |
| `AddUseCaseStepCommand` | `UseCaseStepDto` | |
| `AttachUseCaseActorCommand` | `UseCaseActorDto` | Create actor if needed in same workspace |
| `LinkUseCaseToFrCommand` | `UseCaseFrLinkDto` | Validates FR exists (`Kind=fr`); default LinkType Realizes; 409 on active duplicate |
| `UnlinkUseCaseFromFrCommand` | `bool` | Soft-delete link |
| `CreateUseCaseFromFrCommand` | `UseCaseDetailDto` | Shell UC from FR title/body + Realizes link |

### 4.3 Queries (reads) — minimum set

| Query | Result |
|-------|--------|
| `GetUseCaseQuery` | `UseCaseDetailDto` (full aggregate, non-deleted) |
| `ListUseCasesQuery` | `UseCaseSummaryDto[]` (title filter optional) |
| `GetUseCaseDiagramQuery` | `UseCaseDiagramDto` (`format` must be `mermaid`) |
| `GetUseCasesForFrQuery` | `LinkedUseCaseDto[]` (for FR projection) |
| `GetUseCaseFrCoverageQuery` | `UseCaseFrCoverageDto` (gaps for Realizes) |

### 4.4 Command / query shape examples

```csharp
public sealed record CreateUseCaseCommand(
    string WorkspacePath,
    CreateUseCaseRequest Request) : ICommand<UseCaseDetailDto>;

public sealed record GetUseCaseQuery(
    string WorkspacePath,
    long UseCaseId) : IQuery<UseCaseDetailDto>;

public sealed record GetUseCaseDiagramQuery(
    string WorkspacePath,
    long UseCaseId,
    string Format) : IQuery<UseCaseDiagramDto>;

public sealed record LinkUseCaseToFrCommand(
    string WorkspacePath,
    long UseCaseId,
    string FrId,
    string? LinkType,
    int LinkOrder,
    string? Notes) : ICommand<UseCaseFrLinkDto>;
```

Handlers resolve workspace from `WorkspaceContext` when already set; accept `WorkspacePath` for MCP override paths consistent with other tools. Prefer injecting `WorkspaceContext` and `McpDbContext` rather than re-resolving ad hoc.

### 4.5 Correlation and logging

All dispatches use existing `Dispatcher` pipeline (correlation ids, Result logging) per `TR-MCP-CQRS-002`…`004`. Do not invent a parallel logging path.

---

## 5. REST API (pure API first)

Route: `[Route("mcpserver/usecases")]`  
Auth: existing middleware only (no WorkspaceIndependentPrefixes).

| Method | Path | Dispatches |
|--------|------|------------|
| POST | `/mcpserver/usecases` | `CreateUseCaseCommand` |
| GET | `/mcpserver/usecases` | `ListUseCasesQuery` |
| GET | `/mcpserver/usecases/{id}` | `GetUseCaseQuery` |
| PUT | `/mcpserver/usecases/{id}` | `UpdateUseCaseCommand` |
| DELETE | `/mcpserver/usecases/{id}` | `DeleteUseCaseCommand` |
| POST | `/mcpserver/usecases/{id}/flows` | `AddUseCaseFlowCommand` |
| POST | `/mcpserver/usecases/{id}/flows/{flowId}/steps` | `AddUseCaseStepCommand` |
| POST | `/mcpserver/usecases/{id}/actors` | `AttachUseCaseActorCommand` |
| POST | `/mcpserver/usecases/{id}/links` | `LinkUseCaseToFrCommand` |
| DELETE | `/mcpserver/usecases/{id}/links/{frId}` | `UnlinkUseCaseFromFrCommand` |
| GET | `/mcpserver/usecases/{id}/diagram?format=mermaid` | `GetUseCaseDiagramQuery` |
| POST | `/mcpserver/usecases/from-fr/{frId}` | `CreateUseCaseFromFrCommand` |
| GET | `/mcpserver/usecases/coverage` | `GetUseCaseFrCoverageQuery` |

Map `Result` failures to HTTP: validation → 400, not found → 404, conflict → 409, unexpected → centralized 500 contract (`TR-MCP-HTTP-002`).

### 5.1 FR surface (additive only)

Extend FR get / list DTO (or nested property on existing FR payload) with:

```json
"linkedUseCases": [
  { "useCaseId": 1, "title": "...", "linkType": "Realizes", "linkOrder": 0 }
]
```

Implementation: requirements document service calls `GetUseCasesForFrQuery` (or shared store method used by both handlers). Do not change FR id scheme.

---

## 6. MCP tools + typed client

### 6.1 MCP (`FwhMcpTools.UseCases.cs`)

| Tool | Behavior |
|------|----------|
| `usecase_list` | `ListUseCasesQuery` |
| `usecase_get` | `GetUseCaseQuery` |
| `usecase_create` | `CreateUseCaseCommand` |
| `usecase_update` | `UpdateUseCaseCommand` |
| `usecase_delete` | `DeleteUseCaseCommand` |
| `usecase_link_fr` | `LinkUseCaseToFrCommand` (`LinkUseCaseToFr`) |
| `usecase_from_fr` | `CreateUseCaseFromFrCommand` (`CreateUseCaseFromFr`) |
| `usecase_diagram` | `GetUseCaseDiagramQuery` (`RenderMermaidDiagram`) |
| `usecase_coverage` | `GetUseCaseFrCoverageQuery` |

Each takes `workspacePath`, calls `ApplyWorkspaceOverride`, returns JSON string; errors via `McpToolErrors.Serialize`.

### 6.2 Client (`McpServer.Client`)

- `Models/UseCaseModels.cs`
- `UseCaseClient.cs`
- Property on `McpServerClient`
- Register DTOs on `McpClientJsonContext` (`TR-MCP-CLIENT-001`)
- Unit tests with mock HTTP handler

### 6.3 External editors

Mermaid Live, VS Code, Obsidian, etc. consume REST only. No first-party UI in this repo.

---

## 7. Mermaid generation

DI-owned `IUseCaseDiagramService`:

```csharp
public interface IUseCaseDiagramService
{
    /// <summary>TR-MCP-USECASE-004: Build Mermaid sequence text from a loaded aggregate.</summary>
    string GenerateMermaid(UseCaseDetailDto useCase);
}
```

Rules:

1. Emit `sequenceDiagram`.
2. Participants from attached actors (sanitize names for Mermaid).
3. Flows by `SequenceNumber`, steps by `StepNumber`.
4. Prefer Actor → System messages from `Action` / `SystemResponse`.
5. Label Alternative/Exception flows with notes or alt blocks; v1 may linearize with notes.
6. Pure function of input DTO: no DB, no network; stable ordering for tests.
7. `format != mermaid` rejected at query handler with validation failure.

Query handler loads aggregate, then calls diagram service (pull model; diagram service does not own DbContext).

---

## 8. Validation and coverage (two seams)

### 8.1 Docs / Nuke `ValidateTraceability` (unchanged purpose)

Continues to validate **FR/TR/TEST markdown matrix** for requirement IDs that exist in docs/exports (`TR-MCP-REQ-002` family).

**Do not** load `UseCaseFrLinks` into the docs Nuke validator.

When this feature lands, **new** FR-MCP-USECASE / TR-MCP-USECASE / TEST-MCP-USECASE ids must appear in mapping + matrix so the existing gate stays green.

### 8.2 Runtime UC↔FR coverage (new)

`GetUseCaseFrCoverageQuery` / `GET /mcpserver/usecases/coverage` reports, for active (non-deleted) workspace data and default interest link type `Realizes`:

- Use cases with no Realizes FR link
- FRs (optionally filtered to effective layer later) with no Realizes use case link

Optional later: Nuke or CI job that calls the API or a library entry point; that is a separate TR if added.

---

## 9. GraphRAG (Phase 3 optional)

When GraphRAG is enabled for the workspace:

- On create/update of use case header or steps, optionally ingest a text projection via existing GraphRAG ad-hoc ingest command/path (`TR-GRAPHRAG-ADHOC-001`): title, brief, flows/steps.
- Document id pattern: `usecase-{workspaceHash}-{useCaseId}` (stable).
- Soft-delete of use case should not leave stale index without a defined policy: v1 policy = **best-effort re-ingest or delete document** on soft-delete if GraphRAG APIs allow; otherwise document as deferred.

Phase 3 is not required for v1 acceptance of REST/MCP/CQRS/storage.

---

## 10. Failure modes

| Case | Behavior |
|------|----------|
| Unknown use case id | NotFound Result → 404 |
| FR missing or not kind fr | NotFound/Validation → 404/400 |
| Duplicate active UC–FR link | Conflict → 409 |
| Invalid actor Type / flow FlowType | Validation → 400 |
| Diagram format not mermaid | Validation → 400 |
| Soft-deleted UC | Treated as not found for default gets |
| Cross-workspace id guess | Query filter hides row → not found |
| Storage unreachable | Typed backend error / health storage field (`TR-MCP-HEALTH-003`); do not flip liveness falsely |

---

## 11. Phased rollout (Byrd)

Each phase: write/update FR/TR/TEST for the slice → **red** unit tests → mocks green → implement → full slice suite green (0 fail, 0 skip) → next phase.

### Phase 0 — Storage (migrations + entities)

- Entities, DbContext, filters, soft-delete, workspace FK, 3 providers
- Tests: schema create, FK to FR string id, unique link, soft delete hides rows

### Phase 1 — CQRS core + FR links

- Commands/queries for create/get/list/update/delete/link/unlink/from-fr/coverage
- Handler unit tests; DI registration both hosts
- FR `linkedUseCases` projection

### Phase 2 — Mermaid + REST + MCP + client

- Diagram service + diagram query
- `UseCasesController`
- MCP tools + client + tests
- Manual smoke: create, link FR, get Mermaid

### Phase 3 — GraphRAG + polish

- Optional ingest hooks
- Docs/context notes for agents
- Effort buffer for federation/txn gating review if turn-transactions apply to new mutations

**Effort:** larger than v1’s ~6 days; plan as multi-slice BDPv4 work. Do not treat v1’s 6-day estimate as binding.

---

## 12. Implementation checklist

1. Ingest FR/TR/TEST + mappings via MCP requirements tools  
2. Tests first for Phase 0  
3. Entities + migrations (x3)  
4. CQRS commands/queries/handlers + DI  
5. REST controller (dispatcher only)  
6. FR projection  
7. Mermaid service  
8. MCP tools + client + JsonContext  
9. Coverage API  
10. Phase gates: Compile, filtered tests, ValidateTraceability  
11. Update this doc status to Implemented when Phase 2 gates pass  

Template paths to mirror:

- CQRS: `McpServer.GraphRag` Commands/Queries + `ServiceCollectionExtensions`
- REST + MT: `MemoryController`, middleware pipeline
- Multi-provider: Memory / Requirements migrations
- MCP: `FwhMcpTools.Triage.cs`
- Client: `MemoryClient` + `McpClientJsonContext`

---

## 13. Out of scope (v2.0 product v1)

- Blazor / built-in diagram UI  
- Non-Mermaid formats  
- Use case versioning / approval workflow  
- Product multi-workspace sharing (`MCP-PRODUCTS-001`)  
- Changing FR id scheme or TR/TEST mapping tables  
- Overloading docs `ValidateTraceability` with DB UseCaseFrLinks  

---

## 14. Changelog from v1.0

| v1.0 | v2.0 |
|------|------|
| `FrId BIGINT` | `FrId string(128)` → `RequirementEntity` |
| `FunctionalRequirement` type | `RequirementEntity` navigation |
| SQL Server IDENTITY-only SQL | Multi-provider EF migrations |
| `WorkspaceId NVARCHAR(50)` | `WorkspaceId` 1024 + FK Workspaces |
| No soft delete | Soft delete on durable rows |
| No audit | Audit ledger required |
| CQRS named only | Full command/query/handler design |
| Diagram service owns query | Query handler loads; diagram pure |
| Extend docs ValidateTraceability for UC links | Separate runtime coverage API |
| Agent tools only | REST + MCP tools + typed client |
| ~6 day estimate | Multi-phase Byrd; estimate not binding |
| Missing feature FR/TR/TEST | Explicit ID set in section 2 |

---

## 15. Acceptance criteria (v1 product done = Phase 2 complete)

1. Create use case with basic flow/steps via REST and MCP in a workspace.  
2. Link to existing string FR id with default Realizes; FR get shows use case; from-fr works.  
3. Diagram endpoint returns Mermaid for that structure.  
4. Coverage query reports Realizes gaps.  
5. Soft-delete hides use case from default get/list; no physical cascade of durable rows.  
6. UseCase* unit tests green with zero skips in gate scope; ValidateTraceability green for new requirement IDs.  
7. Controllers dispatch only through CQRS; no service-layer bypass of handlers for domain mutations.  

**End of Document**
