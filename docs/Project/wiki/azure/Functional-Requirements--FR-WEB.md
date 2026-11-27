# FR-WEB

[Back to Functional Requirements](Functional-Requirements)

## FR-WEB-001 Orchestration cockpit in mcp-web

mcp-web SHALL present a single orchestration dashboard that shows the active workspace, agent fleet, TODO bank, live activity, and global controls without requiring a separate terminal per agent.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Opening mcp-web for an authenticated workspace renders fleet, task bank, and header controls on one page.
- [ ] The page does not require the operator to open one terminal window per agent to see status.
- [ ] Refreshing the page restores the same workspace binding and current statuses from MCP APIs.

## FR-WEB-002 Workspace is the mission

mcp-web SHALL treat an MCP workspace as the Prompter Hawk "mission": one named project folder shared by all agents. The operator can bind the dashboard to an existing registered workspace; mcp-web SHALL NOT invent a parallel mission store.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Dashboard title and path equal the bound workspace name and workspacePath from MCP workspace APIs.
- [ ] Switching workspace retargets agent pool, TODOs, and session logs to that workspace only.
- [ ] No .prompter-hawk or other third-party mission directory is required for the dashboard to function.

## FR-WEB-003 Agent fleet lifecycle and status

mcp-web SHALL show each pooled agent as off, idle, working, or waiting-for-feedback, and SHALL start, stop, recycle, start-all, and stop-all through the agent pool API.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Fleet panel lists every configured pooled agent with one of off, idle, working, waiting-for-feedback.
- [ ] Start and Stop on one agent call AgentPool start/stop and the status updates without a full reload.
- [ ] Start All and Stop All act on every listed agent and report per-agent success or failure.
- [ ] Recycle on one agent calls AgentPool recycle, the agent returns to idle or off without requiring a browser reload, and in-flight TODO status is preserved or marked failed according to the pool result.

## FR-WEB-004 TODO task bank lanes

mcp-web SHALL present MCP TODOs in lanes equivalent to Prompter Hawk task categories: tentative, pending, in progress, blocked, feedback-required, recurring, and completed. MCP TODO storage remains the only source of truth.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Every TODO in the bound workspace appears in exactly one lane derived from status and tags.
- [ ] Creating a TODO from the bank uses the MCP TODO API and never writes docs/todo.yaml or TODO.yaml directly.
- [ ] Lane filters by agent assignee and priority match the stored TODO fields.

## FR-WEB-005 Fire-and-forget parallel dispatch

The operator SHALL be able to create or approve TODOs and leave. Eligible idle agents SHALL pick up pending work in parallel. Returning to mcp-web SHALL show current status without the operator babysitting a chat.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Two pending TODOs assigned to two idle agents both enter in_progress without a third operator prompt.
- [ ] Closing the browser does not cancel in-progress pool work.
- [ ] Reopening mcp-web shows those TODOs in their live statuses from the store.

## FR-WEB-006 Recurring scheduled TODOs

mcp-web SHALL let the operator attach a cron-style schedule (hourly, daily, weekly, or custom) to a TODO so MCP recreates or reopens that work on the schedule (daily test+fix, weekly docs, north-star summary).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A TODO saved with a daily 06:00 UTC schedule appears in the recurring lane.
- [ ] When the schedule fires, a pending TODO exists for that template without a manual click.
- [ ] Disabling the schedule stops further firings and leaves historical TODOs intact.

## FR-WEB-007 Hierarchical context inheritance

Context configured at workspace, then agent, then TODO SHALL merge in that order and travel automatically into the dispatched agent run so the operator does not paste the same instructions into every task.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Workspace context is present in every dispatched run in that workspace.
- [ ] Agent persona/prompt overrides workspace text for that agent only.
- [ ] TODO description is the most specific layer and is included in the run payload.

## FR-WEB-008 One-click retry with preserved context

A failed or completed TODO SHALL be retryable from mcp-web in one action that reuses the original TODO identity, hierarchical context, and prior session linkage. The operator SHALL NOT copy-paste prompts.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Retry on a failed TODO enqueues new work bound to the same TODO id or a recorded retry child linked to it.
- [ ] The retry payload includes the original description plus workspace and agent context.
- [ ] The UI does not require the operator to locate a prior chat transcript to retry.

## FR-WEB-009 Live peek of agent work

mcp-web SHALL show live reasoning, tool calls, model id, and message history for a selected in-progress agent or TODO from the MCP session log and event stream (Prompter Hawk Live Peek).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Selecting a working agent streams new dialog and tool_call items without a manual refresh.
- [ ] A completed TODO still opens its full message history.
- [ ] Live peek reads session log APIs; it does not scrape terminal windows.

## FR-WEB-010 Token burn and daily progress

mcp-web SHALL chart token burn versus a workspace baseline (color-coded) and show today's lines added/removed, commits, files analyzed, peak parallelism, and TODO completions.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A 24-hour burn chart renders from session-log token counts.
- [ ] Burn rate above the workspace baseline uses a distinct warning color; at or below baseline uses a distinct normal color.
- [ ] Progress metrics include commits, net line change, peak concurrent working agents, and completions for the current UTC day.
- [ ] Progress metrics show lines added and lines removed as separate counts, not only net change.
- [ ] Progress metrics include files analyzed for the current UTC day.

## FR-WEB-011 Human feedback queue

When an agent needs validation, mcp-web SHALL queue a non-blocking feedback request. Other agents SHALL continue. The operator answers on their schedule with structured options plus free text.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A feedback-required TODO appears in the feedback lane while other in_progress TODOs stay in progress.
- [ ] Submitting feedback resumes or fails that TODO through MCP APIs.
- [ ] The request records what to test, an optional command, a question, and response options.

## FR-WEB-012 Idle orchestrator proposes tentative TODOs

An optional orchestrator mode SHALL detect idle agents and create tentative TODOs for operator approval. Tentative items SHALL NOT dispatch until approved (Prompter Hawk Team Captain, mapped to MCP TODOs).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] With orchestrator enabled, an idle agent with an empty pending queue results in at least one tentative TODO.
- [ ] Tentative TODOs are not assigned to running agents until the operator approves.
- [ ] Disabling orchestrator stops new proposals; existing tentative items remain until dismissed or approved.

## FR-WEB-013 Tool and path allow/deny UI

mcp-web SHALL let the operator set per-agent or workspace allow/deny rules for tools (including shell, read, write, edit) and path globs (for example allow src/*, deny .env). Denied actions fail closed.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Saving an allow src/* and deny .env rule persists through the workspace policy API.
- [ ] A denied path or tool is rejected with a visible error on the agent run, not silently ignored.
- [ ] Rules are visible and editable in mcp-web without editing YAML by hand.

## FR-WEB-014 Per-agent multi-provider backends

Each pooled agent SHALL be assignable in mcp-web to a configured backend (Claude, OpenAI, Gemini, Grok, or other already-supported MCP providers) and model id, mixing providers in one workspace.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Two agents in one workspace can show two different providers and models.
- [ ] Changing an agent model persists and is used on the next start.
- [ ] mcp-web does not add a billed proxy; it uses operator-configured provider credentials already known to MCP Server.

## FR-WEB-015 TODO to git commit linkage

When task-to-commit mapping is enabled, completing a TODO SHALL show linked commit SHAs from session-log commit actions on that TODO's card.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A completed TODO with a session-log commit action displays the SHA on the card.
- [ ] Disabling the mapping hides SHAs but does not delete session-log actions.
- [ ] Missing commits render as no-link, not as a fabricated SHA.

## FR-WEB-016 mcp-web local-first privacy

mcp-web SHALL NOT upload prompts, file contents, or diffs to a product analytics host. Code leaves the machine only toward AI providers the operator already configured on MCP Server.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Default mcp-web configuration has no third-party analytics endpoint for prompt or file payloads.
- [ ] Automated tests fail if a dashboard bundle sends prompt text or file bodies to a non-MCP, non-provider host.
- [ ] Operator secrets are not rendered in live peek beyond existing session-log redaction rules.

## FR-WEB-017 Desktop and mobile viewports

The orchestration dashboard SHALL remain usable at 1280px desktop and 390px mobile widths: fleet, bank, and live peek must be reachable without clipped primary controls.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] At 1280x800, fleet and task bank are both visible without horizontal clipping of primary controls.
- [ ] At 390x844, fleet, bank, and peek are each reachable through a documented navigation pattern.
- [ ] Start All / Stop All remain operable at both widths.

## FR-WEB-018 TODO dependencies and blocked lane

A TODO MAY declare prerequisite TODO ids. mcp-web SHALL show it blocked until every prerequisite is done, matching Prompter Hawk prerequisite behavior on MCP TODOs.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A TODO with an incomplete prerequisite appears in the blocked lane and is not auto-dispatched.
- [ ] When the last prerequisite is done, the TODO moves to pending without a manual status edit.
- [ ] Cycles are rejected with a visible validation error.

## FR-WEB-019 Persistent specialized agent prompts

Each agent SHALL have an editable persona/system prompt in mcp-web that survives process restart and is applied to later runs (Prompter Hawk agent context prompts).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Editing an agent prompt and restarting that agent still uses the new prompt.
- [ ] Prompts are stored through MCP agent/template/memory APIs, not only in browser localStorage.
- [ ] Clearing the prompt reverts to the agent definition default.

## FR-WEB-020 Per-agent spend budgets

mcp-web SHALL expose optional hourly, daily, and monthly USD caps per agent. Exceeding a cap SHALL fail new dispatches for that agent with a visible error.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Saving hourly and daily caps persists on the agent configuration.
- [ ] When the hourly cap is exceeded, a new dispatch for that agent fails closed and the TODO stays pending or failed, not silently running.
- [ ] Clearing caps restores unlimited dispatch for that agent.

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
