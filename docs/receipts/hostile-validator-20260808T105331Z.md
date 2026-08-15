# Hostile Validator Receipt

- **Agent:** GrokSubagentHostile
- **UTC:** 20260808T105331Z
- **Workspace:** F:\GitHub\McpServer
- **Default posture:** FAIL; require machine evidence
- **OverallVerdict:** DISAGREE

## Summary

Source and unit-test claims **A-E PASS**. Live deploy claim **F FAILS** as **LIVE-DEPLOY** (served `/usecases/` is pre-canvas content: no `umlCanvas`, no `canvas-editor.js`). Engine claim **G** is documented: custom SVG, not JointJS library load. Per instructions, overall is **DISAGREE** when live is missing even if source/tests pass.

## Claim A: Source canvas surface + editor APIs

**Verdict: PASS**

Evidence:

- `src/McpServer.Support.Mcp/wwwroot/usecases/index.html`
  - CSS `#umlCanvas` (line 134)
  - Palette tools `palette-actor` through `palette-extend` (lines 226-231)
  - `<svg id="umlCanvas" ...>` (line 234)
  - `<script src="canvas-editor.js"></script>` (line 317)
  - SRC length 11005 bytes; `id="umlCanvas"` true; `palette-extend` true
- `src/McpServer.Support.Mcp/wwwroot/usecases/canvas-editor.js`
  - `placeNode` (line 138)
  - `startConnect` (line 166)
  - `toGraph` (line 281)
  - `fromGraph` (line 313)
  - Public return API includes those methods (lines 363-375)

## Claim B: app.js diagram-graph client wiring

**Verdict: PASS**

Evidence in `src/McpServer.Support.Mcp/wwwroot/usecases/app.js`:

- `loadDiagramGraph` GET `/mcpserver/usecases/{id}/diagram-graph` then `ed.fromGraph(graph)` (lines 324-329)
- `saveDiagramGraph` builds `ed.toGraph()` and PUT `/mcpserver/usecases/{id}/diagram-graph` (lines 332-339)
- Buttons wired: `btnSaveGraph` / `btnLoadGraph` (lines 400-401)
- Exported on `window.UseCaseUi` (lines 413-414)

## Claim C: UseCaseCanvasUiAssetTests green (re-run)

**Verdict: PASS**

Commands re-run:

1. `dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~UseCase" --no-restore`
   - **Passed: 58, Failed: 0, Skipped: 0**, EXIT=0
2. `dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~UseCaseCanvasUiAssetTests" --no-build -v n`
   - All 6 facts passed (palette, umlCanvas element, interaction APIs, graph serialize, diagram-graph REST, script reference)
   - **Total tests: 6, Passed: 6**, EXIT=0

Test file: `tests/McpServer.Support.Mcp.Tests/Web/UseCaseCanvasUiAssetTests.cs`

## Claim D: Graph CQRS + UML serialization green (re-run)

**Verdict: PASS**

Command:

`dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~UseCaseDiagramGraphCqrsTests|FullyQualifiedName~UseCaseUmlSerializationTests" --no-build -v n`

- **Total tests: 15, Failed: 0, Skipped: 0**, EXIT=0
- Includes put/get round-trip, empty schema, validation failures, soft-delete, audit log, Mermaid/PlantUML golden fixtures

Earlier broader filter `UseCaseDiagramGraphCqrs|UseCaseUmlSerialization` also: Passed 15, Failed 0, Skipped 0, EXIT=0

## Claim E: REST controller diagram-graph routes in source

**Verdict: PASS**

`src/McpServer.Support.Mcp/Controllers/UseCasesController.cs`:

- Class route: `[Route("mcpserver/usecases")]` (line 18)
- `[HttpGet("{id:long}/diagram-graph")]` (line 389) -> `GetDiagramGraphAsync` / `GetUseCaseDiagramGraphQuery`
- `[HttpPut("{id:long}/diagram-graph")]` (line 406) -> `PutDiagramGraphAsync` / `PutUseCaseDiagramGraphCommand`

Client mirror also present: `src/McpServer.Client/UseCaseClient.cs` diagram-graph GET/PUT paths (not required for E, corroborating).

## Claim F: LIVE deploy after UpdateService has umlCanvas on /usecases/

**Verdict: FAIL (LIVE-DEPLOY)**

Not scored as source failure. Live process is healthy and serves an **older** Use Case Manager page without the canvas.

Probes (UTC near 20260808T1052Z):

| URL | HTTP | LEN | HAS_umlCanvas |
|-----|------|-----|---------------|
| http://localhost:7147/usecases/ | 200 | 9187 | **False** |
| http://localhost:7147/usecases/index.html | 200 | 9187 | **False** |
| http://PAYTON-LEGION2:7147/usecases/ | 200 | 9187 | **False** |
| http://PAYTON-LEGION2:7147/usecases/index.html | 200 | 9187 | **False** |

Additional live markers:

- `HAS_palette-actor=False`, `HAS_canvas-editor=False`, `HAS_diagram-graph=False`, `HAS_mermaid=True`
- Live page title/content is pre-canvas: `<h2>Use cases</h2>`, sequence diagram section, only `<script src="app.js"></script>` (no canvas-editor.js)
- `GET http://localhost:7147/usecases/canvas-editor.js` -> **404**
- Live `app.js` STATUS 200 LEN=11132; `HAS_saveDiagramGraph=False`, `HAS_diagram-graph=False`
- `/health` local and remote **200** Healthy; marker `version` style health shows server up (pid in marker 21800, started ~2026-08-08T09:58:42Z)
- Source index length **11005** vs live **9187** (content mismatch)

Conclusion: server is running but **content root / deployed wwwroot was not refreshed** with the canvas assets after source UpdateService work. Source PASS does not equal LIVE deploy PASS.

## Claim G: Engine (JointJS vs custom SVG)

**Verdict: DOCUMENT (no fail on custom SVG alone)**

- **Runtime engine:** **custom SVG**, not JointJS library.
  - `canvas-editor.js` uses `document.createElementNS` with SVG namespace (line 35)
  - Comment only: "Interaction model inspired by classic UML tools / JointJS demos" (line 2)
  - No joint.js / JointJS script tags in `wwwroot/usecases/`
  - Repo search for joint/JointJS hits only that comment and spike doc preference
- Spike preference recorded in `docs/receipts/usecase-canvas-engine-spike-20260808.md` (JointJS preferred for S5); custom SVG is acceptable per hostile rule when UX contracts A/B are met.
- UX contracts A/B **are** met in **source** (palette, free canvas, place/connect, toGraph/fromGraph, REST save/load wiring).

## Per-claim matrix

| Claim | Scope | Verdict |
|-------|--------|---------|
| A | Source | PASS |
| B | Source | PASS |
| C | Tests (re-run) | PASS |
| D | Tests (re-run) | PASS |
| E | Source REST | PASS |
| F | LIVE deploy | **FAIL LIVE-DEPLOY** |
| G | Engine note | Custom SVG (acceptable); JointJS not loaded |

## OverallVerdict

**DISAGREE**

Reason: F live deploy missing `umlCanvas` on `/usecases/` after claimed UpdateService deploy. A-E source and tests **PASS** and are listed as such. Fix path: redeploy/restart so process content root serves current `wwwroot/usecases/{index.html,app.js,canvas-editor.js}` (verify live GET returns `umlCanvas` and 200 for `canvas-editor.js`).

## Artifacts

- Markdown: `docs/receipts/hostile-validator-20260808T105331Z.md`
- JSON: `docs/receipts/hostile-validator-20260808T105331Z.json`
