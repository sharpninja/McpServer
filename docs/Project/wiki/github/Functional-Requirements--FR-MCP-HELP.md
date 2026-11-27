# FR-MCP-HELP

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-HELP-001 Agent Help conversation API

Agents stuck on MCP Server surfaces can open help sessions, submit turns, query status, retrieve transcripts, and stream responses over HTTP.
Scope: layer-1+

## FR-MCP-HELP-002 Inbound guardrails

Inbound user messages are evaluated by deterministic guard rules before helper execution; violations terminate the session.
Scope: layer-1+

## FR-MCP-HELP-003 Transcript persistence

Help turns are captured as append-only JSONL under the workspace data root.
Scope: layer-1+

## FR-MCP-HELP-004 Guard incident logging

Blocked inbound messages produce durable incident records for operator review.
Scope: layer-1+

## FR-MCP-HELP-005 Corpus bootstrap and outcome analysis

Sessions optionally bootstrap a stub context pack; completed sessions can be analyzed for triage and documentation follow-ups.
Scope: layer-1+

## FR-MCP-HELP-006 MCP STDIO tool parity

STDIO MCP exposes agent_help_create_session, agent_help_submit_turn, and agent_help_get_status mirroring the HTTP controller.
Scope: layer-1+

## FR-MCP-HELP-007 Typed client library

SharpNinja.McpServer.Client exposes AgentHelpClient on McpServerClient for session create, turn submit, status, and transcript retrieval.
Scope: layer-1+

## FR-MCP-HELP-008 Marker prompt guidance

Generated markers instruct agents when and how to invoke Agent Help for MCP Server issues.
Scope: layer-1+

## FR-MCP-HELP-009 REPL workflow parity

The REPL exposes workflow.agenthelp typed wrappers with the same contracts as REST and MCP tools.
Scope: layer-1+

## FR-MCP-HELP-010 Agent Help configuration

AgentHelp options control enablement, guard behavior, storage paths, execution strategy, and helper model defaults.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] With no nonblank agentPath override, Agent Help supplies a strategy-appropriate executable: codex for codex-cli, copilot for copilot-cli, grok for grok-cli/grok-build, while generic one-shot execution retains cline.
- [ ] A nonblank caller agentPath is preserved exactly and takes precedence over the strategy default.
- [ ] Null, empty and whitespace overrides and case-insensitive strategy names are handled consistently; the codex-cli path must never silently resolve to cline solely because of the generic options default.

## FR-MCP-HELP-011 Agent Help grok-cli execution strategy and default

Agent Help SHALL provide a Grok CLI execution strategy named grok-cli (with grok-build accepted as an alias) and SHALL use grok-cli as the default execution strategy when a caller does not specify one.
Scope: layer-1+

