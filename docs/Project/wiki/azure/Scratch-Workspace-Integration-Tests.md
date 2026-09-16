# Scratch Workspace Integration Tests

Use this pattern when an integration test must exercise a real MCP Server process
against an isolated workspace, especially when the test must prove behavior that
depends on the generated `AGENTS-README-FIRST.yaml` marker file.

## Required Shape

The test must build a complete scratch environment on every run:

1. Create a unique scratch root under the test temp directory.
2. Create a workspace directory under that root, for example
   `<scratchRoot>/workspace`.
3. Create a data directory under that root, for example `<scratchRoot>/data`.
4. Create the minimal workspace file layout required by the services under test:
   `docs/Project`, `docs/sessions`, `docs/external`, and `templates`.
5. Seed `docs/Project/TODO.yaml` plus the requirements documents needed by the
   test: `Functional-Requirements.md`, `Technical-Requirements.md`,
   `Testing-Requirements.md`, `TR-per-FR-Mapping.md`, and
   `Requirements-Matrix.md`.
6. Copy or generate the prompt template file at
   `templates/prompt-templates.yaml`. Prefer copying the repository template
   when the marker content matters.
7. Delete the scratch root in test cleanup. If the server or REPL process is
   still running, stop it first and fall back to killing the process tree only as
   cleanup.

Do not reuse the developer's live workspace marker, data directory, SQLite file,
port, or cached plugin state.

## SQLite Workspace Seeding

The workspace must be staged in SQLite before the MCP Server process starts.
The startup marker writer reads configured workspaces from server state and then
writes a marker for each enabled workspace.

Recommended setup:

1. Allocate `<scratchRoot>/data/mcp.db`.
2. Build `DbContextOptions<McpDbContext>` with SQLite and the
   `McpServer.Storage.SqliteMigrations` migrations assembly.
3. Call `Database.MigrateAsync()` before inserting rows.
4. Insert a `WorkspaceEntity` row with:
   - `WorkspaceId`: normalized absolute scratch workspace path.
   - `WorkspacePath`: the same normalized absolute path.
   - `Name`: a readable test name.
   - `TodoPath`: `docs/Project/TODO.yaml`.
   - `DataDirectory`: the scratch data directory.
   - `IsPrimary`: `true` for the one-workspace harness.
   - `IsEnabled`: `true`.
   - `CurrentRequirementLayerKey`: usually `layer-1`.
   - `DateTimeCreated` and `DateTimeModified`: current UTC time.

The test should fail if the marker appears without this pre-staged workspace.
Do not patch production startup marker generation to make the test pass.

## Server Startup

Start the real `McpServer.Support.Mcp` process against the scratch root.

1. Allocate a random high port. Check that the port is free immediately before
   launch, but still treat binding as race-prone and fail with server diagnostics
   if startup cannot bind.
2. Write scratch-local configuration or set environment variables so the server
   uses:
   - the scratch SQLite database,
   - `Mcp:Database:Provider = sqlite`,
   - `Mcp:Database:Sqlite:DataSource = <scratchRoot>/data/mcp.db`,
   - `Mcp:DatabaseMigrationsAssembly = McpServer.Storage.SqliteMigrations`,
   - `Mcp:RepoRoot = <scratchWorkspace>`,
   - `Mcp:TodoStorage:Provider = database`,
   - requirements paths under the scratch workspace,
   - `Mcp:TemplateStorage:FilePath` under the scratch workspace,
   - the random high port through `PORT` or `Mcp:Port`.
3. Set the process working directory to the scratch root so relative config,
   logs, and fallback files stay isolated.
4. Capture stdout and stderr for assertion failure diagnostics.
5. After the marker exists, health-check the marker's `baseUrl` rather than an
   assumed localhost URL.

## Marker Gate

REPL or client calls must not begin until the server-generated
`AGENTS-README-FIRST.yaml` exists in the scratch workspace root.

Use `FileSystemWatcher`:

1. Watch the scratch workspace root.
2. Set the filter to `AGENTS-README-FIRST.yaml`.
3. Listen for `Created`, `Changed`, and `Renamed`.
4. Poll defensively in the same loop because the file can appear between watcher
   setup and event subscription.
5. Use a reasonable timeout, usually one minute.
6. If the server exits before the marker appears, fail with captured stdout and
   stderr.
7. Parse the generated marker and assert it contains the expected port, a
   non-empty `apiKey`, a usable `baseUrl`, and the scratch workspace path when
   that field is present.

This gate proves the same startup contract future agents rely on: MCP Server
writes the marker for each enabled configured workspace on startup.

## REPL and Client Calls

After marker gating:

1. Read endpoint and auth data from the generated marker. Do not use cached
   bearer state, the live developer marker, hard-coded port `7147`, or a guessed
   localhost URL.
2. For REPL tests, launch `McpServer.Repl.Host` with:
   - `--agent-stdio`,
   - `--workspace-path <scratchWorkspace>`,
   - `--marker-file <scratchWorkspace>/AGENTS-README-FIRST.yaml`.
3. For typed client tests, construct options from the marker's `baseUrl`,
   `apiKey`, and workspace path.
4. Keep request/response diagnostics. When testing REPL envelopes, assert the
   final `type: result` or `type: error` document for the specific `requestId`.

## xUnit Sequencing

Prefer one `[Fact]` for workflows where mutations must happen in a strict
sequence. That keeps the setup, mutation chain, and assertions in one ordered
method.

If multiple tests need the same mutable prepared server state, separate setup
from the tests and let xUnit call it first:

1. Put scratch workspace creation, SQLite migration, `WorkspaceEntity` seeding,
   server startup, marker waiting, and REPL/client initialization in an
   `IAsyncLifetime.InitializeAsync` fixture or collection fixture.
2. Put cleanup in `IAsyncLifetime.DisposeAsync`.
3. Apply `[Collection]` so tests that share mutable state do not run in parallel
   with other tests using that fixture.
4. Avoid relying on default test method order. xUnit does not guarantee method ordering.
5. If a true multi-`Fact` mutation sequence is unavoidable, add an explicit
   xUnit test case orderer and make the setup test/order run first. Prefer the
   fixture setup pattern unless the ordering itself is part of the behavior being
   tested.

The setup fixture is the authoritative "called first by xUnit" hook. A setup
method that only happens to sort first by name is not sufficient.

## Failure Rules

Treat these as test failures:

- The server starts without using the scratch SQLite database.
- REPL or client calls run before the generated marker appears.
- The test reads auth or endpoints from cached state instead of the marker.
- The marker is manually written by the test instead of generated by the server.
- A multi-test mutation sequence depends on xUnit's default method order.
- Cleanup leaves a running server or REPL process.
