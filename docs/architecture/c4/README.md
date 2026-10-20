# C4 model of QBrain.AI (McpServer)

docs/architecture/c4/workspace.dsl is the one definition of every diagram in this repository's documentation (PLAN-C4MODEL-001, ADR 0001). It models the whole codebase: every project under src, the compatibility packages, the Nuke build, the test projects, the agent plugin core, the developer tools, the operations scripts and the FRP relay, plus the EF Core entity model. Documents show a diagram as an image link to docs/architecture/c4/rendered/<viewKey>.svg followed by the line "View: <viewKey>"; no document defines a diagram of its own.

## Files
- workspace.dsl: people, external systems, the QBrain.AI software system with its containers and components, relationships, deployment environments, view-set properties and styles. It includes the fragments below.
- model/entities.dsl: the EF Core entity model, one Entity element per entity (88) with columns as element properties, 126 foreign-key relationships with cardinality, and a "Maps table" relationship from each DbContext component to its entities.
- views/c4.dsl: static C4 views (levels 1 to 3). Each is a neighbourhood view of one hub element and at most three neighbours, so it fits a letter page at 8 to 16 pt (TR-MCP-NUKE-017 AC-5); together they show every element and every relationship at each level. Keys: CTX-<hub>-NN (system context), CTR-<hub>-NN (containers), CMP-<container>-<hub>-NN (components).
- views/entities.dsl: custom views of the entity model, EF-<area>-NN, one DbContext area per group.
- model/mkt.dsl and views/mkt.dsl: the four docs/Marketing diagrams (DocCode MKT), MKT-<diagram>-01 views with -partN tiles; docs/Marketing/diagrams/README.md lists them.
- model/qmd.dsl and views/qmd.dsl: the six imported diagrams of docs/Project/Quad-Model-Transactional-Diffgram-Plan.md (DocCode QMD), keyed by their imported IDs with -partN tiles and -BR-<branch> views.
- Deployment views (in workspace.dsl): DeploymentDocker, DeploymentDockerWorkspace, DeploymentWindowsService, DeploymentWindowsKeyServer, DeploymentWindowsDesktop, DeploymentLinuxSystemd, DeploymentFrpRailway, DeploymentFrpLocal.
- documentation/ and decisions/: the architecture overview and the ADRs of PLAN-C4MODEL-001, attached to the software system with !docs and !adrs.
- evidence.md: file-path evidence and confidence per element, including the full EF Core entity model.
- rendered/: one SVG per view, written by the Nuke target. Do not edit by hand.

## Render and check
Run from the repository root:

    ./build.ps1 ArchitectureDiagrams

The target (TR-MCP-NUKE-017) validates the workspace with structurizr/cli 2025.11.09 pinned by digest, fails on any Structurizr inspection error, renders every view through the Structurizr static export in headless Chromium (light theme, cropped to the drawn content plus 50 units, generated element ids renumbered per render so a render's bytes depend only on its own view), writes only changed files, removes renders of deleted views, and fails on Mermaid fences or .mmd files outside docs/receipts, on image links to unknown views, on views without renders, and on renders outside the letter-page standard. Add --what-if to report without writing.

## Editing
- Add an element once and reuse it in every view that shows it (ADR 0001, D4).
- After adding elements or relationships, every element must appear in at least one view (Structurizr inspection) and each view must still pass the page check; split a view that overflows rather than shrinking text.
- Diagram kinds that are not C4 (activity, entity, state) are custom views of custom elements (ADR 0002).

## Still open
- Plugin repositories (mcpserver-*-plugin) live outside this repository and appear as one external system.
- LLM access: brain slots use CLIs; the OpenAI-compatible HTTPS path was not traced to concrete endpoints.
- Identity: McpIdentityDbContext uses a separate connection string (UseSqlServer in Identity/IdentityServerExtensions.cs); whether it is the same database as McpDbContext is configuration.
- The Windows database, any Kubernetes or cloud hosting of the server, and which plugin and tunnel combinations run in practice are not evidenced in the repository.
- Install scripts that mention McpServer.Support.Mcp may predate the QBrainAi rename; the Dockerfile still uses .NET 9 images (triage-report-1ce108ca70f34191a81a9927a5b3d512).
