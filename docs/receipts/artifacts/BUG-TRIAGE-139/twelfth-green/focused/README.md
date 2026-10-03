# BUG-TRIAGE-139 twelfth focused GREEN evidence

Captured 2026-09-03 from branch `codex/bug-triage-139-remediation` after the signed test-only RED commit `16435544c538a331437ab103ec62070d43fb22a3`.

All build and test outputs were routed outside the source tree through:

- `UseArtifactsOutput=true`
- `ArtifactsPath=C:\Users\kingd\AppData\Local\Temp\BUG-TRIAGE-139-twelfth-20260903T201419Z`
- `MCP_REPOSITORY_ROOT=F:\GitHub\McpServer\.mcpServer\worktrees\bug-triage-139-review`

## Authoritative focused command

```powershell
dotnet test tests/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.csproj -c Release --no-restore --no-build --settings docs/receipts/artifacts/BUG-TRIAGE-139/ninth-green/serial.runsettings --filter "FullyQualifiedName~Twelfth" --logger "trx;LogFileName=focused-twelfth-serial.trx" --results-directory C:\Users\kingd\AppData\Local\Temp\BUG-TRIAGE-139-twelfth-20260903T201419Z\focused-green-serial
```

Result: 20 passed, 0 failed, 0 skipped in 25 seconds; exit code 0.

The 20 tests cover durable identifier round-trip and replay on SQLite, SQL Server LocalDB, and PostgreSQL; scoped SQLite timeout restoration; bounded/cancellable physical identity; streamed and bounded requirements snapshots; real junction-swap read/write containment; repository-root validation; process timeout/kill; review provenance; and byte-level editable-text EOL enforcement.

## Supplemental scheduler evidence

A first non-serialized aggregate invocation emitted 17 passing results and no failures/skips. The three discovered but absent results were rerun explicitly and passed 3/3. Both raw artifacts are retained. The authoritative serialized invocation then emitted all 20 results in one TRX with zero failures and zero skips.

## SHA-256 inventory

- `focused-twelfth-serial-console.txt`: `FDDCD806D34332277DB8057A4D8AE06169EB656862623BEC1A11A7DAD4A34231`
- `focused-twelfth-serial.trx`: `4D05E97F1C1506622F052323FE03E71C6D526EBAEE1694C08D3A69CFAE4FD3AF`
- `focused-twelfth-console.txt`: `98D714E39719C2CB820F8ABC7A0586A55B26591FD353DCB1D0E83E49D7988C2A`
- `focused-twelfth.trx`: `3192BFD34636AFC1259B56EF17E8F7829D7C7A75D5BDA87D331DAF3E9AFEC3AE`
- `focused-twelfth-isolated-three-console.txt`: `4597528522E8B0C182320E39B66789EBBBD08324A9BB9D95BCB4D1832666DDF8`
- `focused-twelfth-isolated-three.trx`: `80DFCB776CCF10349BB591E896E7A3C8DB4122140578248797F731BE0118CFE3`
