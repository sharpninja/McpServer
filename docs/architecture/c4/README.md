# C4 model of QBrain.AI (McpServer) reverse engineered from origin/develop

Commit: 36783b79d061303bfb3e17abe685640a953491af (fetched 2026-10-17). Generated on LAB-OMARCHY by reverse engineering the code; this directory is documentation only and changes no code. Re-run the validate and export commands after editing workspace.dsl, from this directory.

## Files
- workspace.dsl: Structurizr DSL (12 views: SystemContext, DataLayerComponents, Containers, ServerComponents, ReplComponents, QBAgentComponents, KeyServerComponents, plus five deployment views DockerCompose, WindowsService, LinuxSystemd, FrpRelay, FrpRelayLocal).
- data/: EF Core entity model. ef-entity-model.md (per DbContext: entities, keys, relationships, evidence paths, providers, migrations), ef-er-*.mmd (Mermaid ER diagrams, one per DbContext, McpDbContext split into 6 areas), ef-er-diagram.mmd (index).
- evidence.md: file-path evidence and confidence per element.
- mermaid/: Mermaid export of each view (structurizr-*.mmd).

## Validate and export
Use the pinned CLI image, because structurizr/cli:latest is an end-of-life stub that prints a banner and validates nothing:

    docker run --rm -v "$PWD:/usr/local/structurizr" structurizr/cli:2025.11.09 validate -workspace workspace.dsl
    docker run --rm --user 1000:1000 -v "$PWD:/usr/local/structurizr" structurizr/cli:2025.11.09 export -workspace workspace.dsl -format mermaid -output mermaid

Result after the DI trace and deployment steps: validate exit 0, export wrote 12 .mmd files (with the DataLayerComponents view). Structurizr has moved to the consolidated structurizr/structurizr tooling; not tried.

## Resolved by the DI trace (evidence.md, Step A and Step B)
1. Transport sharing: RESOLVED. /mcp-transport (HTTP) and McpStdio both call WithToolsFromAssembly(typeof(FwhMcpTools).Assembly), so the tool set is identical. STDIO is the same executable started with --transport stdio (Program.cs:50-52), not a separate container. Modeled as the MCP STDIO Host component.
2. Key Server and Subscriber modes: RESOLVED for the default. The server and REPL both call AddInProcessTransactionSecurity, which embeds InMemoryKeyServerService and InMemorySubscriberCommitService in process. The standalone Key Server and Subscriber hosts are optional. The server reaches a standalone Subscriber only when TurnTransactionOptions.PubSubTransport is Http (default Direct; ExternalBroker also exists). Which mode any given installation uses is configuration and is still unknown.
3. Key Server database: RESOLVED. It has none. It is in memory (InMemoryKeyServerService). SQLite is used only for the optional pub/sub broker store when PubSubDatabasePath is set.
4. Desktop Launcher caller: PARTLY. DesktopProcessSpawner and DesktopLaunchService are in the server (H) but the exact command line to the launcher exe was not traced (M).
7. Deployment views: RESOLVED. Five deployment views were added from repo files.
9 and 10. Component links: RESOLVED to constructor-dependency level (about 120 traced relationships, mostly H). Endpoint-to-handler wiring inside controllers was not traced beyond constructor parameters.

## Still open
5. Plugin repos (mcpserver-*-plugin) live outside this repo; only plugins/core is modeled.
6. LLM access: brain slots use CLIs; the OpenAI-compatible HTTPS path was not traced to concrete endpoints.
8. Names: the repo is QBrainAi.* (renamed from McpServer); compatibility shims keep SharpNinja.McpServer.* names and are not modeled.
11. Identity: McpIdentityDbContext uses a separate connection string (UseSqlServer in Identity/IdentityServerExtensions.cs). Whether it is the same database as McpDbContext in practice is configuration.
12. The Windows database, any Kubernetes or cloud hosting of the server, and which plugin and tunnel combinations run in practice are not evidenced in the repo.
13. Install scripts that mention McpServer.Support.Mcp may predate the QBrainAi rename.
