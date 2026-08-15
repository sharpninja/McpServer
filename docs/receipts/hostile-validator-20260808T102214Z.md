# Hostile Validator Receipt (S0 UML canvas plan requirements)

- **TimestampUtc:** 2026-08-08T10:22:14Z
- **ValidatorIdentity:** GrokSubagentHostile
- **Workspace:** F:\GitHub\McpServer
- **Scope:** S0 only (requirements authority for UML canvas plan)
- **Live base:** http://PAYTON-LEGION2:7147
- **Marker:** AGENTS-README-FIRST.yaml (pid 21800, started 2026-08-08T09:58:42Z)
- **Stance:** Adversarial. Default FAIL/UNKNOWN until re-verified on disk / live HTTP / build output.

## Method

1. Defaulted every claim A-I to FAIL/UNKNOWN.
2. Re-verified docs via Read/Grep on disk under `docs/Project/` and `docs/context/`.
3. Re-ran `./build.ps1 ValidateTraceability` and captured Nuke output (findings=0).
4. Live MCP REST after health nonce echo: `GET /mcpserver/requirements/{fr|tr|test}` with `X-Api-Key` + `X-Workspace-Path`.
5. Proved canvas incompleteness via `wwwroot/usecases` assets, product grep (no umlCanvas/palette/drag), diagram service still sequence-only, `GET .../diagram-graph` => 404.
6. Did not implement product changes. Did not trust prior implementer narrative without re-check.

## OverallVerdict

**AGREE**

All of A-H **PASS**. Claim I **PASS** because canvas editor product is proven **not complete** (still form + sequence diagram view UI).

## Claim matrix

| Claim | Verdict | One-line proof |
|------|---------|----------------|
| A | PASS | FR-MCP-USECASE-011..014 headings + AC-011-* .. AC-014-* in Functional-Requirements.md |
| B | PASS | FR-005 sequence vs use-case graph; FR-007 canvas-primary UI text |
| C | PASS | TR-MCP-USECASE-011..016 sections in Technical-Requirements.md |
| D | PASS | TEST-MCP-USECASE-012..017 entries in Testing-Requirements.md |
| E | PASS | TR-per-FR-Mapping + Requirements-Matrix rows for new IDs |
| F | PASS | `docs/context/usecase-diagram-mermaid-schema-v1.md` has schemaVersion 1 + header directive |
| G | PASS | `./build.ps1 ValidateTraceability` Succeeded, findings=0 |
| H | PASS | Live REST lists FR-011..014, TR-011..016, TEST-012..017 (missing=0) |
| I | PASS | Canvas incomplete proven: form/sequence UI only; no umlCanvas; diagram-graph 404 |

## Per-claim evidence

### A - FR-MCP-USECASE-011..014 with AC text - PASS

File: `F:\GitHub\McpServer\docs\Project\Functional-Requirements.md`

Headings present:

- `## FR-MCP-USECASE-011 UML use-case canvas editor` (~line 2036)
- `## FR-MCP-USECASE-012 Persist use-case diagram graph` (~line 2051)
- `## FR-MCP-USECASE-013 Export diagram to Mermaid` (~line 2063)
- `## FR-MCP-USECASE-014 Export diagram to PlantUML` (~line 2073)

AC text present (numbered sub-ACs under each FR):

- AC-011-1 .. AC-011-9 (palette, place, association, include/extend, rename, move, boundary, primary UI, REST-only)
- AC-012-1 .. AC-012-6 (get/put, isolation, soft-delete, invalid, audit)
- AC-013-1 .. AC-013-4 (schema header, golden, deterministic, empty)
- AC-014-1 .. AC-014-3 (PlantUML start/end, golden, deterministic)

Hostile note: claim phrasing "AC-011 / AC-012 / AC-013 / AC-014 text" is satisfied by the AC-011-* family text under those FRs (not a single bare `AC-011` token). No bare `AC-011` without suffix required by claim wording.

### B - FR-005 and FR-007 revised - PASS

Same file:

**FR-MCP-USECASE-005** (~1988):

- Explicit split: sequence generated from flows/steps vs UML use-case from persisted graph
- Mermaid primary; PlantUML also; "Sequence is not a substitute for the use-case canvas editor"
- AC mentions `kind=sequence` sequenceDiagram and `kind=usecase` with `mcp-usecase-diagram-schema:1`

**FR-MCP-USECASE-007** (~2005):

- "Primary diagram UI is the UML use-case drag-and-drop canvas (FR-MCP-USECASE-011)"
- Structure forms secondary; sequence render separate
- AC: canvas primary surface (not forms-only); structure panels secondary; REST-only

### C - TR-MCP-USECASE-011..016 - PASS

File: `F:\GitHub\McpServer\docs\Project\Technical-Requirements.md` (~3123-3182)

- TR-MCP-USECASE-011 Graph storage
- TR-MCP-USECASE-012 Diagram graph CQRS
- TR-MCP-USECASE-013 Diagram graph REST
- TR-MCP-USECASE-014 UML serialization service
- TR-MCP-USECASE-015 Canvas UI hosting
- TR-MCP-USECASE-016 Graph put audit

Each has Covered by / Status / Scope / AC-T11..AC-T16 acceptance criteria.

### D - TEST-MCP-USECASE-012..017 - PASS

File: `F:\GitHub\McpServer\docs\Project\Testing-Requirements.md` (~1196-1224)

- TEST-MCP-USECASE-012 serialization goldens
- TEST-MCP-USECASE-013 graph CQRS/storage
- TEST-MCP-USECASE-014 controller/client
- TEST-MCP-USECASE-015 canvas UI asset/contract
- TEST-MCP-USECASE-016 migration apply
- TEST-MCP-USECASE-017 adversarial Grok hostile validator + live canvas smoke claim pack

### E - Mapping + matrix - PASS

**TR-per-FR-Mapping.md** (~260-269):

- FR-005 maps TR-014 + TEST-012
- FR-007 maps TR-015 + TEST-015 (among others)
- FR-011 -> TR-015 / TEST-015,017
- FR-012 -> TR-011,012,013,016 / TEST-013,014,016
- FR-013 -> TR-013,014 / TEST-012,014
- FR-014 -> TR-013,014 / TEST-012,014

**Requirements-Matrix.md** (~1075-1090):

- FR-011..014 Tracked
- TR-011..016 Tracked
- TEST-012..017 Tracked

### F - Mermaid schema doc - PASS

File: `F:\GitHub\McpServer\docs\context\usecase-diagram-mermaid-schema-v1.md`

- Exists on disk
- Header directive: `%% mcp-usecase-diagram-schema:1`
- Graph JSON example includes `"schemaVersion": 1` and `"kind": "uml-usecase"`
- Node/edge types and deterministic export rules documented
- Status notes stub for S0 / goldens own contract

### G - ValidateTraceability - PASS

Command (this session):

```text
./build.ps1 ValidateTraceability
```

Output excerpt:

```text
05:21:31 [INF] UseCaseFrLinks coverage source: F:\GitHub\McpServer\src\McpServer.Support.Mcp\mcp.db (findings=0)
05:21:31 [INF] Traceability validation passed.
ValidateTraceability     Succeeded      < 1sec
Build succeeded on 8/8/2026 5:21:31 AM.
```

Exit code: 0. findings=0.

### H - MCP store live list - PASS

Health: `GET /health?nonce=hv-8ccdf49d` echoed nonce exactly.

```text
LIST fr usecaseIds=... FR-MCP-USECASE-011,012,013,014 (usecaseCount=14)
LIST tr usecaseIds=... TR-MCP-USECASE-011..016 (usecaseCount=16)
LIST test usecaseIds=... TEST-MCP-USECASE-012..017 (usecaseCount=17)
FOUND count=16
MISSING count=0
```

Required set all present:

- FR-MCP-USECASE-011, 012, 013, 014
- TR-MCP-USECASE-011, 012, 013, 014, 015, 016
- TEST-MCP-USECASE-012, 013, 014, 015, 016, 017

### I - Canvas product NOT complete - PASS (incompleteness proven)

PASS condition for I: prove canvas editor is **not** done; still form/sequence UI.

Evidence:

1. **UI assets** `src/McpServer.Support.Mcp/wwwroot/usecases/`:
   - `index.html` / `app.js` describe form CRUD + structure edit + diagram **view** (mermaid CDN render)
   - Element IDs: actors/flows/steps/links forms, `#diagramView`, `#diagramSource` - **no** `#umlCanvas`, palette, drag hooks
   - Grep `umlCanvas|palette|drag|free canvas` in wwwroot/usecases: **0 matches**

2. **Live UI** `GET /usecases/` status 200, len=9187:
   - `hasCanvasHint=False`, `hasFormish=True`
   - elementIds list is form/diagram-view only

3. **Backend product surface still sequence-first for diagrams**:
   - `MermaidUseCaseDiagramService.cs` still emits `sequenceDiagram` (TR-MCP-USECASE-004)
   - No `diagram-graph` controller route in product grep of Controllers
   - Live `GET /mcpserver/usecases/{nil}/diagram-graph` => **404**

4. **Partial stubs do not equal canvas complete**:
   - `UseCaseDiagramGraphDto` and `IUseCaseUmlSerializationService` exist under Services
   - That is model/service scaffolding only; no canvas editor product, no graph REST route green

Hostile rule applied: if any implementer claimed "canvas editor done", that product claim would FAIL. This S0 pack correctly asserts incompleteness; claim I therefore **PASS**.

## Anti-claims checked

- Did **not** treat plan markdown checkboxes as proof of product completion.
- Did **not** accept docs alone for H (required live REST).
- Did **not** accept prior hostile receipt (100309Z was R0-R7 product slice, different claim pack).
- ValidateTraceability re-run this session; not borrowed from memory.

## Artifacts

- Markdown: `docs/receipts/hostile-validator-20260808T102214Z.md`
- JSON: `docs/receipts/hostile-validator-20260808T102214Z.json`
- Probe script used then deleted: `docs/receipts/_hv-query-req.ps1` (ephemeral)

## Final

**OverallVerdict: AGREE**

Path: `F:\GitHub\McpServer\docs\receipts\hostile-validator-20260808T102214Z.md`
