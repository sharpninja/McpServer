# PR87 P2 Claude HV launcher receipt

Recorded 2026-11-09T11:06:10-06:00 by Codex. This receipt proves launcher preparation only. It does not claim that Claude HV was launched or passed.

## CLI identity and route

- The `claude` wrapper attempted an automatic mise install and failed in the restricted environment because its install location was read-only.
- The already installed binary was selected directly at `/home/sharpninja/.local/share/mise/installs/claude/2.1.318/claude`.
- `--version` returned `2.1.318 (Claude Code)`.
- `auth status` returned loggedIn true, firstParty provider and a Max subscription. Account identifiers are intentionally omitted from this receipt.
- `--help` documents noninteractive `--print`, `--model`, `--effort` values including high, `--input-format text`, `--output-format stream-json`, partial-message and hook-event streaming, and permission controls.
- A read-only string inventory of this installed binary contains the exact model identifier `claude-opus-5-5`. The launcher refuses any other model or any effort other than `high`.
- Binary length: 337814032 bytes.
- Binary SHA-256: `708C77B204DB30086FF6F1ED34577BDA03E1BE74CED80557CBF7B54249EBF0BD`.

## Effort-local files

The files are intentionally under ignored `.mcpServer/pr87-hv/`. They are local execution tooling, not a new product or BDP framework.

- `Invoke-ClaudeHvRound.ps1`, SHA-256 `C54625BA217F6B297927E31056A9001DC8FA286118E0B31DFD375DD6E14A248B`.
- `Start-ClaudeHvRun.ps1`, SHA-256 `3024E7F3D5F9E2D5725C5F74FECABA796DAAECE2391D4547EE4C631D861CD07D`.
- `Test-ClaudeHvLauncher.ps1`, SHA-256 `B84D7E084221904CF93CC285FD2F39FA6C808D85CB0329BDFB90F640137262B3`.
- All three scripts parsed without errors and produced zero PSScriptAnalyzer Warning or Error findings.
- The workspace code-verify hook returned succeeded after each edit.

## Launch contract

- The runner uses `System.Diagnostics.ProcessStartInfo.ArgumentList`, writes the complete prompt to standard input, and streams every stdout line to the response JSONL with AutoFlush.
- The generated invocation includes `--print --model claude-opus-5-5 --effort high --input-format text --output-format stream-json --include-partial-messages --include-hook-events --verbose`.
- The invocation has no timeout, deadline, turn cap, evidence-size cap or `--max-budget-usd`.
- Edit, Write and NotebookEdit tools are disallowed for the reviewer. The prompt and launch gate separately prohibit reviewer builds and tests.
- Request metadata records actual CLI identity, model, effort, binary, reviewed head, code head, task map and hash, manifest and hash, evidence paths, full prompt hash, response path and operator directives.
- The runner writes the complete stream and a terminal done record. An interrupted run remains an interrupted stream and is not converted into a verdict.

## Refusal and success tests

`Test-ClaudeHvLauncher.ps1 -WhatIf` listed 12 disposable scenarios and wrote nothing. The actual test then created isolated local git repositories and pushed branches under `/tmp`, invoked the real gate only with `-WhatIf`, and produced 12 of 12 passing expectations:

- success: gate passed and generated the required Claude command without starting a process or writing launcher output files;
- unresolved FILL token: refused;
- unresolved PENDING token: refused;
- unresolved brace token: refused;
- dirty tree: refused;
- committed but unpushed tree: refused;
- wrong manifest hash: refused;
- missing task map: refused;
- missing TRX host or run identity: refused;
- missing standing rule: refused;
- wrong reviewer model: refused;
- wrong reviewer effort: refused.

The final disposable test root was `/tmp/pr87-claude-launcher-tests-e759c08d64724243b8d80e54f4f337d1`. It is retained rather than deleted. No Claude review session was started.
