# MCP Server Release Checklist

## Pre-Release Verification

### Build & Test

- [ ] `./build.ps1 Compile --configuration Release` succeeds with 0 errors, 0 warnings
- [ ] `./build.ps1 Test` — all tests pass (target: 236+)
- [ ] `./build.ps1 ValidateConfig` — config validation passes
- [ ] `./build.ps1 ValidateTraceability` — requirements coverage passes
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

- [ ] `appsettings.yaml` has all required keys with sensible defaults
- [ ] `C:\ProgramData\McpServer\appsettings.yaml` is the canonical Windows service config (a legacy `appsettings.json` in the install directory is rejected at startup by `WindowsServiceDeploymentGuard` and removed on redeploy by `scripts\Update-McpService.ps1`; no `appsettings.{Environment}.yaml` override)
- [ ] Environment variable overrides work (Mcp__Port, Mcp__RepoRoot, etc.)
- [ ] Feature toggles (Embedding:Enabled, VectorIndex:Enabled) respect settings
- [ ] TODO storage uses the single `database` provider (Provider=database; legacy `sqlite` accepted as an alias, `yaml` rejected) routed through Mcp:Database:Provider, and TODO.yaml is a read-only projection

### Documentation

- [ ] README.md is current with all features
- [ ] `docs/MCP-SERVER.md` server documentation up to date (workspaces, diagnostic endpoints, Production deployment)
- [ ] `docs/stdio-tool-contract.json` manifest matches actual tools
- [ ] `docs/Project/` requirements documents reflect current state
- [ ] CHANGELOG or release notes drafted

## Release Steps

1. **Version bump**: `./build.ps1 BumpVersion` (updates `GitVersion.yml` next-version). Plugin packaging may also use a root `.version` file where applicable; do not treat a stale alpha `.version` as the product line if `GitVersion.yml` is ahead.
2. **Final test run**: `./build.ps1 Test`
3. **Docker build**: `docker build -t mcp-server:$(cat .version) -t mcp-server:latest .`
4. **Tag release**: `git tag v$(cat .version) && git push origin v$(cat .version)`
5. **CI publish**: Azure DevOps `publish-packages` job publishes `McpServer.Client` on `main` when `NuGetApiKey` is configured
6. **MSIX package**: Azure DevOps `windows-msix` job publishes the installer artifact

## Post-Release Verification

- [ ] Azure DevOps pipeline run completed with the expected published artifacts
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
