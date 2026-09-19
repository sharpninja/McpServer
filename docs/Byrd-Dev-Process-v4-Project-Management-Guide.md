# Project Management Guide

## Byrd Dev Process v4 on McpServer — Astra, Sol, and Grok

Audience: the human coordinator (project lead) running AI-assisted delivery in a McpServer workspace.
Canonical process reference: `docs/Development-Process-draft-v4.md` in [sharpninja/McpServer](https://github.com/sharpninja/mcpserver). In-repo aliases `Byrd Dev Process`, `BDP`, and `BDPv4` all resolve to that file, per `CODEX-HANDOFF.md`.

Version 4 — three named personas with separated authority, their durable persona prompts (§11), and concrete launch commands (§12).

---

## 1. The three personas

| Persona | Role | Engine | Sandbox authority | Never does |
|---|---|---|---|---|
| **Astra** | Planning | Codex CLI, `--profile astra` | `read-only` (Codex default) | Never writes production code or tests |
| **Sol** | Coding | Codex CLI, `--profile sol` | `workspace-write` | Never certifies its own work |
| **Grok** | Hostile validation | Grok Build, adversarial sub-agent spawn | `read-only` plus test/HTTP execute | Never plans, never fixes what it finds |

The separation of authority is the point. Astra decides *what* and *why*; Sol produces *how*; Grok refuses to believe either of them. The process doc's core insight — that an AI's failure mode is marching into oblivion with machine efficiency — is only contained when the actor who claims success is not the actor who verifies it.

### 1.1 Critical: Astra and Sol share an engine

Both personas run Codex CLI. Prompt text alone will not keep them apart — a planning session that can write files *will* eventually write files. Enforce separation mechanically:

| Boundary | Mechanism |
|---|---|
| Configuration | Separate profiles: `$CODEX_HOME/astra.config.toml`, `$CODEX_HOME/sol.config.toml`, invoked with `--profile astra` / `--profile sol` ([Codex CLI reference](https://developers.openai.com/codex/cli/reference)) |
| Write authority | Astra always `--sandbox read-only`; only Sol may pass `--sandbox workspace-write`. Astra's profile must not set a writable default |
| Working tree | Sol works in a dedicated branch or git worktree per slice; Astra runs against `main` read-only |
| Session identity | Distinct session logs and distinct `sourceType` (§3) |
| Reasoning effort | Both `model_reasoning_effort="xhigh"`; planning quality and implementation quality both depend on it |

If Astra needs to record a plan, it writes through MCP (TODOs, requirements, plan artifacts) rather than the filesystem. That is the one write path a read-only sandbox still permits, and it keeps the plan in the system of record instead of a loose markdown file.

---

## 2. Role responsibilities

| Responsibility | Astra | Sol | Grok | Coordinator |
|---|---|---|---|---|
| Scope, V² decision | Recommends | — | — | **Decides** |
| FR / TR / TEST authoring | **Owns** | Refines when a test exposes a defect | Verifies ids exist in docs *and* store | Approves |
| Component design, public interfaces | **Owns** | Implements to them | Verifies they match what shipped | Approves |
| Iteration phase and TODO decomposition | **Owns** | — | — | Approves |
| Acceptance unit tests | Specifies criteria | **Writes (Red)** | Re-runs independently | — |
| Implementation (mocks → Green → refactor) | — | **Owns** | Re-runs gates | Steers in real time |
| Completion claims | — | Submits receipt | **Authorizes or refuses** | Final call on DISAGREE |
| Deploy, environment promotion | — | — | Verifies health and UI proof | **Owns** |
| Guideline updates after drift | Updates planning guidance | Updates its own guidance after a tangent | Files what it caught | Approves |

The process doc is explicit that human interaction during implementation is not passive observance; an experienced coordinator steering in real time is what prevents a large budget burn down a path the agent would eventually escape anyway. Grok catches what steering misses; it does not replace steering.

---

## 3. Identity and audit — the change you asked for

You chose **persona as `sourceType`**. That is a deliberate deviation from current workspace policy, so it has to be implemented, not just declared.

`AGENTS.md` rule 11 and `templates/prompt-templates.yaml` currently require `sourceType` and the SessionId `<Agent>` prefix to be the agent's real engine identity in Pascal-Case, naming `Codex` as the valid example and rejecting placeholders. Under the new model, `Astra`, `Sol`, and `Grok` become the logged identities.

**Files to update before the first persona-logged session:**

1. `AGENTS.md` rule 11 — redefine "real agent identity" as the persona identity from the approved persona registry, Pascal-Case.
2. `templates/prompt-templates.yaml` — the `sourceType` guidance and the `<Agent>` slug rules (the marker file is rendered from this, so editing only `AGENTS-README-FIRST.yaml` will be overwritten on the next server start).
3. Any test asserting accepted identity values, then re-run `./build.ps1 Test` — this is itself a Byrd slice and needs the normal gates.

**Mandatory compensating control.** Two personas on one engine means persona alone no longer tells you which engine produced a turn. Every turn must therefore also carry the engine:

- `model` field set to the actual Codex model id
- a turn tag `engine:Codex` (or `engine:Grok`)
- a turn tag `profile:astra` / `profile:sol`

Without this you lose the ability to attribute a behavior pattern to an engine, which is exactly the data you need for the §9 metrics.

**One exception, kept on purpose.** The hostile-validator receipt field `ValidatorIdentity` stays `GrokSubagentHostile` as specified in `docs/McpServer-UseCase-Extension-Design-v3.0.md` §6.1. `ValidatorIdentity` is not `sourceType`; it asserts *the validator was a separate adversarial spawn*, which is the property the gate depends on. Grok's session log uses `sourceType: Grok`; its receipts keep `GrokSubagentHostile`.

Session ids are still generated with `session.init` or `New-McpSessionLogSlug -Agent Sol -Model <model>` — never handcrafted.

---

## 4. Sources of truth

| Data | Source of truth | Never do this |
|---|---|---|
| TODOs / work items | The configured database, via `workflow.todo.*` / `todo_*` tools | Never read or write `docs/Project/TODO.yaml` — read-only projection |
| Requirements | `docs/Project/Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, `TR-per-FR-Mapping.md`, `Requirements-Matrix.md` | Never add an FR/TR/TEST id in code without the doc entry |
| Work history | Session log via the plugin (`workflow.sessionlog.*`) | Never hand-edit session log files |
| Completion truth | The latest hostile-validator receipt pair in `docs/receipts/` | Never trust a plan `[x]`, an implementer narrative, or an implementer-authored receipt |
| Connection details, API key, agent prompt | `AGENTS-README-FIRST.yaml` (regenerated on server start from `templates/prompt-templates.yaml`) | Never hardcode the key; it rotates every restart |
| Any YAML change | Deserialize → mutate object → serialize → save (`plugins/core/lib-ps/yaml-object-mutation.ps1`) | Never append, replace, or remove YAML lines as text |

---

## 5. The lifecycle, with handoffs

### 5.1 Phase and status machine

Each iteration is one `IterationPhase` (`Planning → Implementing → Validating → Complete`, plus `Blocked`, `Cancelled`), owning many TODOs (`docs/byrd-todo-execution-spec.md`). Persona ownership maps directly onto the TODO status machine:

```
Draft ─ Planned ─ TestDesign ─ TestReady ─ Implementing ─ Validating ─ Complete
└──────── Astra ────────┘    └──────── Sol ────────┘    └── Grok ──┘   │
                                   ↑                         │         │
                                 Blocked ←── DISAGREE ────────┘         │
                                                        AGREE ──────────┘
```

Enforced transitions, which are your gate definitions:

- `TestDesign → TestReady` only when `UnitTestsDefined == true`
- `TestReady → Implementing` — tests exist and fail for the right reason before code
- `Implementing → Validating` only when code changes or checkpoints exist
- `Validating → Complete` only when unit tests pass, required integration tests pass, every acceptance criterion is satisfied or explicitly waived, **and Grok returns AGREE**
- Any state → `Blocked`; leaving `Blocked` requires an explicit reason
- `Complete` is terminal unless reopened by requirement refinement

### 5.2 Handoff contracts

**Astra → Sol.** Astra hands over a slice only when every item exists in MCP state: FR/TR/TEST ids present in both the docs and the store; acceptance criteria stated in testable terms; `dependsOn` all valid persisted TODO ids; public interfaces designed and documented; target test project and production project named; `PlanningDecision` checkpoints recording alternatives considered and rejected. A slice missing any of these goes back to Astra, not into Sol's queue.

**Sol → Grok.** Sol hands over an implementer receipt plus the TODO in `Validating`, never in `Complete`. Sol's receipt is explicitly *untrusted input* — the receipt path is recorded in the hostile brief as `UntrustedImplementerReceipt`. Sol's own claim of green is worth nothing until re-verified.

**Grok → Coordinator.** Grok returns AGREE or DISAGREE with a claim-by-claim verdict. DISAGREE is not a failure of the process; §6.1 states it plainly: DISAGREE is process success, honesty preserved. On DISAGREE the coordinator routes each FAIL claim back — requirement defects to Astra, implementation defects to Sol — and Grok re-runs. Grok never fixes what it finds; a validator that patches becomes an implementer and the gate collapses.

### 5.3 The Byrd Test Gate

From `AGENTS.md` and `skills/byrd-tdd-process/SKILL.md`, the hard exit condition for any slice:

> The entire unit test suite for the current iteration and all previous iterations must pass, with **zero failures and zero skips** in the executed scope.

A skipped test is not a passing test. Deferred work belongs in MCP TODO or requirements state, never in a skipped-test placeholder. Sol runs these; Grok re-runs them independently:

```powershell
./build.ps1 Test                  # unit gate; excludes *.IntegrationTests, Category=Integration, Category=AiReview
./build.ps1 ValidateConfig        # appsettings instance validation
./build.ps1 ValidateTraceability  # FR/TR/TEST coverage across docs and doc-comments
./build.ps1 MigrationIntegrationTests   # integration suites, after the unit gate is green
```

Traceability is a gate, not paperwork: new public types and members need XML docs (`TreatWarningsAsErrors` plus CS1591 break the build), and every new `FR-MCP-*` / `TR-MCP-*` / `TEST-MCP-*` id must be referenced in source or test doc-comments.

---

## 6. Runbooks

This section is the policy for each persona's turn. The copy-paste launch commands and launcher scripts are consolidated in §12.

### 6.1 Session start (all personas, every session)

1. Coordinator starts the server: `./build.ps1 StartServer --instance default` (Swagger at `http://localhost:7147/swagger`).
2. Persona reads `AGENTS-README-FIRST.yaml` for the current API key and endpoints, then `AGENTS.md` for durable policy.
3. Trust handshake per the process doc: health check → verify the cryptographic signature in the marker file → one-time nonce challenge. Use `Invoke-FullBootstrap` from `plugins/core/lib-ps/marker-resolver.ps1`, which performs all three; never hand-parse the marker (§12.2). On any failure, log `MCP_UNTRUSTED`, fall back to internal memory, and stop touching MCP endpoints. No probing around a failed handshake.
4. Bootstrap or resume the session log with the persona `sourceType` plus the engine and profile tags from §3.
5. Hydrate only the active TODO plus relevant deltas. The execution spec exists so personas resume from MCP state rather than chat memory.

### 6.2 Astra — planning

One-time profile, `$CODEX_HOME/astra.config.toml`:

```toml
model_reasoning_effort = "xhigh"
sandbox_mode = "read-only"
```

Interactive planning:

```bash
codex --profile astra --sandbox read-only -C /path/to/workspace
```

Unattended planning pass:

```bash
codex exec --profile astra --sandbox read-only \
  --json --output-last-message ./receipts/astra-plan-ITER-07.md \
  "$(cat ./prompts/astra-plan-ITER-07.md)"
```

Astra's deliverables all land in MCP, not the filesystem: `create_iteration_phase`, `create_todos_from_plan`, `set_todo_test_plan`, requirements entries, and `PlanningDecision` checkpoints. Astra's success criterion is that Sol never has to ask a clarifying question about scope — only about implementation.

### 6.3 Sol — coding

One-time profile, `$CODEX_HOME/sol.config.toml`:

```toml
model_reasoning_effort = "xhigh"
sandbox_mode = "workspace-write"
```

MCP registration, once per machine:

```bash
codex mcp add mcpserver --url http://localhost:7147/mcp-transport \
  --bearer-token-env-var MCP_API_KEY
codex mcp list
```

Codex activation in the workspace is the hook lifecycle via `.codex-plugin/plugin.json` (`MarkerFileService`); sync it with `./build.ps1 SyncAgentPlugins`.

Interactive slice work, steered:

```bash
codex --profile sol --sandbox workspace-write -C /path/to/workspace
```

Unattended slice work, mirroring how the server itself drives Codex — one-shot `codex exec` at highest reasoning effort:

```bash
codex exec --profile sol --sandbox workspace-write \
  -c model_reasoning_effort="xhigh" \
  --json --output-last-message ./docs/receipts/sol-MCP-TODOPROGRESSION-001.md \
  "$(cat ./prompts/sol-slice-MCP-TODOPROGRESSION-001.md)"

codex exec --profile sol resume --last \
  "unit gate is red on TodoProgressionTests; fix without weakening assertions"
```

`codex exec` defaults to a read-only sandbox, so `workspace-write` is a deliberate grant; reserve `danger-full-access` for isolated runners; `--json` with `--output-last-message` yields machine-readable progress plus the human summary that becomes Sol's untrusted receipt ([non-interactive mode](https://learn.chatgpt.com/docs/non-interactive-mode)). In GitHub Actions prefer the Codex action over installing and authenticating the CLI, and never set `CODEX_API_KEY` as a job-level environment variable in a job that runs repository-controlled code.

Sol's loop per TODO: Red → mocks-green → real Green → refactor → full unit gate → `Validating`. Sol stops at `Validating`. Sol does not write "done."

### 6.4 Grok — hostile validation

The contract already exists in `docs/McpServer-UseCase-Extension-Design-v3.0.md` §6.1. Its operating principle: **default every claim to FAIL or UNKNOWN until independently re-verified with tools.** Grok does not trust implementer narrative, plan `[x]` checkboxes, or implementer-authored receipts.

Setup:

```bash
grok login --device-auth
grok mcp add                 # register the mcpserver Streamable HTTP endpoint
grok plugin install <mcpserver-grok-plugin source>
grok plugin enable <plugin>
grok inspect                 # confirm rules, skills, plugins, hooks, MCP servers
```

The skill lives at `~/.grok/skills/hostile-validator/SKILL.md` and workspace `.grok/skills/hostile-validator/SKILL.md`. Grok Build loads enabled plugin skills, hooks, and MCP servers from the Grok/Claude-compatible manifests (`MarkerFileService`); installed plugins land under `~/.grok/installed-plugins`, kept in step by `./build.ps1 SyncAgentPlugins`. Tool naming: use `sessionlog_*`, `todo_*`, `requirements_*` when the Streamable HTTP MCP server is discoverable; `mcp_*` are hosted-agent aliases and `workflow.*` are plugin shim / REPL method names, not literal Grok tool results.

Validation run — separate spawn, plan-permission mode, highest effort, in a worktree so it cannot disturb Sol's tree:

```bash
grok -p "$(cat ./prompts/hostile-MCP-TODOPROGRESSION-001.md)" \
  --cwd . --permission-mode plan \
  --output-format plain --effort high --reasoning-effort high --max-turns 60
```

`--effort high` is the strongest level current Grok CLIs accept; the server's own strategy notes `max` is rejected. Use `--output-format streaming-json` when a script must parse events, and `grok export <session-id>` to capture the transcript ([Grok CLI reference](https://docs.x.ai/build/cli/reference)).

**When Grok must run** (§6.1, verbatim intent):

1. Before marking any step complete.
2. Before any status report claiming pass, green, done, complete, or deploy success.
3. After any material code or requirements change that could invalidate prior receipts.
4. Before updating plan checklists.

**Forbidden substitutes:** Sol self-review labeled "hostile"; a PowerShell script the implementer wrote (`Invoke-HostileValidator.ps1` is an optional evidence collector only, and only if the sub-agent chooses it); Astra or any Codex profile performing the review. Same engine, same blind spots — and the operator will not burn planning capacity babysitting the implementer.

**Required outputs:** `docs/receipts/hostile-validator-<utc>.md` plus its JSON twin, `ValidatorIdentity: GrokSubagentHostile`, and `OverallVerdict: AGREE` only if every claim is PASS, otherwise `DISAGREE`.

### 6.5 Transcript ingestion

`McpServer.SessionLog.Transcripts` normalizes transcripts for Codex, Grok, Claude, Cline, Copilot, and OpenCode, and `/mcpserver/sessionlog` supports transcript import with full-text search. Policy: import at the end of every unattended run. An Astra-planned, Sol-implemented, Grok-validated slice must read as one continuous project history under three personas.

---

## 7. Risk management

The process doc names three AI failure modes. With three personas, each has a persona-specific response.

| Risk | Signal | Response |
|---|---|---|
| Context compaction amnesia | Stops logging turns, forgets workspace steps, loops on failing tests, starts calling a test "invalid" | Steering message to re-read workspace instructions; repetition also raises the weight compaction retains. Sol is most exposed — never leave long Sol runs unattended |
| Rogue session 