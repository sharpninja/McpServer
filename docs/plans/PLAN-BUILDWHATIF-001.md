<!-- SPDX-License-Identifier: Apache-2.0 -->
# Plan: `./build.ps1 -WhatIf`, a full dry run with checks (PLAN-BUILDWHATIF-001)

Status: draft for operator approval (2026-10-10). Implements TR-MCP-NUKE-005 under FR-MCP-BUILD-001 "Tooling for repeatable builds".

## Context

Operator rulings this plan encodes, verbatim (2026-10-10):

- "FR: Project will provide tooling for repeatable builds." and "Now, the TR are implementation detail of the FR."
- "The unit test becomes running `build.ps1 -WhatIf`", with the choice "Full dry run with checks (Recommended)".
- "What you are doing is offloading your lack of long-term retention to a mechanical process.  WHICH IS THE ENTIRE PURPOSE OF QBRAIN.AI.  The better recommendation is a reusable feature, not a one-off unit test."
- The first rulings to enforce: "Build.Tests is infrastructure, not application and should not be treated as unit tests.", "NUKE is only accountable through TR.", and "Never edit an npm lockfile.  It belongs to npm.  If you want it changed, use npm."

Starting point:

- `build.ps1` declares `[CmdletBinding()]` without `SupportsShouldProcess`, relaunches itself under a clean `pwsh`, and forwards every argument to `dotnet run --project build/_build.csproj`.
- Nothing under `build/` honors a dry run.
- The 38 Nuke targets perform side effects directly. They call DotNet tasks, start processes, write files and folders, and run git. SyncAgentPlugins alone has 18 such calls and UpdateService has 14.

## Acceptance criteria (TR-MCP-NUKE-005)

- **AC-001.** `build.ps1` declares `SupportsShouldProcess` and passes `-WhatIf` through to the Nuke build. A `-WhatIf` run changes nothing: no file writes, restores, builds, tests, publishes, deploys, network writes, or git changes. `git status` and the working tree are identical afterwards.
- **AC-002.** A `-WhatIf` run resolves the .NET SDK, the solution named by `.nuke/parameters.json`, the shared build project set (TR-MCP-NUKE-002), and the full target graph. It fails when any of them cannot be resolved; this would have caught BUG-BUILD-001 and BUG-BUILD-002.
- **AC-003.** A `-WhatIf` run executes every check (ValidateConfig, ValidateTraceability, and the ruling checks). It fails on any finding and reports each with its rule and evidence.
- **AC-004.** Each side-effecting step reports the exact command or change it would make instead of performing it.
- **AC-005.** Ruling checks are data: each ruling is recorded once with its verbatim source and a checkable predicate, so a new ruling adds a check without new test code.

## Design

1. **The `build.ps1` seam.**
   - `build.ps1` becomes `[CmdletBinding(SupportsShouldProcess)]`.
   - When `$WhatIfPreference` is set, it passes `-WhatIf` to its own non-interactive relaunch and then appends `--what-if` to the Nuke arguments. It makes no other change.
2. **The Nuke parameter and the side-effect gateway.**
   - Nuke gains `[Parameter] readonly bool WhatIf`.
   - A new `build/SideEffects.cs` is the only place that performs a side effect: DotNet tasks, `ProcessTasks.StartProcess`, file and folder writes, copies, moves, and deletes, git, and network calls that write.
   - Each gateway method takes the exact command or change as data. In a normal run it performs it. In a `-WhatIf` run it records `WHATIF <target>: <exact command or change>`, skips it, and returns a typed "not performed" result. Dependent steps then report what they would do with that result.
   - Every target is migrated to the gateway in the same change. A Build.Tests source guard, in the style of BoundedProcess AC-006, fails when any build source outside `SideEffects.cs` calls a side-effecting API directly. There is no exclusion list.
3. **Resolution (AC-002).**
   - In a `-WhatIf` run, a resolution phase runs before any target. It reads `global.json` and `dotnet --list-sdks` (read-only), checks that the solution from `.nuke/parameters.json` exists and loads, and checks that `GetBuildProjectFiles` returns only existing projects that the solution or the Restore extension covers.
   - It then asks Nuke's execution planner for the plan of the requested targets, or of every target when none is named, and prints it. Any resolution failure exits non-zero.
4. **Checks (AC-003).** In a `-WhatIf` run, ValidateConfig, ValidateTraceability, and every ruling check run for real, because they are read-only. Their findings are collected, printed with rule and evidence, and decide the exit code.
5. **Ruling data (AC-005).**
   - Rulings live in one YAML file. Each ruling has `id`, `ruling` (the verbatim quote), `by`, `date`, `scope`, and `predicate` (`kind` and `args`).
   - Predicate kinds are implemented once in C#, and each is unit-tested with seeded violations. A new ruling that fits an existing kind needs only a YAML entry. A new kind needs code and its test.
   - The first three rulings and their kinds are:
     - `project-excluded-from-targets`: tests/Build.Tests is not in the Test target's project set (Build.Tests-as-infrastructure).
     - `tr-family-maps-only-to-fr`: every `TR-MCP-NUKE-*` row of TR-per-FR-Mapping maps only to FR-MCP-BUILD-001 and has no TEST ids (Nuke-through-TR).
     - `npm-lockfile-npm-owned`: for every package-lock.json in scope, each local `file:` dependency's `integrity` equals the SHA-512 of the referenced tarball, and `npm install --package-lock-only --dry-run --ignore-scripts` reports no lockfile change (npm-owned lockfiles; this would have caught the EINTEGRITY in BUG-BASELINE-012).
6. **Reuse.** The workspace hygiene rules engine (FR-MCP-HYGIENE-002/-003) runs inside the server. Making the build depend on a running server would make builds less repeatable, so the ruling predicates stay in the build. Feeding the same rulings file to the hygiene engine is a follow-up TODO, not part of this plan.

## Test-first order (Byrd v4)

All of these are Build.Tests cases, run in the Build.Tests infrastructure step and accountable through TR-MCP-NUKE-005 only.

- **RED (behavior).**
  - `./build.ps1 -WhatIf` today is rejected as an unknown parameter (exit non-zero).
  - A `-WhatIf` Compile run would restore and build.
  - Seeded violations in temporary fixture trees go unreported: Build.Tests in the Test project set, a TR-MCP-NUKE TR mapped to an application FR, a `file:` dependency with a stale integrity, and a `.nuke/parameters.json` naming a missing solution.
- **RED (structure).** The side-effect guard lists today's direct calls.
- **Green.**
  - `./build.ps1 -WhatIf` exits 0 on this tree, prints the target plan and the `WHATIF` lines, and leaves `git status --porcelain` and the tree's file hashes unchanged.
  - Each seeded violation exits non-zero with its rule id and evidence.
  - The guard finds no direct call.
  - A normal (non-what-if) run of Compile, ValidateConfig, and the full Build.Tests suite is unchanged.
- **HV.** Codex gpt-6-sol xhigh before done.

## Scope and risk

The migration touches all 38 targets in one change set, because the guard allows no exclusion list. Regressions are guarded by:

- the full Build.Tests suite;
- G1 Compile;
- ValidateConfig;
- one real run of each target that is safe to run locally.

Targets that deploy or publish (UpdateService, PublishNuGet, deploy targets) are verified only in `-WhatIf` and by their existing tests.

## Decisions for the operator

- **D1. Build tool compile.** `dotnet run` compiles `build/_build.csproj` into `build/bin` and `build/obj`, which are ignored by git, before Nuke runs. Options:
  - (a) accept that as the only write of a `-WhatIf` run, since git status is unchanged (recommended);
  - (b) require an already compiled build tool (`--no-build`) and fail when it is missing.
- **D2. Rulings file location.** Options:
  - `build/rulings.yaml`, next to the code that evaluates it (recommended);
  - `docs/Project/rulings.yaml`, next to the requirements.
- **D3. The npm-lockfile predicate** shown above (integrity check plus npm's own dry run), or a stricter one that runs `npm ci --dry-run` per package.
- **D4. Default scope.** `./build.ps1 -WhatIf` with no target plans every target (recommended, the TR's "full target graph"), or only the default target.
