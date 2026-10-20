## Overview

This Structurizr workspace (docs/architecture/c4/workspace.dsl) is the one definition of every diagram in the QBrain.AI documentation. It models the whole codebase as built, the EF Core data model, and the planned design of the architecture documents (tagged Planned), and it holds every documentation view: C4 system context, container, component and deployment views, dynamic views for sequences, and custom views for activity, entity, state and topic diagrams.

## How the diagrams reach the documents

Each view is rendered to docs/architecture/c4/rendered/<viewKey>.svg by the Nuke target ArchitectureDiagrams (TR-MCP-NUKE-017 under FR-MCP-BUILD-001). A document shows a diagram with an image link to its rendered view followed by a line "View: <viewKey>". No document carries diagram source of its own; the target fails when a Mermaid fence or an .mmd file appears outside docs/receipts.

Run the target from the repository root:

    ./build.ps1 ArchitectureDiagrams
    ./build.ps1 ArchitectureDiagrams --what-if

The first validates the workspace with the pinned Structurizr CLI, renders every view through the Structurizr static export and headless Chromium, and runs the checks. The second reports what would change and writes nothing.

## Evidence

Every element and relationship that describes code as built cites file paths in docs/architecture/c4/evidence.md, with H (read directly) or M (inferred) confidence. Planned elements cite the architecture document and section that define them.

## Decisions

The decisions behind this workspace are recorded as architecture decision records in docs/architecture/c4/decisions.
