---
name: bash-usage
description: Use when a QBAgent (Quad-Brain) tool call needs a shell on this Windows host: it explains that run_powershell (pwsh) is the primary shell, when the optional run_bash (Git Bash on PATH) tool is available versus unavailable (available=false), and how to fall back without inventing APIs.
license: MIT
---

# Bash Usage on the McpServer Windows Host

This skill governs shell selection for the optional QBAgent (`McpServer.QBAgent`, Quad-Brain)
`run_bash` and `run_powershell` tools. The host OS is Windows 11. PowerShell 7+ (`pwsh.exe`)
is the primary and always-present shell. Git Bash is optional and only usable when it is
resolvable on `PATH`.

## When to Use

- A QBAgent tool loop needs to run a shell command and you must choose between
  `run_powershell` and `run_bash`.
- A `run_bash` call returned `available: false` and you need the correct fallback.
- You are porting a POSIX shell snippet (the build doc examples use `./build.ps1 ...`
  but show bash fences) and need to decide which tool actually executes it.

## When Not to Use

- Pure file read/write/search work. Use the dedicated file tools, not a shell.
- Session log, TODO, requirements, import/export, or traceability operations. Those must
  go through the `mcpserver-claude-code-plugin` wrapper (`workflow.*` / `client.*` methods),
  not through `run_bash` or `run_powershell`. Never `cat`, `grep`, or `sed` the
  `docs/Project/TODO.yaml` or session-log files from a shell.
- Long-lived servers or builds you intend to background. Prefer the project's documented
  entry points (`./build.ps1 Compile`, `./build.ps1 Test`, `./build.ps1 StartServer`).

## Inputs

- The shell command string you intend to run.
- The QBAgent tool result fields, specifically the `available` boolean returned by
  `run_bash`. When `available` is `false`, Git Bash was not found on `PATH` and no command
  was executed.
- Whether the command is POSIX-specific (uses `/dev/null`, `&&` chains over POSIX builtins,
  `grep`/`sed`/`awk`, single-quoted here-docs) or is already PowerShell-native.

## Critical Rules

1. `run_powershell` (pwsh) is the primary shell on this host and is always available.
   Default to it. The repo standard is `pwsh.exe` (PowerShell 7+); never target
   `powershell.exe` (Windows PowerShell 5.1).
2. `run_bash` is OPTIONAL. It only works when Git Bash is on `PATH`. If the tool returns
   `available: false`, treat bash as unavailable for this run and do not retry the same
   call expecting a different result.
3. Do not invent shell tool names, flags, or fields. Only `run_powershell` and `run_bash`
   exist for QBAgent shell execution, and the only availability signal is the `available`
   field on the `run_bash` result.
4. When `run_bash` is unavailable, translate the command to PowerShell and run it through
   `run_powershell`. Do not assume a bash path or call `bash.exe` directly.
5. Use absolute paths in commands. The agent working directory is not guaranteed to persist
   between tool calls.
6. PowerShell syntax differs from POSIX: use `$null` not `/dev/null`, `$env:VAR` not `$VAR`,
   backtick for line continuation, and `pwsh`-native cmdlets instead of `grep`/`sed`/`cat`.

## Workflow

1. Classify the command. If it is already PowerShell-native or platform-neutral, go to
   step 4 and use `run_powershell`.
2. If the command is POSIX-specific and you prefer bash, call `run_bash`.
3. Inspect the `run_bash` result:
   - If `available: false`, Git Bash is not on `PATH`. Translate the command to PowerShell
     and continue at step 4.
   - If `available: true`, use the returned stdout/stderr/exit code as the result.
4. Run the (translated) command with `run_powershell` using `pwsh` syntax and absolute
   paths.
5. For build, test, or server actions, prefer the documented `./build.ps1` targets
   (`Compile`, `Test`, `ValidateConfig`, `ValidateTraceability`, `StartServer`) over
   ad-hoc shell, since those are the supported entry points.
6. Report the result. If you fell back from bash to PowerShell, state that bash was
   unavailable (`available: false`) so the reason is traceable in the session log turn.

## Validation Checklist

- Did you confirm the shell choice matches host reality (pwsh primary, bash optional)?
- If you called `run_bash`, did you check the `available` field before trusting output?
- On `available: false`, did you translate to PowerShell rather than retrying bash or
  hard-coding a bash path?
- Are all paths in the command absolute?
- Did you avoid shelling out for session log, TODO, requirements, or traceability work,
  routing those through `mcpserver-claude-code-plugin` instead?
- Is the command free of `powershell.exe` (5.1) and free of invented tool names or fields?

## Common Pitfalls

- Assuming `run_bash` is always present. It is optional and returns `available: false`
  when Git Bash is not on `PATH`.
- Retrying `run_bash` in a loop after `available: false`. The result will not change within
  the same host state; fall back to `run_powershell`.
- Copying a POSIX command verbatim into `run_powershell`. POSIX redirects and builtins
  (`/dev/null`, `$VAR`, `grep`) are not PowerShell syntax and will fail.
- Targeting `powershell.exe`. The repo standard is `pwsh.exe` (PowerShell 7+).
- Using a shell to read or edit `docs/Project/TODO.yaml` or session-log files. Use the
  plugin `workflow.*` / `client.*` methods.
- Relying on working-directory persistence between tool calls. Use absolute paths every
  time.
