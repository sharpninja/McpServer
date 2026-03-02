# MCP Server Release Checklist

## Pre-Release Verification

### Build & Test

- [ ] `dotnet build McpServer.sln -c Release` succeeds with 0 errors, 0 warnings
- [ ] `dotnet run --project tests/McpServer.Support.Mcp.Tests` — all tests pass (target: 236+)
- [ ] Docker build succeeds: `docker build -t mcp-server:latest .`
- [ ] Container health check passes: `curl http://localhost:7147/health`

### Compatibility

- [ ] REST API routes unchanged (compare with `docs/stdio-tool-contract.json` httpEquivalent fields)
- [ ] STDIO tool names and parameters unchanged (compare with `docs/stdio-tool-contract.json`)
- [ ] TODO YAML schema compatible (test with existing `docs/Project/TODO.yaml`)
- [ ] ISSUE-* frontmatter parse/serialize stable
- [ ] Session log schema compatible (test with existing session logs)
- [ ] Multi-tenant workspace resolution tested with `X-Workspace-Path` header
- [ ] Director workspace switching via header verified
- [ ] EF Core global query filter workspace isolation verified

### Configuration

- [ ] `appsettings.json` has all required keys with sensible defaults
- [ ] `C:\ProgramData\McpServer\appsettings.json` is the canonical Windows service config (no `appsettings.Production.json` override)
- [ ] Environment variable overrides work (Mcp__Port, Mcp__RepoRoot, etc.)
- [ ] Feature toggles (Embedding:Enabled, VectorIndex:Enabled) respect settings
- [ ] Per-instance TODO storage backend selection works (YAML and SQLite)

### Documentation

- [ ] README.md is current with all features
- [ ] `docs/MCP-SERVER.md` server documentation up to date (workspaces, diagnostic endpoints, Production deployment)
- [ ] `docs/stdio-tool-contract.json` manifest matches actual tools
- [ ] `docs/Project/` requirements documents reflect current state
- [ ] CHANGELOG or release notes drafted

## Release Steps

1. **Version bump**: Update `.version` file
2. **Final test run**: `dotnet run --project tests/McpServer.Support.Mcp.Tests`
3. **Docker build**: `docker build -t mcp-server:$(cat .version) -t mcp-server:latest .`
4. **Tag release**: `git tag v$(cat .version) && git push origin v$(cat .version)`
5. **CI release**: GitHub Actions `release-main` job creates release automatically on tag push
6. **MSIX package**: CI `windows-msix` job publishes installer artifact

## Post-Release Verification

- [ ] GitHub Release created with correct artifacts
- [ ] Docker image runs and passes health check
- [ ] MSIX installer works on clean Windows machine
- [ ] FunWasHad workspace can connect to released MCP server
- [ ] VS Code extension connects to released MCP server
- [ ] No regression in TODO, session log, or context search operations

## Rollback Plan

If issues are discovered after release:

1. **Revert tag**: `git tag -d v<version> && git push origin :refs/tags/v<version>`
2. **Revert to previous image**: Docker users pull previous tag
3. **Windows service**: `sc.exe stop McpServer.Support.Mcp`, replace binaries, restart
4. **MSIX**: Uninstall current, install previous version

## Monitoring Gates

- Health endpoint returns `Healthy` within 30s of startup
- No unhandled exceptions in first 5 minutes of operation
- TODO CRUD operations succeed
- Context search returns results after ingestion
- Session log submit/query works
