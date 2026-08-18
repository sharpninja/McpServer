# Plan: MCP-PRODUCTS-001 Products as workspace context sharing

**TODO:** MCP-PRODUCTS-001 (high, Architecture, `Done: true`)
**Process:** Byrd Development Process v4 (`docs/Development-Process-draft-v4.md`)
**Status:** Implemented. Skeptic rerun H5-done AGREE `docs/receipts/hostile-validator-20260818T174337Z.md`. MCP-PRODUCTS-001 `Done: true`. Live host remains 1.4.26 until operator asks for Nuke UpdateService.
**Breaking change:** No. Existing workspace-scoped requirement CRUD and `listFr` stay local. Sharing is additive on `GetEffectiveRequirements` and an explicit context source.
**Hostile gates:** Named checkpoints H0, H1-red, H1-green, H2-red, H2-green, H3-red, H3-green, H4-red, H4-green, H5-done. AGREE required to proceed. See Hostile validation checkpoints.

## Problem

Agents working in one workspace cannot see FR/TR/TEST/layers that already exist in a sibling repo without copying records. Copies drift. Use cases already have an optional `ProductKey` string hook (`FR-MCP-USECASE-009`) that lists use cases by key but does not share requirements.

A Product is the missing grouping: a workspace belongs to zero or more products. Membership exposes sibling workspaces' requirement records into the caller's effective set and into context, without duplicating rows.

## Value

One product (for example McpServer plus its plugins) can share a requirement catalog across repos while each workspace remains the owner of its own rows. Agents reason across related repositories through provenance-tagged reads.

## Locked decisions

1. **Ownership.** A requirement row stays in its origin workspace. Composite PK remains `(WorkspaceId, Kind, Id)`. Products never copy FR/TR/TEST rows.

2. **Scope of sharing.** Product membership shares FR, TR, TEST, acceptance criteria, traceability mappings, and origin-workspace layer catalogs. It does **not** share TODOs, session logs, source-file context chunks, GraphRAG, memories, or use-case structure (except the existing `ProductKey` hook).

3. **Host-local only.** v1 products exist only among workspaces registered in the same host `McpDbContext`. Federation/hub/proxy product membership is out of scope.

4. **Membership.** A product has exactly one owner workspace (`OwnerWorkspaceId` = creator). The owner adds and removes member workspace IDs. A member may leave itself. Only the owner may rename, disable, or soft-delete the product. Adding a workspace requires that workspace to be registered, enabled, and not soft-deleted. No invite tokens in v1.

5. **Consent.** Joining (being added) is the consent. After that, `GET /mcpserver/requirements/effective` defaults to product union. `listFr` / `getFr` / create / update / delete stay local. Effective accepts `productScope=local|product` (default `product`) so callers can preview local-only.

6. **Identity collisions.** The same FR id in two workspaces is two records. Effective and context results carry `originWorkspaceId` (and origin workspace name). Composite identity is `(originWorkspaceId, kind, id)`. Never merge or overwrite on id alone.

7. **Layer matching.** The caller resolves `layerKey` (explicit preview or the calling workspace's `CurrentRequirementLayerKey`). A sibling row is included only if the **origin** workspace has that layer key and the row is effective against the origin layer catalog's order. If the origin workspace has no matching layer key, exclude the sibling row. Do not invent a mapping.

8. **Mutations.** Foreign (sibling) requirement rows are read-only. Local update/delete of an id that exists only in a sibling returns the current local 404. Product APIs do not mutate requirement rows.

9. **Use case ProductKey.** Registered `Product.Key` values are `PROD-*` strings (example `PROD-MCPSERVER`). `UseCaseEntity.ProductKey` remains an unconstrained hook in v1 (preserves `FR-MCP-USECASE-009`). Product CRUD uniqueness is among non-deleted `PROD-*` keys. v1 does not require a registered Product before setting a use-case ProductKey.

10. **Authorization.** All product routes stay behind `/mcpserver/*` API-key auth. The caller's API key still binds to one workspace. That workspace may read sibling requirements only if it is an active member of a shared product. A leaked workspace key cannot list all products or dump non-member workspaces.

11. **Storage.** Products and memberships are host-global tables (no workspace EF query filter). Authorization is in CQRS handlers. Soft-delete on products and memberships. Append-only audit on every product mutation (`TR-MCP-DB-004`). Multi-provider migrations for SQLite, PostgreSQL, and SQL Server. Turn-transaction gating on mutating product commands, matching other domain writes.

12. **Context.** Context search/pack gains an optional source type `product-requirements`. When requested (and by default when packing for a member workspace), include text chunks synthesized from product-visible requirement rows, tagged with origin workspace. Do not pull sibling source files.

13. **Surfaces.** Always CQRS. REST, MCP STDIO, typed `ProductClient`, REPL, and plugin tools are thin adapters over commands/queries. New standalone domain services are not allowed except as private helpers that exist only to service the CQRS layer (handlers own the public application API).

14. **Product key format.** `Product.Key` is a string matching `^PROD-[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$`. Examples: `PROD-MCPSERVER`, `PROD-MCP-PLUGIN`. Reject lowercase, missing `PROD-` prefix, or empty. Unique among non-deleted products. Use-case `ProductKey` remains an unconstrained hook in v1 (`FR-MCP-USECASE-009`).

15. **No Python.** Tests and verification are `pwsh.exe` / `dotnet test` only.

16. **Hostile checkpoints.** Every phase has a named hostile checkpoint (see Hostile validation checkpoints). Do not start the next phase, and do not mark MCP-PRODUCTS-001 done, without OverallVerdict AGREE on that checkpoint.

## Out of scope

- Federated/hub product membership
- Sharing TODOs, session logs, or repo files
- Automatic ProductKey validation on use-case write
- Merging SharpMind or QuadBrain strategy work
- UI beyond REST/MCP (no first-party Products page in v1)
- Copy/sync of requirement rows between databases

## Requirements to create (Phase 0, before any product code)

Create these in the MCP requirements store, map FR to TR and TEST, then export `docs/Project` so `./build.ps1 ValidateTraceability` is green. IDs do not exist today (`FR-MCP-PRODUCT` grep is empty).

**Functional**

- **FR-MCP-PRODUCT-001** Product CRUD. A registered workspace can create, get, list (visible), update, and soft-delete products it owns. Key is a `PROD-*` string (`PROD-MCPSERVER`, `PROD-MCP-PLUGIN`) unique among non-deleted products. AC: create returns key + owner; invalid key is 400; duplicate key is 409; non-owner update/delete is 403; soft-delete hides from default list.
- **FR-MCP-PRODUCT-002** Workspace membership. A workspace maps to zero or more products. Owner adds/removes members by workspace id. Member can leave. AC: add requires registered enabled workspace; list members returns owner + members; leave removes only the caller; removed member loses product reads.
- **FR-MCP-PRODUCT-003** Shared effective requirements. Member `GetEffectiveRequirements` unions local effective rows with sibling members' effective rows, provenance-tagged, layer-matched per origin catalog. AC: two workspaces in one product see each other's in-scope FR/TR/TEST/mappings; `productScope=local` hides siblings; id collision returns two rows with different `originWorkspaceId`.
- **FR-MCP-PRODUCT-004** Isolation. Non-members cannot read product membership or sibling requirements. Local requirement mutations never write sibling rows. AC: outsider get-product is 404; outsider effective is local-only; update-fr on sibling id does not change the sibling row.
- **FR-MCP-PRODUCT-005** Product requirement context. Context search/pack can retrieve product-visible requirement text with origin tags, not sibling source files. AC: pack for a member includes sibling FR body; pack does not include sibling `.cs` chunks; source type `product-requirements` filters to those chunks.

**Technical**

- **TR-MCP-PRODUCT-MODEL-001** Entities `ProductEntity`, `ProductWorkspaceMembershipEntity`; host-global; soft-delete; unique `Key` matching `^PROD-[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$`; FK to `Workspaces`; audit rows; migrations apply on SQLite, PostgreSQL, SQL Server without re-adding unrelated columns.
- **TR-MCP-PRODUCT-SHARE-001** CQRS query path (private helper only) ignores workspace query filter only for member `WorkspaceId`s of the caller's products; layer filter uses origin catalog; results include `originWorkspaceId`.
- **TR-MCP-PRODUCT-API-001** CQRS commands/queries in `McpServer.Support.Mcp/Products/`. REST `/mcpserver/products`, MCP `product_*`, `ProductClient`, REPL `client.Products`, and plugin descriptors dispatch those handlers only. Effective endpoint gains `productScope`.
- **TR-MCP-PRODUCT-AUTH-001** Authorization lives in CQRS handlers (or a helper used only by those handlers); transaction gating on mutating product commands.
- **TR-MCP-PRODUCT-CTX-001** Context indexer/search path for `product-requirements` chunks derived from the CQRS share helper, not from sibling `ContextDocument` rows.

**Testing**

- **TEST-MCP-PRODUCT-001** Entity/membership unit tests (isolation, `PROD-*` key accept/reject, unique key, soft-delete).
- **TEST-MCP-PRODUCT-002** Share helper unit tests (union, collision, layer miss, leave).
- **TEST-MCP-PRODUCT-003** Controller/API unit tests (403/404/409 mapping).
- **TEST-MCP-PRODUCT-004** Client + MCP tool contract tests.
- **TEST-MCP-PRODUCT-005** Migration apply on empty and production-shaped DBs (all available providers).
- **TEST-MCP-PRODUCT-006** Context pack/search does not leak sibling source files; includes requirement chunks for members.

**Mappings**

- FR-MCP-PRODUCT-001 -> TR-MCP-PRODUCT-MODEL-001, TR-MCP-PRODUCT-API-001, TR-MCP-PRODUCT-AUTH-001 -> TEST-MCP-PRODUCT-001, TEST-MCP-PRODUCT-003, TEST-MCP-PRODUCT-004, TEST-MCP-PRODUCT-005
- FR-MCP-PRODUCT-002 -> TR-MCP-PRODUCT-MODEL-001, TR-MCP-PRODUCT-AUTH-001 -> TEST-MCP-PRODUCT-001, TEST-MCP-PRODUCT-003
- FR-MCP-PRODUCT-003 -> TR-MCP-PRODUCT-SHARE-001, TR-MCP-PRODUCT-API-001 -> TEST-MCP-PRODUCT-002, TEST-MCP-PRODUCT-004
- FR-MCP-PRODUCT-004 -> TR-MCP-PRODUCT-AUTH-001, TR-MCP-PRODUCT-SHARE-001 -> TEST-MCP-PRODUCT-002, TEST-MCP-PRODUCT-003
- FR-MCP-PRODUCT-005 -> TR-MCP-PRODUCT-CTX-001 -> TEST-MCP-PRODUCT-006

After store create: `requirements_generate` `doc=all` `format=markdown` (and wiki if that is the current export path). Link these IDs onto MCP-PRODUCTS-001 via TODO update.

## Public interfaces (locked)

**REST** (`X-Api-Key` + workspace resolution unchanged)

- `POST /mcpserver/products` body `{ key: "PROD-MCPSERVER", name, description? }` -> product (caller is owner)
- `GET /mcpserver/products` -> products the caller owns or is a member of
- `GET /mcpserver/products/{key}`
- `PATCH /mcpserver/products/{key}` name/description (owner)
- `DELETE /mcpserver/products/{key}` soft-delete (owner)
- `GET /mcpserver/products/{key}/members`
- `PUT /mcpserver/products/{key}/members/{workspaceId}` add (owner)
- `DELETE /mcpserver/products/{key}/members/{workspaceId}` remove (owner, or self-leave)
- `GET /mcpserver/requirements/effective?layerKey=&productScope=product|local`

**DTO additions**

- `FrEntry` / `TrEntry` / `TestEntry` already have `workspaceId`. Keep it as origin. Effective result adds optional `productKeys: string[]` on the envelope so callers know which products contributed.
- Product DTO: `key`, `name`, `description`, `ownerWorkspaceId`, `memberWorkspaceIds`, `createdAtUtc`, `updatedAtUtc`.

**MCP tools** (STDIO / streamable HTTP): `product_create`, `product_list`, `product_get`, `product_update`, `product_delete`, `product_list_members`, `product_add_member`, `product_remove_member`. `requirements_effective` gains `productScope`.

**Client:** `McpServerClient.Products` (`ProductClient`) + JSON context registrations + `ENDPOINTS.md`.

**REPL:** `client.Products.*` allow-listed like `client.UseCases`.

**CQRS (mandatory):** commands/queries/handlers live under `McpServer.Support.Mcp/Products/` (`ICommand` / `IQuery` / handlers / `Dispatcher` / `Result<T>`), registered on HTTP and STDIO hosts the same way use cases are. Controllers, MCP tools, REPL, and `ProductClient` only dispatch. New public `IProductService`-style facades are forbidden. Private helpers are allowed only when called from handlers (for example membership authorization or the share query). This is `FR-MCP-029` / `TR-MCP-CQRS-*`, not optional.

## Data model

`ProductEntity`

- `ProductId` long PK
- `Key` string 128, format `PROD-MCPSERVER` (`^PROD-[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$`), unique filtered where `IsDeleted = false`
- `Name` string 512
- `Description` string? 
- `OwnerWorkspaceId` string 1024 FK Workspaces
- timestamps + soft-delete columns (same shadow/pattern as other domain rows)

`ProductWorkspaceMembershipEntity`

- `ProductId` + `WorkspaceId` composite PK
- `Role` string (`Owner` or `Member`)
- `AddedAtUtc`, `AddedBy`
- soft-delete columns (leave/remove is soft-delete so audit remains)

Indexes: `Key`, `OwnerWorkspaceId`, `WorkspaceId`.

Do not put a workspace query filter on these tables.

## Failure modes

- Invalid product key (not `PROD-*` canonical): 400
- Duplicate product key: 409
- Unknown workspace member: 400 with registered-workspace message
- Non-owner mutate: 403
- Non-member get: 404 (do not leak existence to strangers; list is visibility-filtered)
- Transaction coordinator degraded: mutating product routes fail closed
- Sibling layer key missing: sibling row omitted, no error
- Soft-deleted product: membership reads stop; effective reverts to local

## Migration / backfill

- New migration in all three provider projects.
- No backfill of use-case ProductKey into Product rows.
- Existing effective behavior unchanged until a workspace is a member of at least one product.
- Migration apply tests must not recreate SessionLogs agent-header columns (lesson from use-case migrations).

## Rollout

- Branch from `develop`: `feat/mcp-products-001`
- Deploy only via Nuke `UpdateService` after full unit suite green (Failed 0, Skipped 0 in the executed gate).
- No config flag required; empty product tables keep current behavior.

## Byrd phases

Each implementation slice: write AC-covering unit tests first (shown red), mocks/stubs where the seam is a CQRS handler, implement until those tests are green without skips, then run the current + prior unit suite for that gate. Hostile checkpoint AGREE required before the next phase (see Hostile validation checkpoints).

### Phase 0: Requirements and plan artifacts

- Create FR/TR/TEST/AC/mappings in MCP store; export docs; attach IDs to MCP-PRODUCTS-001.
- **No product code.**
- Validation: `requirements_list` / get for each new id; `./build.ps1 ValidateTraceability`.
- **Hostile checkpoint H0** (requirements exist, AC testable, mappings complete) before Phase 1.

### Phase 1: Model and membership (FR-001, FR-002, TR-MODEL, TR-AUTH)

**Red tests first**

- `tests/McpServer.Support.Mcp.Tests/Storage/ProductEntityTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Products/CreateProductCommandHandlerTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Products/AddProductMemberCommandHandlerTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Storage/ProductMigrationApplyTests.cs`

Named cases: accept `PROD-MCPSERVER`; reject `mcpserver` / empty / missing prefix (400); unique key; owner add/remove; self-leave; reject unknown workspace; soft-delete hides product; non-owner cannot add.

**Hostile checkpoint H1-red** after tests exist and fail for the right reason (not compile errors).

**Green:** entities, migrations, CQRS commands/queries/handlers for product CRUD and membership. Private helper only if a handler calls it. No public `IProductService`. Audit emission from handlers.

**Gate:** `dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter FullyQualifiedName~Product`

**Hostile checkpoint H1-green** before Phase 2.

### Phase 2: Shared effective requirements (FR-003, FR-004, TR-SHARE)

**Red tests first**

- `tests/McpServer.Support.Mcp.Tests/Products/GetProductEffectiveRequirementsQueryHandlerTests.cs`
- Extend existing effective-requirements tests so a workspace with zero products is unchanged.

Named cases: union; `productScope=local`; collision two origins; layer miss excludes; leave drops sibling; outsider cannot share.

**Hostile checkpoint H2-red.**

**Green:** `GetEffectiveRequirements` (or a dedicated query dispatched from the existing requirements path) uses a handler-owned share helper; DTO provenance.

**Gate:** Support.Mcp.Tests Product* + Requirement*effective/scope filters, Failed 0 Skipped 0.

**Hostile checkpoint H2-green** before Phase 3.

### Phase 3: API, client, MCP, REPL (TR-API)

**Red tests first**

- `tests/McpServer.Support.Mcp.Tests/Controllers/ProductsControllerTests.cs`
- `tests/McpServer.Client.Tests/ProductClientTests.cs`
- MCP/REPL allow-list tests following the use-case CQRS dispatch pattern
- Plugin descriptor tests if Node descriptors are generated in this repo

**Hostile checkpoint H3-red.**

**Green:** thin controller and `FwhMcpTools.Products.cs` that only call `IDispatcher`; `ProductClient`; JSON context; REPL passthrough; plugin sync list if required.

**Gate:** those test projects' Product filters plus existing RequirementsClient effective tests.

**Hostile checkpoint H3-green** before Phase 4.

### Phase 4: Context pack (FR-005, TR-CTX)

**Red tests first**

- `tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs`

Named cases: member pack contains sibling FR text + origin; does not contain sibling source path chunks; non-member pack does not contain sibling FR.

**Hostile checkpoint H4-red.**

**Green:** pack/search contribution via CQRS query or a helper used only by that query; source type `product-requirements`.

**Gate:** context + product test filters.

**Hostile checkpoint H4-green** before Phase 5.

### Phase 5: Integration, traceability, docs, deploy

- Integration tests with two workspaces on one factory if the harness allows; otherwise two-`WorkspaceId` handler tests remain the proof and this phase adds HTTP integration on CustomWebApplicationFactory with workspace header switching.
- `docs/USER-GUIDE.md` / `docs/MCP-SERVER.md` product section.
- Client `ENDPOINTS.md`.
- Wiki export of requirements.
- Full unit suite: `./build.ps1 Test` (excludes `*.IntegrationTests` per Nuke Test). Failed 0, Skipped 0.
- **Hostile checkpoint H5-done** on the done claim (architecture, CQRS-only, PROD-* keys, isolation, no source-file leak). Only then consider MCP-PRODUCTS-001 `done: true`.
- Deploy: Nuke `UpdateService` only if operator asks to ship.

## Implementation task mapping (existing TODO tasks)

1. Domain model + mapping rules: Phase 1
2. Requirements/layer sharing + effective: Phase 2
3. API/MCP/REPL/plugin: Phase 3
4. Authorization/isolation/conflicts: Phases 1-2 (tests in both)
5. Unit/integration/traceability/context: Phases 1-5

## Hostile validation checkpoints

Use the workspace `hostile-validator` skill. Class 1 (project implementation). Surface C applies. Parent must run add-profile in the hostile brief. AGREE required to proceed. Receipt under `docs/receipts/hostile-validator-<utc>.md` plus `.json`. Do not mark a phase or MCP-PRODUCTS-001 done on DISAGREE.

- **H0** after Phase 0, before Phase 1. Attack: FR/TR/TEST/AC exist in the MCP store; mappings complete; key format and CQRS-only are in the FR/TR text; no product implementation started.
- **H1-red** after Phase 1 tests exist, before implementation. Attack: tests cover `PROD-MCPSERVER` accept and invalid-key reject; membership ACs; tests are red for the right reason.
- **H1-green** after Phase 1 implementation, before Phase 2. Attack: handlers not a public service facade; key validation; owner/member rules; migrations; Phase 1 gate green with zero skips.
- **H2-red** after Phase 2 tests exist. Attack: union, local scope, collision, layer miss, leave, outsider isolation are named tests and red.
- **H2-green** after Phase 2 implementation, before Phase 3. Attack: effective share is handler-owned; provenance; no sibling mutation; gate green.
- **H3-red** after Phase 3 tests exist. Attack: controller/MCP/client tests dispatch CQRS only.
- **H3-green** after Phase 3 implementation, before Phase 4. Attack: no new public domain service; tools/REPL/client wired; gate green.
- **H4-red** after Phase 4 tests exist. Attack: member requirement chunks vs no sibling source files.
- **H4-green** after Phase 4 implementation, before Phase 5. Attack: `product-requirements` source; no file leak; gate green.
- **H5-done** after Phase 5 full `./build.ps1 Test` (Failed 0, Skipped 0) and ValidateTraceability. Attack: all five FRs, CQRS-only, `PROD-*` keys, isolation, DoD. Only then `done: true`.

## DoD for MCP-PRODUCTS-001

- All five FR ACs have named tests that passed in the Phase 5 gate
- Zero skipped tests in that gate
- ValidateTraceability green
- Hostile OverallVerdict AGREE on the done claim
- `todo_get MCP-PRODUCTS-001` still `Done: false` until that AGREE exists

## Open operator overrides

These are locked above. Change them only by amending this plan:

- Owner-add membership (not invite tokens)
- Effective defaults to product union once joined
- No sibling source-file sharing
- No federation in v1
- Use-case ProductKey remains a hook, not a FK
- CQRS only; no public product service facade
- Product keys are `PROD-*` strings (`PROD-MCPSERVER`)
