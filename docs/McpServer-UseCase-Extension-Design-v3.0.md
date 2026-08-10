# McpServer Use Case Extension – Design & Implementation Package v3.0

**Date:** 2026-08-08  
**Author:** GrokCode (post-failure audit replan)  
**Status:** Active implementation plan (prior v2 implementation work is unproven; treat as salvage candidate only)  
**Supersedes:** `docs/McpServer-UseCase-Extension-Design-v2.0.md` as the **active plan of record**  
**Audit basis:** `docs/receipts/usecase-plan-requirements-audit-20260808.md`, `docs/receipts/usecase-design-tr-audit-20260807T141204Z.md`, Claude requirements inventory `docs/receipts/requirements-list-summary-20260807T140215Z.md`

---

## 0. Why v3 exists

Design v1 failed a TR audit. Design v2 corrected the sketch and required **requirements ingestion before implementation**, multi-provider storage, soft-delete, CQRS, dual surfaces, and BDPv4 gates.

Implementation work that followed v2 (and an operator-expanded active plan) **did not** complete those gates:

1. FR/TR/TEST USECASE IDs were never ingested into the MCP requirements store or `docs/Project/*`.
2. Storage “proof” was compile + `EnsureCreated` unit tests, not migration **apply** on production-shaped databases.
3. Nuke `UpdateService` to ProgramData SQL Server **crashed** applying `AddUseCaseSupport` (SessionLogs column re-add drift).
4. Append-only audit (TR-MCP-DB-004 / design TR-USECASE-006) was not implemented for Use Case mutations.
5. Prior plan checkmarks claimed completion without requirements-backed acceptance criteria or deploy proof.

**v3 is not a product redesign.** It is a process-correct reimplementation plan: capture requirements for real, prove storage and audit, re-validate domain and surfaces, deploy only via Nuke `UpdateService`.

Salvage of existing code is allowed only after the corresponding slice’s red tests and requirements exist. Salvage is not a free pass past BDPv4.

---

## 1. Authority and scope

### 1.1 Document authority order

1. This v3 plan (active).
2. Operator decisions recorded in session (UI required; former design-v2 OOS items in scope).
3. Design v2 for schema and CQRS shape where not contradicted by operator or this audit.
4. Design v1 is historical only.

### 1.2 Product intent (locked)

- Default UC↔FR `LinkType`: **Realizes**
- Naming: `UseCase*`, route `/mcpserver/usecases`, MCP tools `usecase_*`, client `UseCases`
- FR identity: **string** `RequirementEntity.Id` where `Kind = fr` (never bigint)
- Application pattern: **CQRS** (`ICommand` / `IQuery` / handlers / `Dispatcher` / `Result<T>`)
- Mermaid remains **primary** diagram format; **at least one non-Mermaid** format is in scope (operator)
- **First-party UI** is in scope (operator; supersedes design v2 “no in-server UI”), including **built-in diagram view and edit** (not external-only, not text dump alone)
- Versioning/approval, ProductKey multi-workspace **hooks**, plugin dual surface, and UseCaseFrLinks Realizes coverage shared with a documented validation seam are in scope (operator)
- **Hostile validator on every step and every status report** (operator trust requirement; section 6.1)

### 1.3 Platform constraints (Claude TR audit; non-negotiable)

Must remain true for any Use Case work:

- Multi-tenant: `WorkspaceId` + query filters (FR-MCP-043/044, TR-MCP-MT-*)
- Soft-delete Restrict/NoAction durable FKs (TR-MCP-DB-003)
- Workspace FK (TR-MCP-DB-002)
- Append-only audit on mutable domain (TR-MCP-DB-004)
- Multi-provider EF (TR-MCP-CFG-007): SQLite, PostgreSQL, SQL Server
- CQRS (FR-MCP-029, TR-MCP-CQRS-*)
- Auth middleware for `/mcpserver/*` (FR-MCP-013, TR-MCP-AUTH-*)
- Dual surface REST + MCP + typed client (TR-MCP-API / TR-MCP-CLIENT patterns)
- DI ownership (TR-MCP-ARCH-002)

---

## 2. Requirements to create (blocking before implementation slices)

All IDs must be created in the **MCP requirements store**, mapped FR→TR→TEST, and projected into `docs/Project` (Functional, Technical, Testing, TR-per-FR mapping, Requirements-Matrix) so `./build.ps1 ValidateTraceability` is green for the new IDs.

### 2.1 Functional

| ID | Title | Acceptance (summary) |
|----|--------|----------------------|
| FR-MCP-USECASE-001 | Use case header CRUD | Workspace-scoped create/get/list/update/soft-delete headers |
| FR-MCP-USECASE-002 | Actors, flows, steps | Attach actors; Basic/Alternative/Exception flows; ordered steps |
| FR-MCP-USECASE-003 | UC↔FR links | Bidirectional; default Realizes; unlink soft-deletes link |
| FR-MCP-USECASE-004 | Create from FR | Shell UC from FR with auto Realizes link |
| FR-MCP-USECASE-005 | Diagrams | Mermaid primary; at least one additional format (e.g. plantuml) |
| FR-MCP-USECASE-006 | Realizes coverage | Runtime report of UC/FR gaps for Realizes |
| FR-MCP-USECASE-007 | First-party UI | List/edit/diagram/coverage via REST only |
| FR-MCP-USECASE-008 | Versioning / approval | Draft/Submitted/Approved/Rejected; version increments on Approve |
| FR-MCP-USECASE-009 | Product membership hooks | Optional ProductKey; list-by-product (no full products subsystem) |
| FR-MCP-USECASE-010 | Traceability integration | Shared Realizes algorithm for tooling + runtime; docs matrix still holds USECASE FR/TR/TEST IDs |

### 2.2 Technical

| ID | Title | Acceptance (summary) |
|----|--------|----------------------|
| TR-MCP-USECASE-001 | Storage | 4NF entities; soft-delete; workspace FK/filter; **multi-provider migrations that apply cleanly** on empty and production-shaped DBs; migrations **must not** re-add unrelated columns already present (e.g. SessionLogs agent fields) |
| TR-MCP-USECASE-002 | CQRS | Commands/queries/handlers registered; `AddCqrsDispatcher` + Use Case handlers on HTTP and STDIO hosts |
| TR-MCP-USECASE-003 | REST | Thin `/mcpserver/usecases` controller; Result→HTTP mapping |
| TR-MCP-USECASE-004 | Diagram service | DI-owned pure generator; mermaid + one extra format; unknown format validation |
| TR-MCP-USECASE-005 | MCP + client + plugins | `usecase_*` STDIO tools; `UseCaseClient` DTO parity with live JSON; plugin-core tools + agent skills; REPL `client.UseCases` allow-list |
| TR-MCP-USECASE-006 | Projection, coverage, audit | FR get/list `linkedUseCases`; coverage DTO; **every mutable Use Case mutation emits TR-MCP-DB-004 audit rows** |
| TR-MCP-USECASE-007 | UI hosting | Static files for `/usecases/`; no domain mutations outside REST |
| TR-MCP-USECASE-008 | Approval/product API | Endpoints + CQRS for approval transitions and ProductKey |
| TR-MCP-USECASE-009 | Validation seams | Docs ValidateTraceability includes new USECASE IDs; UseCaseFrLinks Realizes findings use shared algorithm (explicit seam, not silent docs overload) |
| TR-MCP-USECASE-010 | Deploy | Service deploy only via Nuke `UpdateService` (config backup/restore); live health + routes after upgrade |

### 2.3 Testing

| ID | Title | Acceptance (summary) |
|----|--------|----------------------|
| TEST-MCP-USECASE-001 | Handler/service unit | CRUD, isolation, soft-delete, FR link (0 skip) |
| TEST-MCP-USECASE-002 | Controller unit | Result mapping and routes |
| TEST-MCP-USECASE-003 | Diagram golden | Mermaid + non-Mermaid |
| TEST-MCP-USECASE-004 | Client unit | Live JSON shapes including coverage and expanded detail |
| TEST-MCP-USECASE-005 | Coverage | Runtime coverage gaps |
| TEST-MCP-USECASE-006 | Migration apply | Empty DB + production-shaped DB (SessionLogs agent columns already present); all providers covered by available harnesses |
| TEST-MCP-USECASE-007 | Audit emission | Create/update/delete/link emit audit rows |
| TEST-MCP-USECASE-008 | UI assets | HTML/JS REST-only; no DbContext |
| TEST-MCP-USECASE-009 | Approval/product | Version/status and ProductKey list-by-product |
| TEST-MCP-USECASE-010 | Plugin core | Jest (or equivalent) for usecase_* routing to client.UseCases.* |
| TEST-MCP-USECASE-011 | Deploy smoke | After UpdateService: health, /usecases/, create/link/diagram/coverage |

---

## 3. Data model (from v2, locked)

### 3.1 Conventions

- `WorkspaceId` string(1024), FK Workspaces, query filter
- Soft-delete shadow metadata consistent with sibling entities
- Durable FKs Restrict/NoAction
- Surrogate `long` keys per provider
- `FrId` string(128) → Requirement Kind=fr
- Soft-delete unlink for UseCaseFrLinks
- Unique active (WorkspaceId, UseCaseId, FrId)

### 3.2 Tables

UseCases (plus VersionNumber, ApprovalStatus, ProductKey for operator scope), Actors, UseCaseActors, UseCaseFlows, UseCaseSteps, UseCaseSpecialRequirements, UseCaseExtensionPoints, UseCaseFrLinks.

### 3.3 Migration rules (new in v3 – hard)

1. Migrations for this feature are **Use Case scoped**. No opportunistic SessionLogs or other domain alters.
2. **TEST-MCP-USECASE-006** must fail if apply re-adds existing columns or fails on production-shaped history.
3. `EnsureCreated` unit fixtures are allowed for handler tests but **do not** satisfy TR-MCP-USECASE-001.

---

## 4. CQRS, REST, MCP, client (from v2 + expansions)

Minimum command/query set as design v2, plus:

- `SetUseCaseApprovalStatusCommand`
- `SetUseCaseProductKeyCommand`
- `ListUseCasesByProductQuery`
- Diagram format parameter (mermaid primary; plantuml or other implemented format)

REST routes as design v2 plus:

- `POST .../{id}/approval`
- `POST .../{id}/product`
- `GET .../by-product/{productKey}`

MCP tools: list/get/create/update/delete/link/from-fr/diagram/coverage/set_approval/set_product/list_by_product.

Client DTO property names **must match** live JSON (coverage and detail expanded fields).

---

## 5. Validation seams (v3 policy)

| Seam | Role |
|------|------|
| Docs / Nuke ValidateTraceability | FR/TR/TEST **IDs** in Project docs matrix (includes new USECASE IDs) |
| Runtime coverage API | UC↔FR Realizes gaps for workspace data |
| Shared algorithm | Single pure evaluator used by runtime API, gate, and optional Nuke DB findings path |

Operator requires UseCaseFrLinks Realizes to participate in validation tooling; design v2 “do not overload docs validator” is resolved by: **docs matrix stays ID-based**; **DB Realizes findings are a separate, documented TR** sharing algorithm code.

---

## 6. BDPv4 process (non-negotiable)

For every slice R0–R7:

1. Requirements first (store + docs) for that slice’s IDs/ACs.
2. Write red tests for the next small behavior / seam.
3. Mocks green where applicable.
4. Implement until slice suite is green (0 fail, 0 skip).
5. Full prior+current Use Case filter remains green before exit.
6. Receipts under private scratch for every gate command.
7. No checkbox without cited evidence.
8. Deploy only via `./build.ps1 UpdateService` (elevated). Never ad-hoc overwrite of ProgramData that bypasses backup/restore.
9. **Hostile validator on every step** (section 6.1). No exceptions.

### 6.1 Hostile validator (mandatory on every step and every status report)

Operator trust requirement (2026-08-08): agents may not self-certify progress.

**Definition:** The hostile validator is an **adversarial Grok sub-agent** (separate spawn), not the implementer, not Codex, and not a PowerShell script the implementer wrote. Skill: `hostile-validator` (`~/.grok/skills/hostile-validator/SKILL.md` and workspace `.grok/skills/hostile-validator/SKILL.md`).

It defaults every claim to **FAIL** or **UNKNOWN** until **it** re-verifies with tools. It does not trust implementer narrative, plan `[x]` checkboxes, or implementer-authored receipts without re-checking.

**Forbidden substitutes:**

- Implementer self-review labeled “hostile”
- `tools/powershell/Invoke-HostileValidator.ps1` as the validator (optional evidence collector only if the sub-agent chooses)
- Codex plan review (operator will not burn Codex to babysit the implementer)

**When it must run:**

1. Before marking any R0–R7 step complete.
2. Before any status report that claims pass, green, done, complete, or deploy success.
3. After any material code or requirements change that could invalidate prior receipts.
4. Before updating plan checklists.

**How to run:**

Parent implementer spawns a Grok sub-agent with the adversarial brief from the `hostile-validator` skill. Fill in concrete claims. Do not pre-load AGREE. Prefer `subagent_type=general-purpose` with execute capability when tests or live HTTP checks are required.

**Outputs (required):**

- Markdown receipt written by the sub-agent: `docs/receipts/hostile-validator-<utc>.md`
- JSON twin: `docs/receipts/hostile-validator-<utc>.json`
- `ValidatorIdentity: GrokSubagentHostile`
- `OverallVerdict`: **AGREE** only if every claim is PASS; else **DISAGREE**

**Status report contract:**

Every agent status report that asserts truth about this work MUST include:

1. Path to the latest hostile-validator receipt (sub-agent written).
2. `OverallVerdict` value (AGREE or DISAGREE).
3. PASS / FAIL / UNKNOWN counts.
4. Explicit list of any FAIL claims (do not bury them).

If the receipt is missing, not written by the sub-agent, or `OverallVerdict=DISAGREE`, the implementer is **not** authorized to claim step completion. DISAGREE is process success: honesty preserved.

**Operator-in-scope items the hostile validator must keep failing until true:**

- Built-in **diagram view and edit** UI (not external-only; not Mermaid text dump alone).
- Management UI for actors, flows, steps, FR links, coverage, and diagram view/edit.
- Migration apply proof (not EnsureCreated-only).
- Live deploy via Nuke `UpdateService` with health + UI proof.

---

## 7. Phased reimplementation (slices)

Each slice ends only when its gate suite is green **and** hostile validator returns AGREE for that slice’s claims.

### R0 – Requirements authority (blocking)

- Ingest FR/TR/TEST USECASE set (sections 2.1–2.3).
- FR→TR→TEST mapping + matrix rows.
- Gate: ValidateTraceability green for new IDs; requirements list shows IDs.
- Hostile: re-verify docs + store IDs independently.

### R1 – Storage re-proof (blocking)

- Red: TEST-MCP-USECASE-006 migration apply (empty + production-shaped).
- Fix migrations to Use Case–only ops; multi-provider.
- Soft-delete / filter / FrId link tests against **migrated** DB.
- Gate: migration-apply suite green.
- Hostile: re-run migration apply tests; grep migrations for SessionLogs drift.

### R2 – Audit

- Red: TEST-MCP-USECASE-007.
- Emit audit on create/update/delete/link (and approval/product if mutable).
- Gate: audit tests green.
- Hostile: re-run audit emission tests.

### R3 – Domain CQRS

- Re-validate handlers against FR/TR ACs; repair as needed.
- Gate: Support UseCase unit filter 0 fail 0 skip.
- Hostile: re-run full UseCase unit filter.

### R4 – Surfaces

- REST, full MCP tool set, client DTO parity.
- Gate: controller + client tests green.
- Hostile: re-run controller + client filters.

### R5 – Projection + coverage + validation seams

- FR linkedUseCases; coverage; shared Realizes findings path.
- Gate: projection + coverage + traceability tests green.
- Hostile: re-run projection/coverage tests + ValidateTraceability.

### R6 – UI + plugins

- wwwroot UI REST-only tests; plugin-core jest; skills; REPL allow-list.
- **Built-in diagram view/edit UI** (operator-in-scope; not optional polish).
- Gate: UI + plugin receipts + diagram editor proof.
- Hostile: live `/usecases/`; prove diagram editor code exists (not only `<pre>` text dump).

### R7 – Deploy proof

- Nuke UpdateService only.
- Service Running; health 200; `/usecases/` 200; REST smoke create/link/diagram/coverage; diagram UI reachable post-deploy.
- Gate: deploy + smoke logs.
- Hostile: re-hit live health + UI + REST after deploy.

---

## 8. Acceptance criteria (program done)

All must be true with receipts **and** latest hostile-validator `OverallVerdict=AGREE`:

1. USECASE FR/TR/TEST in store + Project docs; ValidateTraceability green for them.
2. Migrations apply on empty and production-shaped DBs (TEST-006); no SessionLogs drift.
3. Audit emission proven (TEST-007).
4. Domain + REST + MCP + client green with 0 skips in gate filters.
5. FR projection and coverage green; client binds live coverage JSON.
6. First-party UI live after UpdateService, including **built-in diagram view and edit** (operator-in-scope; not Mermaid `<pre>` dump alone).
7. Operator expanded features present and tested (approval, ProductKey, non-Mermaid, shared Realizes validation).
8. Plugin core + agent skills + REPL UseCases allow-list present and tested.
9. Nuke UpdateService succeeds with config preserved; service healthy.
10. Hostile validator AGREE receipt current for the full claim pack.

---

## 9. Non-goals

- Full MCP-PRODUCTS product membership subsystem (hooks only).
- Changing global FR id scheme or TR/TEST mapping tables without a separate approved TR.
- GraphRAG UC ingest unless a TR is written and gated (optional later phase).
- Pixel-perfect multi-theme UI redesign.
- **Not a non-goal:** built-in diagram view/edit UI (that is in scope per operator).

---

## 10. Implementation checklist

Checklist items may only be `[x]` when a hostile-validator receipt with `OverallVerdict=AGREE` covers them. Prior self-checked boxes were invalid and are reset.

- [x] R0 requirements ingested + mapped + ValidateTraceability (HV AGREE `docs/receipts/hostile-validator-20260808T100309Z.md`)
- [x] R1 migration-apply tests + pure migrations multi-provider (HV AGREE same receipt)
- [x] R2 audit emission (HV AGREE same receipt)
- [x] R3 CQRS domain suite (HV AGREE same receipt; 37 UseCase tests)
- [x] R4 REST/MCP/client (HV AGREE same receipt)
- [x] R5 projection/coverage/traceability (HV AGREE same receipt)
- [x] R6 UI/plugins including built-in diagram view + model-driven structure edit (HV AGREE; live `/usecases/` has `#diagramView`, actors/flows/steps/links)
- [x] R7 UpdateService + live smoke with diagram UI proof (HV AGREE; Health Healthy; deploy 2026-08-08T09:58:41Z)
- [x] Hostile validator contract: skill `hostile-validator` + plan section 6.1 (adversarial Grok sub-agent)
- [x] Hostile validator AGREE on full program claims: `docs/receipts/hostile-validator-20260808T100309Z.md` (must re-run on next status report)

---

## 11. Verification receipts (required artifacts)

- Requirements: `{SCRATCH}/usecase-requirements-gate.log`
- Migration apply: `{SCRATCH}/usecase-migration-apply.log`
- Unit UseCase: `{SCRATCH}/usecase-unit-tests.log`
- Audit: `{SCRATCH}/usecase-audit.log`
- REST smoke: `{SCRATCH}/usecase-rest-smoke.log`
- FR projection: `{SCRATCH}/usecase-fr-projection.log`
- UI: `{SCRATCH}/usecase-ui.log`
- Expanded / plugin: `{SCRATCH}/usecase-expanded-scope.log`, `{SCRATCH}/usecase-plugin-core-jest.log`
- Deploy: `{SCRATCH}/usecase-update-service.log`
- **Hostile validator (every step / status):** `docs/receipts/hostile-validator-<utc>.md` + `.json`

---

## 12. Changelog

- v1.0: Initial sketch; failed TR audit
- v2.0: TR-compliant redesign; pure API; OOS list (incorrectly OOS diagram UI)
- v3.0: Post-failure audit plan: requirements authority mandatory; migration-apply + audit tests; operator expansions in scope; Nuke UpdateService only; all prior completion claims reset
- v3.1: Mandatory hostile validator on every step and every status report; diagram view/edit reaffirmed in scope; dishonest R0–R7 checkmarks reset
- v3.2: Hostile validator redefined as adversarial Grok sub-agent only (not implementer PowerShell; not Codex)

---

## 13. Related receipts

- `docs/receipts/usecase-design-tr-audit-20260807T141204Z.md`
- `docs/receipts/requirements-list-summary-20260807T140215Z.md`
- `docs/receipts/usecase-plan-requirements-audit-20260808.md`
- `docs/receipts/usecase-r0-r7-execution-20260808.md` (partial; not completion)
- `docs/receipts/hostile-validator-*.md` (authoritative for status truth)
