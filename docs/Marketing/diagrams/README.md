# Marketing Diagrams

The Marketing diagrams are views of the one C4 workspace, `docs/architecture/c4/workspace.dsl` (model fragment `model/mkt.dsl`, views `views/mkt.dsl`). The Nuke target ArchitectureDiagrams renders each view to `docs/architecture/c4/rendered/<viewKey>.svg`; use those SVG files in the Canva website and presentation. The Mermaid sources and PNG renders that used to live in this folder were removed when the diagrams became workspace views (PLAN-C4MODEL-001 P4); the sources remain in the repository history at commit 92e2c6c4.

## Views

Architecture: agents, transports, QBrainAi components and workspace routing, in 7 parts.

- [MKT-ARCHITECTURE-01](../../architecture/c4/rendered/MKT-ARCHITECTURE-01.svg): AI agents and the transport layer
- [MKT-ARCHITECTURE-01-part2](../../architecture/c4/rendered/MKT-ARCHITECTURE-01-part2.svg): transports to tools and controllers
- [MKT-ARCHITECTURE-01-part3](../../architecture/c4/rendered/MKT-ARCHITECTURE-01-part3.svg): multi-workspace routing
- [MKT-ARCHITECTURE-01-part4](../../architecture/c4/rendered/MKT-ARCHITECTURE-01-part4.svg): context search and TODO management
- [MKT-ARCHITECTURE-01-part5](../../architecture/c4/rendered/MKT-ARCHITECTURE-01-part5.svg): session logging and requirements tracking
- [MKT-ARCHITECTURE-01-part6](../../architecture/c4/rendered/MKT-ARCHITECTURE-01-part6.svg): GitHub issue sync
- [MKT-ARCHITECTURE-01-part7](../../architecture/c4/rendered/MKT-ARCHITECTURE-01-part7.svg): GraphRAG and prompt templates

Features: the feature map, the feature areas first and then one area per part, in 6 parts.

- [MKT-FEATURES-01](../../architecture/c4/rendered/MKT-FEATURES-01.svg): QBrainAi feature areas
- [MKT-FEATURES-01-part2](../../architecture/c4/rendered/MKT-FEATURES-01-part2.svg): Context and Search
- [MKT-FEATURES-01-part3](../../architecture/c4/rendered/MKT-FEATURES-01-part3.svg): Task Management
- [MKT-FEATURES-01-part4](../../architecture/c4/rendered/MKT-FEATURES-01-part4.svg): Agent Coordination
- [MKT-FEATURES-01-part5](../../architecture/c4/rendered/MKT-FEATURES-01-part5.svg): Requirements
- [MKT-FEATURES-01-part6](../../architecture/c4/rendered/MKT-FEATURES-01-part6.svg): Infrastructure

UI tooling: the UI surfaces and their connection to QBrainAi, in 2 parts.

- [MKT-UITOOLING-01](../../architecture/c4/rendered/MKT-UITOOLING-01.svg): web, terminal and command-line clients
- [MKT-UITOOLING-01-part2](../../architecture/c4/rendered/MKT-UITOOLING-01-part2.svg): client library and MCP agents

Agent workflow: the sequence from session start through context, TODO, session log and GitHub sync, in 8 parts.

- [MKT-AGENTWORKFLOW-01](../../architecture/c4/rendered/MKT-AGENTWORKFLOW-01.svg): Session Start
- [MKT-AGENTWORKFLOW-01-part2](../../architecture/c4/rendered/MKT-AGENTWORKFLOW-01-part2.svg): Context Retrieval, full-text half
- [MKT-AGENTWORKFLOW-01-part3](../../architecture/c4/rendered/MKT-AGENTWORKFLOW-01-part3.svg): Context Retrieval, vector half and reply
- [MKT-AGENTWORKFLOW-01-part4](../../architecture/c4/rendered/MKT-AGENTWORKFLOW-01-part4.svg): Task Management, backlog query
- [MKT-AGENTWORKFLOW-01-part5](../../architecture/c4/rendered/MKT-AGENTWORKFLOW-01-part5.svg): Task Management, update
- [MKT-AGENTWORKFLOW-01-part6](../../architecture/c4/rendered/MKT-AGENTWORKFLOW-01-part6.svg): Task Management, projection and reply
- [MKT-AGENTWORKFLOW-01-part7](../../architecture/c4/rendered/MKT-AGENTWORKFLOW-01-part7.svg): Session Logging
- [MKT-AGENTWORKFLOW-01-part8](../../architecture/c4/rendered/MKT-AGENTWORKFLOW-01-part8.svg): GitHub Sync

## Regenerating

Edit the workspace, never the SVG files, then run from the repository root:

```powershell
./build.ps1 ArchitectureDiagrams
```

The target and its checks are described in `docs/architecture/c4/README.md`.
