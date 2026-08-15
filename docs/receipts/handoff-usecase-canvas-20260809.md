# Detailed handoff: Use Case Extension + UML canvas (2026-08-09)

## Summary for next agent

Workstream: **UML use-case modeling** (domain already largely live) plus **drag-drop UML canvas** (source complete; **Windows service live UI may still be stale** until elevated Nuke `UpdateService`).

Operator required classic UML canvas (palette + free canvas + drag), not form-only sequence regeneration. Hostile validator is an **adversarial Grok sub-agent** (not implementer PowerShell).

## Verified status (do not invent)

| Area | State | Evidence |
|------|--------|----------|
| Unit tests UseCase filter | **58 passed, 0 failed, 0 skipped** (last full run in session) | `dotnet test ... --filter FullyQualifiedName~UseCase` |
| Serialization goldens | Green (TEST-012 family) | `UseCaseUmlSerializationTests` |
| Graph CQRS | Green | `UseCaseDiagramGraphCqrsTests` |
| Canvas UI asset contracts | Green | `UseCaseCanvasUiAssetTests` |
| Requirements FR/TR/TEST 011-017 | In Project docs + MCP store | S0 HV AGREE `hostile-validator-20260808T102214Z.md` |
| ValidateTraceability | Passed findings=0 (after Project appends) | Nuke target |
| Live `/usecases/` after last agent attempt | **Still pre-canvas** (len ~9187, no umlCanvas) | HV `hostile-validator-20260808T105331Z.md` LIVE-DEPLOY FAIL |
| Elevated UpdateService from agent | **Blocked** (gsudo Medium integrity, no credential cache) | `gsudo status` |

## What is in source (repo)

### Backend
- Entities: `UseCaseEntity.DiagramGraphJson`
- Migrations: `*AddUseCaseDiagramGraph` (Sqlite + SqlServer + Postgres)
- CQRS: `GetUseCaseDiagramGraphQuery`, `PutUseCaseDiagramGraphCommand`
- Serialization: `UseCaseUmlSerializationService` (Mermaid schema v1 + PlantUML)
- REST: `GET/PUT .../diagram-graph`; `GET .../diagram?kind=usecase|sequence`
- Client: `UseCaseClient.GetDiagramGraphAsync` / `PutDiagramGraphAsync`; `GetDiagramAsync(..., kind:)`

### UI (wwwroot)
- `src/McpServer.Support.Mcp/wwwroot/usecases/index.html` - palette + `#umlCanvas` primary
- `canvas-editor.js` - SVG place/connect/rename/drag, toGraph/fromGraph
- `app.js` - save/load diagram-graph REST, export kind=usecase

### Docs / plan
- Plan: session `plan.md` (BDPv4 100% AC TDD canvas plan)
- Design: `docs/McpServer-UseCase-Extension-Design-v3.0.md` (program) + canvas ACs in Project FR 011-014
- Schema: `docs/context/usecase-diagram-mermaid-schema-v1.md`
- Spike: `docs/receipts/usecase-canvas-engine-spike-20260808.md` (JointJS preferred; **implemented custom SVG** with same interaction model)

## Hostile validator contract

- Skill: `.grok/skills/hostile-validator/SKILL.md`
- Every status claim needs Grok sub-agent receipt with `OverallVerdict`
- Canvas claims must not pass on form/sequence-only UI

Latest relevant HV:
- S0 requirements AGREE: `docs/receipts/hostile-validator-20260808T102214Z.md`
- Canvas source PASS / live FAIL: `docs/receipts/hostile-validator-20260808T105331Z.md` (**DISAGREE**)

## Immediate next steps for next agent

1. **Operator or elevated shell:** `.\build.ps1 UpdateService --SkipVersionBump true` (admin).
2. Verify live: `umlCanvas`, `canvas-editor.js` 200, graph put/get smoke.
3. Spawn **adversarial Grok** hostile validator with canvas + LIVE claims; require **AGREE**.
4. Optionally polish: JointJS package instead of custom SVG if operator insists on library; Playwright E2E for AC-011-2..7.
5. Do not mark R0-R7 "forms era" complete as canvas complete.

## Auth / marker (rotates on restart)

- Re-read `AGENTS-README-FIRST.yaml` after every service restart for `apiKey`.
- Health + nonce before MCP mutations.
- Plugin: `mcpserver` MCP tools + `repl-invoke.ps1` / `mcp-status.ps1`.

## Do not

- Bypass Nuke UpdateService with manual ProgramData copies.
- Claim LIVE canvas until HV AGREE on live HTML.
- Use PowerShell claim-runner as the hostile validator.
