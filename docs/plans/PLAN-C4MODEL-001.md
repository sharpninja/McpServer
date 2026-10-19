# PLAN-C4MODEL-001: Every documentation diagram comes from one Structurizr C4 definition

Status: awaiting operator approval (written 2026-10-19 on branch docs/c4-structurizr-model-20261017, PR 84).
Process: Byrd Development Process v4. The plan decides its phases, gates and hostile validation points.

## 1. Operator rulings (verbatim, 2026-10-19)

1. PR 84 comment 6269382182: "Mermaid was supposed to be replaced by a single Structurizr C4 model encompassing the whole codebase."
2. Chat: "Every diagram in every doc should come from the same C4 definition."
3. Answers to the batched decision (all four chosen as recommended):
   - Non-C4 diagram kinds: "Custom views". Activity, entity, state and mindmap diagrams are tagged custom elements plus custom views in the same workspace.
   - Code freeze: "Exempt the Nuke target". One Nuke target renders, validates and enforces the rule, with its own TR-MCP-NUKE TR under FR-MCP-BUILD-001.
   - Renderer: "Structurizr static + Chromium". structurizr/cli export -format static, then headless Chromium.
   - Sequencing: "Everything now, re-HV after". Every doc converts in PR 84; the SessionTurn architecture and its plan get a new HV round afterwards.

These rulings supersede the Mermaid-specific parts of the 2026-10-11 deliverables ruling ("a single, complete activity diagram (mermaid) for every unit of work"): every unit of work still gets one complete activity diagram, as a custom view.

## 2. Scope (receipts: git ls-files inventory, 2026-10-19T09:37:54 local)

In scope, 460 diagrams:
- docs/architecture/SessionTurn-Review-AgentContext-Architecture.md: 213 fences (flowchart 135, sequence 58, entity 16, state 4).
- docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md: 107 fences (flowchart 77, sequence 17, entity 9, state 4).
- docs/Stateless-Agent-Context-Management.md: 109 fences (flowchart 68, sequence 33, entity 6, state 2).
- docs/Project/Quad-Model-Transactional-Diffgram-Plan.md: 6 fences (flowchart 5, sequence 1), the six imported diagrams of FR-MCP-124.
- docs/Marketing/diagrams: 4 .mmd files (sequence 1, graph 2, mindmap 1), plus their committed renders (4 svg/png).
- docs/architecture/c4 in PR 84: 12 Mermaid view exports (mermaid/) and 9 Mermaid entity diagrams (data/).

Out of scope, with reasons:
- docs/receipts (8 fences, 3 .mmd, rendered svg trees): historical evidence of past HV rounds; receipts keep their recorded content.
- Product features that emit Mermaid or PlantUML for users (FR-MCP-USECASE-005, -013, -014; MermaidUseCaseDiagramService, UseCaseUmlSerializationService, the use-case web UI): product output formats, not documentation diagrams.
- tools/powershell/McpContext.USER.html: a VS Code export of McpContext.USER.md that inlines the Mermaid library; the markdown source has no diagram.

## 3. Requirements

New (captured before implementation, through workflow.requirements):
- TR-MCP-NUKE-017 "ArchitectureDiagrams: one C4 definition for every documentation diagram", mapped to FR-MCP-BUILD-001, acceptance criteria:
  - AC-1: validates docs/architecture/c4/workspace.dsl with structurizr/cli:2025.11.09 (pinned digest recorded in the target; never :latest) and fails on a non-zero exit or any inspection error.
  - AC-2: renders every view of the workspace to docs/architecture/c4/rendered/<viewKey>.svg through the Structurizr static export and headless Chromium, in the light theme, with the help dialog dismissed, the content fitted, and no render-time stamp, so two runs on the same workspace produce byte-identical files.
  - AC-3: fails when a tracked file outside docs/receipts contains a Mermaid fence or has the .mmd extension.
  - AC-4: fails when a markdown image under docs points into docs/architecture/c4/rendered with a view key the workspace does not define, and when a rendered file has no view (orphan).
  - AC-5: fails when a workspace font size falls outside 11 to 16 px (8 to 12 pt at 96 dpi) or a render exceeds one letter page (1056 x 816 landscape or 816 x 1056 portrait) without being split into views per functional group.
  - AC-6 (amended 2026-10-19, operator answer "Target-level --what-if"): honors the Nuke parameter WhatIf (--what-if, the name PLAN-BUILDWHATIF-001 gives it): reports what it would validate, render and check, runs the read-only checks, and writes nothing; the build.ps1 -WhatIf pass-through stays with TR-MCP-NUKE-005, which is not implemented on this branch.
  - AC-5 is measured on each rendered SVG: the check passes when one scale exists at which every visible text size lies within the band and the scaled viewBox fits 1056 x 816 or 816 x 1056. Amended 2026-10-19 (operator answer "Show it; widen AC-5 to 2:1"): the band is 10.67 to 21.33 px (8 to 16 pt), because hiding the element metadata line is a Structurizr inspection error and with it visible the spread is a fixed 2:1.
  - Structurizr inspection errors are fixed with real content, never suppressed (operator answer "Real docs and ADRs"): the software system carries !docs (architecture overview) and !adrs (one ADR per ruling of this plan).
  - The pre-existing Build.Tests failure (BoundedProcessTests on OfficialPluginCoreSyncTestSupport.cs, triage-report-1b7f4954cf214dea9fbadebe1ecc65da) is fixed in this plan (operator answer "Fix it in this plan"), so the infrastructure gate is 409/409.

Reconciled (applied only after approval; text below is the proposed replacement):
- TR-MCP-TXNDIAGRAMS-001: "Imported diagrams SHALL be preserved as views of docs/architecture/c4/workspace.dsl whose keys are the imported diagram IDs, with source-section references, branch IDs (element and relationship properties), scope annotations, and test mappings. In-scope branches SHALL have tests; future quad-model, Curiosity, AoT, and weight-update branches SHALL remain explicitly deferred." The record's "Covered by" line is kept and gains docs/architecture/c4/workspace.dsl. (Correction made while applying the approved text: the first proposal omitted the existing deferral clause; the clause is kept, nothing else changed.)
- TEST-MCP-165: "Imported diagram preservation tests SHALL validate all six imported diagram views by key in the workspace, one rendered image link per diagram section of the Quad-Model plan, imported source references, and repo annotations."
- FR-MCP-124 keeps its text (it requires preservation of the diagrams, not a format).

## 4. Locked design decisions

D1 One definition. docs/architecture/c4/workspace.dsl is the only diagram source. It may !include fragments under docs/architecture/c4/model/ and docs/architecture/c4/views/; nothing else in the repository defines a diagram.

D2 Whole codebase. Every project under src/ is an element: deployable units are containers; libraries are components of the container that loads them, tagged Library; the SharpNinja.McpServer.* compatibility shims are components tagged Compatibility; build/ (Nuke), tests/ (one element per test project, relationship "tests" to its target), plugins/core, tools/powershell and infra/frp are modeled. Plugin repositories outside this repository are external software systems.

D3 Planned design. Elements and relationships that exist only in the architecture docs are tagged Planned. As-built views exclude the tag; architecture views include it.

D4 Identity. One element per real thing across all diagrams. A participant or node that names an existing element reuses it; new things get one element. Mermaid node ids never become model identifiers.

D5 Mapping by Mermaid kind.
- Structure flowcharts and graphs: C4 static views (context, container, component) or custom views when the nodes are not C4 elements.
- Activity flowcharts (unit-of-work diagrams): custom view of elements tagged Activity plus one of Start, Action, Decision (Diamond shape), Error, End; edges are relationships whose description is the branch condition. Every branch and error path of the source survives.
- Sequence diagrams: dynamic views. Each alt, opt or par branch becomes its own dynamic view keyed <diagramKey>.<branch>; each loop body becomes a nested dynamic view keyed <diagramKey>.loop<N> placed directly after its parent in the doc (the 2026-10-12 loop rule).
- Entity diagrams: custom view of elements tagged Entity, one element per entity, attributes and keys in element properties, cardinality in the relationship description; an entity drawn in two diagrams is one element.
- State diagrams: custom view of elements tagged State; transitions are relationships named by their trigger.
- Mindmap: custom view of elements tagged Topic.
- The EF Core entity model (data/ef-entity-model.md and the 9 Mermaid ER files) becomes Entity elements per DbContext area; its evidence paths move into evidence.md and the Mermaid files are deleted.

D6 View keys. Imported and named diagrams keep their IDs as keys (AD-TXN-001, SD-DIFFGRAM-001, and so on). Other views use <DocCode>-<Section>-<NN> (for example STR-07-03), stable across edits.

D7 Docs. Each Mermaid fence is replaced in place by an image link to docs/architecture/c4/rendered/<viewKey>.svg with the view title as alt text, followed by the line "View: <viewKey>". Prose around diagrams is unchanged.

D8 Rendering. The Nuke target copies the workspace sources to a temporary directory (nothing is written under docs during export), exports the static site there, starts Chromium headless with a DevTools port, emulates prefers-color-scheme light, and per view calls structurizr.diagram.setDarkMode(false), structurizr.diagram.changeView(key, callback) and structurizr.diagram.exportCurrentDiagramToSVG(false), saving the returned SVG. The workspace sets the view-set property "structurizr.metadata" "false". Spike receipts (2026-10-19, scratch probe workspace): screenshots of the viewer selected the right view but showed the help dialog, the dark theme and a render-time stamp (09:53:17); the SVG export has a white canvas, a viewBox cropped to the content and no dialog (10:05:34); with metadata on, the export still carried a hidden date line and two runs differed (10:06:55); with "structurizr.metadata" "false", two runs in separate Chromium processes were byte-identical for all four probe views (10:07:50). Default element text sizes are 16.8, 24, 33.6 and 36 px, a spread above the 1.5 ratio AC-5 allows, so the workspace styles set font sizes explicitly.

D11 AC-3 scope. A Mermaid fence is a markdown construct: the fence scan reads tracked .md and .markdown files; the .mmd check covers every tracked file; both skip docs/receipts. Source files that carry fence text inside string literals (for example Build.Tests fixtures) are not documents and are not fence-scanned.

D10 Risk recorded: the exported index.html loads themes from https://static.structurizr.com/themes/ before it renders; the target must render without network themes (no theme references in the workspace) and fail with a clear message if the viewer never becomes ready.

D9 Migration tooling (a converter from Mermaid to DSL drafts, alias maps) lives outside the repository under ~/.cache/c4-migration and is never a second definition. Every converted view is checked by a person-equivalent review against its source fence before the fence is removed.

## 5. Phases and gates

P0 Records. Create TR-MCP-NUKE-017 and its mapping; after approval, update TR-MCP-TXNDIAGRAMS-001 and TEST-MCP-165; MCP TODO PLAN-C4MODEL-001 points at this plan.

P1 Tests first (BDPv4 5.1). Build.Tests cases for the target's pure parts (Mermaid scanner, view-link and orphan checker, font and page-size checker, render-plan builder, -WhatIf plan) against fixture trees under tests/Build.Tests/Fixtures/c4, with negative controls (a fixture with a stray fence, an .mmd file, a dangling view key, an orphan render, an oversized font) captured inside passing tests. TurnTransactionPlanArtifactTests rewritten to assert the six view keys and links (TEST-MCP-165), proven first against a fixture copy of the converted plan section. Gate G1: green mock gate, every negative control rejected, 0 failed, 0 skipped.

P2 Implementation. build/Build.ArchitectureDiagrams.cs plus helpers; bind the same tests to the real helpers. Gate G2: Build.Tests green; ./build.ps1 Compile green.

P3 Model. Extend workspace.dsl per D2 to D5 for the as-built codebase and the EF entity model; delete mermaid/ and data/*.mmd. Gate G3: ./build.ps1 ArchitectureDiagrams validates and renders; AC-3 still red only for the docs not yet converted (recorded, not hidden).

P4 Conversion, one doc per agent in parallel (Quad-Model plan, Marketing, Stateless, SessionTurn architecture, Amendment 1), each agent following the standing rules and logging its own session-log turns. Gate G4 per doc: no fence left, every view rendered, review record per view.

P5 Gates. ./build.ps1 ArchitectureDiagrams green (AC-1 to AC-6); Build.Tests green; ./build.ps1 Test green including TurnTransactionPlanArtifactTests; ./build.ps1 ValidateTraceability green; every render inspected as an image against the page standard.

P6 Hostile validation of this plan's result by the reviewer the operator names (not a Claude-family agent), 98/98, request and response jsonl under docs/receipts/hv, full verdict in the session log. Then the new HV round of the SessionTurn architecture and plan (operator ruling), with the converted diagrams.

P7 Delivery. Signed commits on docs/c4-structurizr-model-20261017, fast-forward pushes, PR 84 description updated, signed reply on comment 6269382182.

## 6. Open items for approval

- Approve the plan and the requirement reconciliation in section 3.
- Code freeze: P1 rewrites tests/QBrainAi.Support.Mcp.Tests/Documentation/TurnTransactionPlanArtifactTests.cs, product test code outside the Nuke-target exemption.
- Name the HV reviewer (agent and model) for P6.
