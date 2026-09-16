#Requires -Version 7.0
# Queues Prompter Hawk -> mcp-web FR/TR/TEST/mapping records into the V4 failsafe
# pending directory. Does not call MCP HTTP. Object-first YAML only.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pluginRoot = 'C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f'
$workspace = 'F:\GitHub\McpServer'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:MCP_PLUGIN_HOST = 'grok'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
$env:MCPSERVER_WORKSPACE_PATH = $workspace
$env:GROK_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace

. (Join-Path $pluginRoot 'lib\yaml-object-mutation.ps1')
. (Join-Path $pluginRoot 'lib\resolve-cache-dir.ps1')
Import-McpYamlSerializer

$utc = (Get-Date).ToUniversalTime()
$stamp = $utc.ToString('yyyyMMddTHHmmssZ')
$iso = $utc.ToString('yyyy-MM-ddTHH:mm:ssZ')
$failsafeDir = Get-McpFailsafeDir -StartPath $workspace
[void][System.IO.Directory]::CreateDirectory($failsafeDir)

function New-Ac {
    param([string]$Id, [string]$Text)
    return [ordered]@{ id = $Id; text = $Text; isSatisfied = $false }
}

$notes = 'Proposed for mcp-web orchestration UI. Source: https://prompterhawk.dev/ and https://prompterhawk.dev/docs.html (2026-09-02). Map Prompter Hawk mission/agent/task onto MCP workspace/agent-pool/TODO. Do not implement until PLAN-WEB-ORCH-001 is approved. Replay via failsafe drain when storage returns.'

$fr = @(
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-001'; title = 'Orchestration cockpit in mcp-web'
        description = 'mcp-web SHALL present a single orchestration dashboard that shows the active workspace, agent fleet, TODO bank, live activity, and global controls without requiring a separate terminal per agent.'
        priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Opening mcp-web for an authenticated workspace renders fleet, task bank, and header controls on one page.')
            (New-Ac 'ac-2' 'The page does not require the operator to open one terminal window per agent to see status.')
            (New-Ac 'ac-3' 'Refreshing the page restores the same workspace binding and current statuses from MCP APIs.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-002'; title = 'Workspace is the mission'
        description = 'mcp-web SHALL treat an MCP workspace as the Prompter Hawk "mission": one named project folder shared by all agents. The operator can bind the dashboard to an existing registered workspace; mcp-web SHALL NOT invent a parallel mission store.'
        priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Dashboard title and path equal the bound workspace name and workspacePath from MCP workspace APIs.')
            (New-Ac 'ac-2' 'Switching workspace retargets agent pool, TODOs, and session logs to that workspace only.')
            (New-Ac 'ac-3' 'No .prompter-hawk or other third-party mission directory is required for the dashboard to function.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-003'; title = 'Agent fleet lifecycle and status'
        description = 'mcp-web SHALL show each pooled agent as off, idle, working, or waiting-for-feedback, and SHALL start, stop, recycle, start-all, and stop-all through the agent pool API.'
        priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Fleet panel lists every configured pooled agent with one of off, idle, working, waiting-for-feedback.')
            (New-Ac 'ac-2' 'Start and Stop on one agent call AgentPool start/stop and the status updates without a full reload.')
            (New-Ac 'ac-3' 'Start All and Stop All act on every listed agent and report per-agent success or failure.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-004'; title = 'TODO task bank lanes'
        description = 'mcp-web SHALL present MCP TODOs in lanes equivalent to Prompter Hawk task categories: tentative, pending, in progress, blocked, feedback-required, recurring, and completed. MCP TODO storage remains the only source of truth.'
        priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Every TODO in the bound workspace appears in exactly one lane derived from status and tags.')
            (New-Ac 'ac-2' 'Creating a TODO from the bank uses the MCP TODO API and never writes docs/todo.yaml or TODO.yaml directly.')
            (New-Ac 'ac-3' 'Lane filters by agent assignee and priority match the stored TODO fields.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-005'; title = 'Fire-and-forget parallel dispatch'
        description = 'The operator SHALL be able to create or approve TODOs and leave. Eligible idle agents SHALL pick up pending work in parallel. Returning to mcp-web SHALL show current status without the operator babysitting a chat.'
        priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Two pending TODOs assigned to two idle agents both enter in_progress without a third operator prompt.')
            (New-Ac 'ac-2' 'Closing the browser does not cancel in-progress pool work.')
            (New-Ac 'ac-3' 'Reopening mcp-web shows those TODOs in their live statuses from the store.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-006'; title = 'Recurring scheduled TODOs'
        description = 'mcp-web SHALL let the operator attach a cron-style schedule (hourly, daily, weekly, or custom) to a TODO so MCP recreates or reopens that work on the schedule (daily test+fix, weekly docs, north-star summary).'
        priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'A TODO saved with a daily 06:00 UTC schedule appears in the recurring lane.')
            (New-Ac 'ac-2' 'When the schedule fires, a pending TODO exists for that template without a manual click.')
            (New-Ac 'ac-3' 'Disabling the schedule stops further firings and leaves historical TODOs intact.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-007'; title = 'Hierarchical context inheritance'
        description = 'Context configured at workspace, then agent, then TODO SHALL merge in that order and travel automatically into the dispatched agent run so the operator does not paste the same instructions into every task.'
        priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Workspace context is present in every dispatched run in that workspace.')
            (New-Ac 'ac-2' 'Agent persona/prompt overrides workspace text for that agent only.')
            (New-Ac 'ac-3' 'TODO description is the most specific layer and is included in the run payload.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-008'; title = 'One-click retry with preserved context'
        description = 'A failed or completed TODO SHALL be retryable from mcp-web in one action that reuses the original TODO identity, hierarchical context, and prior session linkage. The operator SHALL NOT copy-paste prompts.'
        priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Retry on a failed TODO enqueues new work bound to the same TODO id or a recorded retry child linked to it.')
            (New-Ac 'ac-2' 'The retry payload includes the original description plus workspace and agent context.')
            (New-Ac 'ac-3' 'The UI does not require the operator to locate a prior chat transcript to retry.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-009'; title = 'Live peek of agent work'
        description = 'mcp-web SHALL show live reasoning, tool calls, model id, and message history for a selected in-progress agent or TODO from the MCP session log and event stream (Prompter Hawk Live Peek).'
        priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Selecting a working agent streams new dialog and tool_call items without a manual refresh.')
            (New-Ac 'ac-2' 'A completed TODO still opens its full message history.')
            (New-Ac 'ac-3' 'Live peek reads session log APIs; it does not scrape terminal windows.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-010'; title = 'Token burn and daily progress'
        description = 'mcp-web SHALL chart token burn versus a workspace baseline (color-coded) and show today''s lines added/removed, commits, files analyzed, peak parallelism, and TODO completions.'
        priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'A 24-hour burn chart renders from session-log token counts.')
            (New-Ac 'ac-2' 'Burn rate above the workspace baseline uses a distinct warning color; at or below baseline uses a distinct normal color.')
            (New-Ac 'ac-3' 'Progress metrics include commits, net line change, peak concurrent working agents, and completions for the current UTC day.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-011'; title = 'Human feedback queue'
        description = 'When an agent needs validation, mcp-web SHALL queue a non-blocking feedback request. Other agents SHALL continue. The operator answers on their schedule with structured options plus free text.'
        priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'A feedback-required TODO appears in the feedback lane while other in_progress TODOs stay in progress.')
            (New-Ac 'ac-2' 'Submitting feedback resumes or fails that TODO through MCP APIs.')
            (New-Ac 'ac-3' 'The request records what to test, an optional command, a question, and response options.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-012'; title = 'Idle orchestrator proposes tentative TODOs'
        description = 'An optional orchestrator mode SHALL detect idle agents and create tentative TODOs for operator approval. Tentative items SHALL NOT dispatch until approved (Prompter Hawk Team Captain, mapped to MCP TODOs).'
        priority = 'medium'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'With orchestrator enabled, an idle agent with an empty pending queue results in at least one tentative TODO.')
            (New-Ac 'ac-2' 'Tentative TODOs are not assigned to running agents until the operator approves.')
            (New-Ac 'ac-3' 'Disabling orchestrator stops new proposals; existing tentative items remain until dismissed or approved.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-013'; title = 'Tool and path allow/deny UI'
        description = 'mcp-web SHALL let the operator set per-agent or workspace allow/deny rules for tools (including shell, read, write, edit) and path globs (for example allow src/*, deny .env). Denied actions fail closed.'
        priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Saving an allow src/* and deny .env rule persists through the workspace policy API.')
            (New-Ac 'ac-2' 'A denied path or tool is rejected with a visible error on the agent run, not silently ignored.')
            (New-Ac 'ac-3' 'Rules are visible and editable in mcp-web without editing YAML by hand.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-014'; title = 'Per-agent multi-provider backends'
        description = 'Each pooled agent SHALL be assignable in mcp-web to a configured backend (Claude, OpenAI, Gemini, Grok, or other already-supported MCP providers) and model id, mixing providers in one workspace.'
        priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Two agents in one workspace can show two different providers and models.')
            (New-Ac 'ac-2' 'Changing an agent model persists and is used on the next start.')
            (New-Ac 'ac-3' 'mcp-web does not add a billed proxy; it uses operator-configured provider credentials already known to MCP Server.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-015'; title = 'TODO to git commit linkage'
        description = 'When task-to-commit mapping is enabled, completing a TODO SHALL show linked commit SHAs from session-log commit actions on that TODO''s card.'
        priority = 'medium'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'A completed TODO with a session-log commit action displays the SHA on the card.')
            (New-Ac 'ac-2' 'Disabling the mapping hides SHAs but does not delete session-log actions.')
            (New-Ac 'ac-3' 'Missing commits render as no-link, not as a fabricated SHA.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-016'; title = 'mcp-web local-first privacy'
        description = 'mcp-web SHALL NOT upload prompts, file contents, or diffs to a product analytics host. Code leaves the machine only toward AI providers the operator already configured on MCP Server.'
        priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Default mcp-web configuration has no third-party analytics endpoint for prompt or file payloads.')
            (New-Ac 'ac-2' 'Automated tests fail if a dashboard bundle sends prompt text or file bodies to a non-MCP, non-provider host.')
            (New-Ac 'ac-3' 'Operator secrets are not rendered in live peek beyond existing session-log redaction rules.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-017'; title = 'Desktop and mobile viewports'
        description = 'The orchestration dashboard SHALL remain usable at 1280px desktop and 390px mobile widths: fleet, bank, and live peek must be reachable without clipped primary controls.'
        priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'At 1280x800, fleet and task bank are both visible without horizontal clipping of primary controls.')
            (New-Ac 'ac-2' 'At 390x844, fleet, bank, and peek are each reachable through a documented navigation pattern.')
            (New-Ac 'ac-3' 'Start All / Stop All remain operable at both widths.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-018'; title = 'TODO dependencies and blocked lane'
        description = 'A TODO MAY declare prerequisite TODO ids. mcp-web SHALL show it blocked until every prerequisite is done, matching Prompter Hawk prerequisite behavior on MCP TODOs.'
        priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'A TODO with an incomplete prerequisite appears in the blocked lane and is not auto-dispatched.')
            (New-Ac 'ac-2' 'When the last prerequisite is done, the TODO moves to pending without a manual status edit.')
            (New-Ac 'ac-3' 'Cycles are rejected with a visible validation error.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-019'; title = 'Persistent specialized agent prompts'
        description = 'Each agent SHALL have an editable persona/system prompt in mcp-web that survives process restart and is applied to later runs (Prompter Hawk agent context prompts).'
        priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Editing an agent prompt and restarting that agent still uses the new prompt.')
            (New-Ac 'ac-2' 'Prompts are stored through MCP agent/template/memory APIs, not only in browser localStorage.')
            (New-Ac 'ac-3' 'Clearing the prompt reverts to the agent definition default.')
        )
    }
    [ordered]@{
        kind = 'fr'; id = 'FR-WEB-020'; title = 'Per-agent spend budgets'
        description = 'mcp-web SHALL expose optional hourly, daily, and monthly USD caps per agent. Exceeding a cap SHALL fail new dispatches for that agent with a visible error.'
        priority = 'medium'; area = 'WEB'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Saving hourly and daily caps persists on the agent configuration.')
            (New-Ac 'ac-2' 'When the hourly cap is exceeded, a new dispatch for that agent fails closed and the TODO stays pending or failed, not silently running.')
            (New-Ac 'ac-3' 'Clearing caps restores unlimited dispatch for that agent.')
        )
    }
)

$tr = @(
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-UI-001'; title = 'Host orchestration UI in McpServer.Web'
        description = 'The orchestration dashboard SHALL ship in src/McpServer.Web (mcp-web), extending FR-MCP-031. If the project is absent from the solution, restore or create it. Do not implement this UI inside Director.'
        priority = 'critical'; area = 'WEB'; subarea = 'UI'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'McpServer.sln contains McpServer.Web after Phase 1.')
            (New-Ac 'ac-2' 'The cockpit route is served by that project, not by McpServer.Support.Mcp HTML pairing pages except for login.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-UI-002'; title = 'Primer layout with desktop and mobile breakpoints'
        description = 'mcp-web SHALL use the existing Primer CSS / Octicons visual language and MUST satisfy FR-WEB-017 breakpoints in automated UI tests.'
        priority = 'high'; area = 'WEB'; subarea = 'UI'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Dashboard components reference Primer CSS or the current mcp-web design system, not a Prompter Hawk theme clone.')
            (New-Ac 'ac-2' 'Layout tests execute at 1280px and 390px widths.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-ORCH-001'; title = 'Fleet uses AgentPoolClient'
        description = 'Start, stop, recycle, status, start-all, stop-all, and queue dispatch SHALL call /mcpserver/agent-pool via AgentPoolClient. mcp-web SHALL NOT spawn agent processes itself.'
        priority = 'critical'; area = 'WEB'; subarea = 'ORCH'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Unit tests mock AgentPoolClient; production code has no Process.Start for coding agents.')
            (New-Ac 'ac-2' 'Start All iterates pool start endpoints already defined on AgentPoolClient.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-TODO-001'; title = 'Task bank uses TodoClient only'
        description = 'Task bank CRUD, lanes, assignment, and dependencies SHALL use MCP TODO APIs. Direct reads or writes of TODO.yaml / docs/todo.yaml are forbidden.'
        priority = 'critical'; area = 'WEB'; subarea = 'TODO'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Web project has no File.Write for TODO.yaml.')
            (New-Ac 'ac-2' 'Lane derivation is unit-tested against TODO status and tags.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-SESS-001'; title = 'Live peek uses session log and events'
        description = 'Live peek and fire-and-forget refresh SHALL use SessionLogClient and EventStreamClient (/mcpserver/events). Terminal scraping is forbidden.'
        priority = 'high'; area = 'WEB'; subarea = 'SESS'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Live peek tests subscribe to a fake event stream and append dialog items.')
            (New-Ac 'ac-2' 'No PTY or console-hook dependency exists in McpServer.Web.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-SEC-001'; title = 'Reuse pairing and OIDC'
        description = 'mcp-web SHALL authenticate with existing pairing (FR-MCP-014) and OIDC (FR-MCP-026). Do not add Prompter Hawk magic-link email auth.'
        priority = 'critical'; area = 'WEB'; subarea = 'SEC'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Unauthenticated cockpit requests redirect to the existing login/pairing flow.')
            (New-Ac 'ac-2' 'No magic-link mailer is added for mcp-web.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-SEC-002'; title = 'Allow/deny stored as workspace policy'
        description = 'Tool and path rules SHALL persist through workspace policy APIs (FR-MCP-033 / WorkspacePolicyService), fail closed, and be editable from mcp-web.'
        priority = 'high'; area = 'WEB'; subarea = 'SEC'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Saving rules calls the workspace policy API, not a new sidecar file format.')
            (New-Ac 'ac-2' 'Missing policy fails closed for denied tools/paths.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-SCHED-001'; title = 'Recurring TODOs have a durable scheduler'
        description = 'If MCP Server has no recurring TODO scheduler, add a server-side scheduler that creates/reopens TODOs. mcp-web only authors the schedule fields. Browser timers SHALL NOT be the source of truth.'
        priority = 'high'; area = 'WEB'; subarea = 'SCHED'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'A schedule fires while mcp-web is closed.')
            (New-Ac 'ac-2' 'Schedule definitions persist in MCP storage, not only in the browser.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-CTX-001'; title = 'Hierarchical context via templates and memories'
        description = 'Workspace/agent/TODO context SHALL compose from existing template and memory APIs where they fit. New fields are allowed only when those APIs cannot represent persona text.'
        priority = 'high'; area = 'WEB'; subarea = 'CTX'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Dispatch payload merge order is workspace, then agent, then TODO.')
            (New-Ac 'ac-2' 'Persona text survives agent recycle.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-OBS-001'; title = 'Burn and progress from session logs'
        description = 'Token burn and daily progress SHALL be aggregated from session-log tokenCount, commit actions, and agent-pool working timestamps. Do not scrape git independently unless session-log data is missing, and then record the fallback in the UI.'
        priority = 'high'; area = 'WEB'; subarea = 'OBS'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Burn series is computed from session-log tokenCount grouped by hour.')
            (New-Ac 'ac-2' 'Peak parallelism counts distinct agents in working status overlapping in time.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-GIT-001'; title = 'Commit linkage from session-log commit actions'
        description = 'TODO-to-commit mapping SHALL read session-log actions with type commit. mcp-web SHALL NOT parse git logs as the primary source.'
        priority = 'medium'; area = 'WEB'; subarea = 'GIT'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Cards bind to action type commit SHA fields.')
            (New-Ac 'ac-2' 'Absence of commit actions shows no SHA.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-API-001'; title = 'New REST only when existing APIs cannot meet AC'
        description = 'Prefer Todo, AgentPool, SessionLog, Events, Workspace, Templates, and Memory APIs. Any new endpoint MUST be documented with FR/TR ids and covered by TEST-WEB tests.'
        priority = 'high'; area = 'WEB'; subarea = 'API'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'A design note in the implementing PR lists reused endpoints versus new ones.')
            (New-Ac 'ac-2' 'Each new endpoint has at least one TEST-WEB requirement mapping.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-PRIV-001'; title = 'No prompt or file telemetry from mcp-web'
        description = 'The mcp-web client bundle SHALL NOT send prompt text, file bodies, or diffs to hosts other than the configured MCP Server and the operator-configured model providers used by the pool.'
        priority = 'critical'; area = 'WEB'; subarea = 'PRIV'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'A static or unit test fails if analytics keys or prompt-post URLs are introduced.')
            (New-Ac 'ac-2' 'Default config has telemetry disabled.')
        )
    }
    [ordered]@{
        kind = 'tr'; id = 'TR-WEB-BUDGET-001'; title = 'Budget enforcement at enqueue'
        description = 'Hourly, daily, and monthly USD caps SHALL be enforced server-side when dispatching. mcp-web only edits the caps. Client-side only checks are not sufficient.'
        priority = 'medium'; area = 'WEB'; subarea = 'BUDGET'; status = 'pending'; notes = $notes
        acceptanceCriteria = @(
            (New-Ac 'ac-1' 'Enqueue returns a structured error when a cap is exceeded.')
            (New-Ac 'ac-2' 'Bypassing mcp-web and calling the pool API still enforces the cap.')
        )
    }
)

$test = @(
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-001'; title = 'Cockpit render tests'; description = 'Unit and UI tests prove FR-WEB-001 cockpit composition and refresh from MCP APIs.'; priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'OrchestrationDashboardTests fail before implementation and pass after for fleet+bank+header.'), (New-Ac 'ac-2' 'Refresh test uses mocked clients, not live HTTP.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-002'; title = 'Workspace binding tests'; description = 'Tests prove FR-WEB-002 workspace-as-mission binding and isolation.'; priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Switching workspace changes TODO and pool queries'' workspace path.'), (New-Ac 'ac-2' 'No test creates a .prompter-hawk directory as a prerequisite.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-003'; title = 'Fleet lifecycle tests'; description = 'Tests prove FR-WEB-003 status mapping and start/stop/all against AgentPoolClient mocks.'; priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Each status enum maps to a visible state.'), (New-Ac 'ac-2' 'Start All invokes start per agent.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-004'; title = 'Task bank lane tests'; description = 'Tests prove FR-WEB-004 lane derivation and TodoClient usage.'; priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Each lane has a fixture TODO that only appears there.'), (New-Ac 'ac-2' 'Create path is asserted against TodoClient, not file IO.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-005'; title = 'Parallel dispatch tests'; description = 'Tests prove FR-WEB-005 fire-and-forget parallel pickup.'; priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Two idle agents receive two TODOs without a third prompt.'), (New-Ac 'ac-2' 'A simulated browser close does not call cancel.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-006'; title = 'Recurring TODO tests'; description = 'Tests prove FR-WEB-006 schedule persist and fire while UI is closed.'; priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Scheduler test advances fake time and creates a pending TODO.'), (New-Ac 'ac-2' 'Disable schedule test asserts no further creates.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-007'; title = 'Context merge tests'; description = 'Tests prove FR-WEB-007 workspace/agent/TODO merge order.'; priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Merge unit test documents order workspace, agent, TODO.'), (New-Ac 'ac-2' 'Agent override wins over workspace for the same key.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-008'; title = 'Retry with context tests'; description = 'Tests prove FR-WEB-008 one-click retry payload.'; priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Retry includes original description and context layers.'), (New-Ac 'ac-2' 'Retry is one UI action in the test harness.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-009'; title = 'Live peek stream tests'; description = 'Tests prove FR-WEB-009 session-log streaming.'; priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Fake SSE emits dialog items that appear in the peek panel.'), (New-Ac 'ac-2' 'Completed TODO history test does not require a live agent.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-010'; title = 'Burn and progress tests'; description = 'Tests prove FR-WEB-010 charts and daily metrics.'; priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Baseline comparison color is asserted for above and below rates.'), (New-Ac 'ac-2' 'Peak parallelism is computed from overlapping working intervals.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-011'; title = 'Feedback queue tests'; description = 'Tests prove FR-WEB-011 non-blocking feedback.'; priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Other agents remain in_progress while one waits for feedback.'), (New-Ac 'ac-2' 'Submit feedback calls TODO or session API, not a local-only flag.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-012'; title = 'Orchestrator tentative tests'; description = 'Tests prove FR-WEB-012 idle proposals stay tentative.'; priority = 'medium'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Idle + enabled orchestrator creates tentative TODO.'), (New-Ac 'ac-2' 'Tentative is not dispatched.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-013'; title = 'Permission UI tests'; description = 'Tests prove FR-WEB-013 allow/deny fail-closed.'; priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Deny .env is persisted via policy API mock.'), (New-Ac 'ac-2' 'Denied tool attempt is rejected in the test double.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-014'; title = 'Multi-provider tests'; description = 'Tests prove FR-WEB-014 per-agent provider/model.'; priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Two agents persist two providers.'), (New-Ac 'ac-2' 'No markup/proxy configuration is introduced.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-015'; title = 'Commit link tests'; description = 'Tests prove FR-WEB-015 SHA rendering from session-log commit actions.'; priority = 'medium'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Fixture commit action renders SHA.'), (New-Ac 'ac-2' 'Missing action renders no SHA.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-016'; title = 'Privacy tests'; description = 'Tests prove FR-WEB-016 no prompt/file telemetry.'; priority = 'critical'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Bundle/config scan fails on analytics prompt sinks.'), (New-Ac 'ac-2' 'Redaction test covers API key patterns already used in session logs.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-017'; title = 'Viewport tests'; description = 'Tests prove FR-WEB-017 desktop and mobile layout.'; priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' '1280px test asserts fleet and bank visible.'), (New-Ac 'ac-2' '390px test asserts primary controls reachable.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-018'; title = 'Dependency tests'; description = 'Tests prove FR-WEB-018 blocked lane and cycle rejection.'; priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Incomplete prereq places TODO in blocked.'), (New-Ac 'ac-2' 'Cycle create is rejected.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-019'; title = 'Persona persistence tests'; description = 'Tests prove FR-WEB-019 prompt survives recycle.'; priority = 'high'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Recycle after edit still returns new prompt.'), (New-Ac 'ac-2' 'Clear restores default.')) }
    [ordered]@{ kind = 'test'; id = 'TEST-WEB-020'; title = 'Budget tests'; description = 'Tests prove FR-WEB-020 server-side cap enforcement.'; priority = 'medium'; area = 'WEB'; status = 'pending'; notes = $notes; acceptanceCriteria = @((New-Ac 'ac-1' 'Exceeded hourly cap fails enqueue in API test, not only UI.'), (New-Ac 'ac-2' 'Clearing cap allows enqueue.')) }
)

$records = @()
$records += $fr
$records += $tr
$records += $test

$batchPath = Join-Path $failsafeDir ("{0}-requirements_create_batch-{1:x4}.yaml" -f $stamp, (Get-Random -Maximum 0xFFFF))
$batch = [ordered]@{
    method = 'workflow.requirements.createBatch'
    label = 'requirements_create_batch'
    timestamp = $stamp
    params = [ordered]@{
        records = $records
    }
}
Write-McpYamlObject -Path $batchPath -Document $batch

$trByFr = @{
    'FR-WEB-001' = @('TR-WEB-UI-001', 'TR-WEB-UI-002', 'TR-WEB-ORCH-001')
    'FR-WEB-002' = @('TR-WEB-UI-001', 'TR-WEB-TODO-001', 'TR-WEB-ORCH-001')
    'FR-WEB-003' = @('TR-WEB-ORCH-001', 'TR-WEB-UI-002')
    'FR-WEB-004' = @('TR-WEB-TODO-001', 'TR-WEB-UI-002')
    'FR-WEB-005' = @('TR-WEB-ORCH-001', 'TR-WEB-TODO-001', 'TR-WEB-SESS-001')
    'FR-WEB-006' = @('TR-WEB-SCHED-001', 'TR-WEB-TODO-001')
    'FR-WEB-007' = @('TR-WEB-CTX-001', 'TR-WEB-ORCH-001')
    'FR-WEB-008' = @('TR-WEB-TODO-001', 'TR-WEB-CTX-001', 'TR-WEB-SESS-001')
    'FR-WEB-009' = @('TR-WEB-SESS-001', 'TR-WEB-OBS-001')
    'FR-WEB-010' = @('TR-WEB-OBS-001', 'TR-WEB-GIT-001')
    'FR-WEB-011' = @('TR-WEB-TODO-001', 'TR-WEB-SESS-001')
    'FR-WEB-012' = @('TR-WEB-TODO-001', 'TR-WEB-ORCH-001')
    'FR-WEB-013' = @('TR-WEB-SEC-002', 'TR-WEB-API-001')
    'FR-WEB-014' = @('TR-WEB-ORCH-001', 'TR-WEB-API-001')
    'FR-WEB-015' = @('TR-WEB-GIT-001', 'TR-WEB-SESS-001')
    'FR-WEB-016' = @('TR-WEB-PRIV-001', 'TR-WEB-SEC-001')
    'FR-WEB-017' = @('TR-WEB-UI-002')
    'FR-WEB-018' = @('TR-WEB-TODO-001')
    'FR-WEB-019' = @('TR-WEB-CTX-001', 'TR-WEB-ORCH-001')
    'FR-WEB-020' = @('TR-WEB-BUDGET-001', 'TR-WEB-ORCH-001')
}

$mappingPaths = @()
$i = 0
foreach ($frId in ($trByFr.Keys | Sort-Object)) {
    $i++
    $testId = 'TEST-WEB-{0:D3}' -f [int]$frId.Substring($frId.Length - 3)
    $mapPath = Join-Path $failsafeDir ("{0}-requirements_create_mapping-{1:D2}-{2:x4}.yaml" -f $stamp, $i, (Get-Random -Maximum 0xFFFF))
    $map = [ordered]@{
        method = 'workflow.requirements.createMapping'
        label = 'requirements_create_mapping'
        timestamp = $stamp
        params = [ordered]@{
            frId = $frId
            trIds = @($trByFr[$frId])
            testIds = @($testId)
            notes = "mcp-web orchestration mapping for $frId. PLAN-WEB-ORCH-001. Replay when storage returns."
        }
    }
    Write-McpYamlObject -Path $mapPath -Document $map
    $mappingPaths += $mapPath
}

$cacheDir = Resolve-McpCacheDir -StartPath $workspace
$sessionFile = Join-Path $cacheDir 'session-state.yaml'
$turnFile = Join-Path $cacheDir 'current-turn.yaml'
$requestId = "req-$stamp-002-prompterhawk-mcpweb-reqs"
$sessionId = 'GrokCode-20260902T145530Z-start-new-session'

Update-McpYamlObject -Path $sessionFile -Create -Mutation {
    param($d)
    $d['lastUpdated'] = $iso
    $d['status'] = 'MCP_UNTRUSTED'
    $d['fallback'] = 'failsafe'
    if (-not $d.Contains('sessionId') -or [string]::IsNullOrWhiteSpace([string]$d['sessionId'])) {
        $d['sessionId'] = $sessionId
    }
}

$turn = [ordered]@{
    turnRequestId = $requestId
    sessionId = $sessionId
    status = 'in_progress'
    queryTitle = 'Prompter Hawk goals as mcp-web requirements'
    queryText = 'Analyze product at https://prompterhawk.dev/ and create a set of requirements for adding UI to accomplish same goals to mcp-web. Write as requirements with AC and store using the failsafe mechanism.'
    openedAt = $iso
    interpretation = 'Operator wants FR/TR/TEST with AC for an mcp-web orchestration UI covering Prompter Hawk goals, stored via failsafe because MCP storage is unreachable. No product implementation until PLAN-WEB-ORCH-001 approval.'
    tags = @('session-start', 'fallback', 'failsafe', 'MCP_UNTRUSTED', 'FR-WEB', 'PLAN-WEB-ORCH-001', 'class-1-requirements')
    contextList = @(
        'https://prompterhawk.dev/'
        'https://prompterhawk.dev/docs.html'
        'docs/plans/PLAN-WEB-ORCH-001.md'
        'docs/Project/Requirements-WebUI.md'
    )
    fallback = 'failsafe'
    filesModified = @(
        'docs/plans/PLAN-WEB-ORCH-001.md'
        $batchPath
    )
}
Write-McpYamlObject -Path $turnFile -Document $turn

$submitPath = Join-Path $failsafeDir ("{0}-session_submit-{1:x4}.yaml" -f $stamp, (Get-Random -Maximum 0xFFFF))
$submit = [ordered]@{
    method = 'client.SessionLog.SubmitAsync'
    label = 'session_submit'
    timestamp = $stamp
    params = [ordered]@{
        sessionLog = [ordered]@{
            sessionId = $sessionId
            sourceType = 'GrokCode'
            agent = 'GrokCode'
            model = 'grok-4.6'
            title = 'Start new session with MCP failsafe fallback'
            started = '2026-09-02T14:55:30Z'
            lastUpdated = $iso
            status = 'in_progress'
            turnCount = 2
            turns = @(
                [ordered]@{
                    timestamp = $iso
                    model = 'grok-4.6'
                    tokenCount = 0
                    requestId = $requestId
                    queryTitle = 'Prompter Hawk goals as mcp-web requirements'
                    queryText = 'Analyze product at https://prompterhawk.dev/ and create a set of requirements for adding UI to accomplish same goals to mcp-web. Write as requirements with AC and store using the failsafe mechanism.'
                    status = 'in_progress'
                    interpretation = 'Operator wants FR/TR/TEST with AC for mcp-web covering Prompter Hawk goals, stored via failsafe.'
                    tags = @('FR-WEB', 'PLAN-WEB-ORCH-001', 'failsafe')
                }
            )
        }
    }
}
Write-McpYamlObject -Path $submitPath -Document $submit

$webRefPath = Join-Path $failsafeDir ("{0}-session_actions-{1:x4}.yaml" -f $stamp, (Get-Random -Maximum 0xFFFF))
$webRef = [ordered]@{
    method = 'workflow.sessionlog.appendActions'
    label = 'session_actions'
    timestamp = $stamp
    params = [ordered]@{
        actions = @(
            [ordered]@{ order = 1; type = 'web_reference'; status = 'completed'; filePath = ''; description = 'https://prompterhawk.dev/ product homepage analyzed for mission-control goals.' }
            [ordered]@{ order = 2; type = 'web_reference'; status = 'completed'; filePath = ''; description = 'https://prompterhawk.dev/docs.html dashboard, agents, tasks, captain, live peek, recurring, permissions, CLI.' }
            [ordered]@{ order = 3; type = 'create'; status = 'completed'; filePath = 'docs/plans/PLAN-WEB-ORCH-001.md'; description = 'BDPv4 plan for mcp-web orchestration; wait for approval before implementation.' }
            [ordered]@{ order = 4; type = 'design_decision'; status = 'completed'; filePath = ''; description = 'Decision: map Prompter Hawk mission/agent/task onto MCP workspace/agent-pool/TODO; host UI in mcp-web; reuse REST/SSE; failsafe-store requirements while storage is down. Rejected: cloning PH Python app, magic-link auth, or a second task store.' }
        )
    }
}
Write-McpYamlObject -Path $webRefPath -Document $webRef

$receipt = [ordered]@{
    utc = $iso
    httpProbed = $false
    frCount = $fr.Count
    trCount = $tr.Count
    testCount = $test.Count
    mappingCount = $mappingPaths.Count
    batchPath = $batchPath
    mappingPaths = $mappingPaths
    submitPath = $submitPath
    actionsPath = $webRefPath
    planPath = 'docs/plans/PLAN-WEB-ORCH-001.md'
    requestId = $requestId
    sessionId = $sessionId
    frIds = @($fr | ForEach-Object { $_.id })
    trIds = @($tr | ForEach-Object { $_.id })
    testIds = @($test | ForEach-Object { $_.id })
}
$receiptPath = Join-Path $workspace 'docs\receipts\_web-orch-20260902\failsafe-write-receipt.json'
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $receiptPath -Encoding utf8
Write-Output ($receipt | ConvertTo-Json -Compress)
Write-Output ('BATCH_EXISTS=' + (Test-Path -LiteralPath $batchPath))
Write-Output ('MAPPING_COUNT=' + $mappingPaths.Count)
Write-Output ('FIRST_MAP=' + $mappingPaths[0])
