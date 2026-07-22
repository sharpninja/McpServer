import type { ReplBridge, ReplResponse } from '../transport/repl-bridge.js';
import { cacheDelete, cacheWrite } from '../cache/cache-manager.js';

/**
 * Shim for the workflow.sessionlog.* MCP tool family.
 *
 * Original bug: every workflow.sessionlog.* method routed straight through
 * ReplBridge.invoke() to mcpserver-repl, which only knows the generic
 * client.<Name>.<MethodName> shape. Server replied with method_not_found
 * and handleSessionTool threw — every session tool call failed.
 *
 * This shim mirrors what the bash plugins do (see lib/repl-invoke.sh in
 * mcpserver-marketplace): keep the conceptual session/turn state in
 * process memory, then upsert the active turn through the real
 * client.SessionLog.UpsertTurnAsync route. Session-level close still posts
 * the full UnifiedSessionLogDto through client.SessionLog.SubmitAsync, and
 * session_query_history maps to client.SessionLog.QueryAsync.
 *
 * Lifecycle methods that only manipulate local state (bootstrap, open,
 * begin_turn) return a synthetic success without touching the server, so
 * an offline server doesn't block the agent from starting work.
 */

export interface DialogItem {
  timestamp: string;
  role: 'model' | 'tool' | 'system' | 'user';
  content: string;
  category: 'reasoning' | 'tool_call' | 'tool_result' | 'observation' | 'decision';
}

export interface ActionItem {
  order: number;
  description: string;
  type:
    | 'edit'
    | 'create'
    | 'delete'
    | 'design_decision'
    | 'commit'
    | 'pr_comment'
    | 'issue_comment'
    | 'web_reference'
    | 'dependency_add';
  status: 'completed' | 'pending';
  filePath?: string;
}

export interface TurnState {
  requestId: string;
  queryTitle: string;
  queryText: string;
  status: 'in_progress' | 'completed' | 'failed';
  response?: string;
  interpretation?: string;
  tokenCount?: number;
  tags?: string[];
  contextList?: string[];
  actions: ActionItem[];
  dialogItems: DialogItem[];
  errorMessage?: string;
  errorCode?: string;
}

export interface SessionState {
  sourceType: string;
  sessionId: string;
  title: string;
  model?: string;
  agentSessionId?: string;
  agentSessionTranscriptFile?: string;
  agentExecutablePath?: string;
  agentExecutableVersion?: string;
  status: 'in_progress' | 'completed' | 'failed';
  currentTurn?: TurnState;
  turns: TurnState[];
}

export class SessionShim {
  private state: SessionState | null = null;

  getState(): SessionState | null {
    return this.state;
  }

  /** Drop in-memory state. Used by tests; not part of the tool surface. */
  reset(): void {
    this.state = null;
  }

  bootstrap(): void {
    /* no-op: nothing to initialize on the client side */
  }

  open(args: {
    agent: string;
    sessionId: string;
    title: string;
    model?: string;
    agentSessionId?: string;
    agentSessionTranscriptFile?: string;
    agentExecutablePath?: string;
    agentExecutableVersion?: string;
  }): void {
    this.state = {
      sourceType: args.agent,
      sessionId: args.sessionId,
      title: args.title,
      model: args.model,
      agentSessionId: args.agentSessionId,
      agentSessionTranscriptFile: args.agentSessionTranscriptFile,
      agentExecutablePath: args.agentExecutablePath,
      agentExecutableVersion: args.agentExecutableVersion,
      status: 'in_progress',
      turns: [],
    };
  }

  beginTurn(args: { requestId: string; queryTitle: string; queryText: string }): void {
    this.requireSession('begin_turn');
    this.state!.currentTurn = {
      requestId: args.requestId,
      queryTitle: args.queryTitle,
      queryText: args.queryText,
      status: 'in_progress',
      actions: [],
      dialogItems: [],
    };
  }

  updateTurn(args: {
    response?: string;
    interpretation?: string;
    tokenCount?: number;
    tags?: string[];
    contextList?: string[];
  }): void {
    const turn = this.requireCurrentTurn('update_turn');
    if (args.response !== undefined) turn.response = args.response;
    if (args.interpretation !== undefined) turn.interpretation = args.interpretation;
    if (args.tokenCount !== undefined) turn.tokenCount = args.tokenCount;
    if (args.tags !== undefined) turn.tags = args.tags;
    if (args.contextList !== undefined) turn.contextList = args.contextList;
  }

  appendDialog(args: { dialogItems: DialogItem[] }): void {
    const turn = this.requireCurrentTurn('append_dialog');
    turn.dialogItems.push(...args.dialogItems);
  }

  appendActions(args: { actions: ActionItem[] }): void {
    const turn = this.requireCurrentTurn('append_actions');
    turn.actions.push(...args.actions);
  }

  completeTurn(args: { response?: string }): void {
    const turn = this.requireCurrentTurn('complete_turn');
    turn.status = 'completed';
    if (args.response !== undefined) turn.response = args.response;
    this.state!.turns.push(turn);
    this.state!.currentTurn = undefined;
  }

  failTurn(args: { errorMessage: string; errorCode?: string }): void {
    const turn = this.requireCurrentTurn('fail_turn');
    turn.status = 'failed';
    turn.errorMessage = args.errorMessage;
    if (args.errorCode !== undefined) turn.errorCode = args.errorCode;
    this.state!.turns.push(turn);
    this.state!.currentTurn = undefined;
  }

  close(args: { agent: string; sessionId: string; status?: 'completed' | 'failed' }): void {
    if (!this.state) {
      // Allow close on un-opened session (idempotent).
      this.state = {
        sourceType: args.agent,
        sessionId: args.sessionId,
        title: args.sessionId,
        status: args.status ?? 'completed',
        turns: [],
      };
      return;
    }
    this.state.status = args.status ?? 'completed';
  }

  /** Build the UnifiedSessionLogDto envelope the server expects. */
  buildSubmitPayload(): Record<string, unknown> {
    if (!this.state) {
      throw new Error('No active session — call session_open first.');
    }
    const allTurns = this.state.currentTurn
      ? [...this.state.turns, this.state.currentTurn]
      : [...this.state.turns];

    return {
      sessionLog: {
        sourceType: this.state.sourceType,
        sessionId: this.state.sessionId,
        title: this.state.title,
        ...(this.state.model ? { model: this.state.model } : {}),
        ...(this.state.agentSessionId ? { agentSessionId: this.state.agentSessionId } : {}),
        ...(this.state.agentSessionTranscriptFile
          ? { agentSessionTranscriptFile: this.state.agentSessionTranscriptFile }
          : {}),
        ...(this.state.agentExecutablePath ? { agentExecutablePath: this.state.agentExecutablePath } : {}),
        ...(this.state.agentExecutableVersion
          ? { agentExecutableVersion: this.state.agentExecutableVersion }
          : {}),
        status: this.state.status,
        turns: allTurns.map(this.serializeTurn),
      },
    };
  }

  /** Build the single-turn payload used by client.SessionLog.UpsertTurnAsync. */
  buildTurnPayload(): Record<string, unknown> {
    if (!this.state) {
      throw new Error('No active session — call session_open first.');
    }

    const turn = this.state.currentTurn ?? this.state.turns[this.state.turns.length - 1];
    if (!turn) {
      throw new Error('No active or completed turn — call session_begin_turn first.');
    }

    return {
      agent: this.state.sourceType,
      sessionId: this.state.sessionId,
      turn: this.serializeTurn(turn),
    };
  }

  private serializeTurn(turn: TurnState): Record<string, unknown> {
    const out: Record<string, unknown> = {
      requestId: turn.requestId,
      queryTitle: turn.queryTitle,
      queryText: turn.queryText,
      status: turn.status,
    };
    if (turn.response !== undefined) out.response = turn.response;
    if (turn.interpretation !== undefined) out.interpretation = turn.interpretation;
    if (turn.tokenCount !== undefined) out.tokenCount = turn.tokenCount;
    if (turn.tags !== undefined) out.tags = turn.tags;
    if (turn.contextList !== undefined) out.contextList = turn.contextList;
    if (turn.actions.length > 0) out.actions = turn.actions;
    if (turn.dialogItems.length > 0) out.processingDialog = turn.dialogItems;
    if (turn.errorMessage !== undefined) out.errorMessage = turn.errorMessage;
    if (turn.errorCode !== undefined) out.errorCode = turn.errorCode;
    return out;
  }

  private requireSession(op: string): void {
    if (!this.state) {
      throw new Error(`session_${op}: no active session — call session_open first.`);
    }
  }

  private requireCurrentTurn(op: string): TurnState {
    this.requireSession(op);
    if (!this.state!.currentTurn) {
      throw new Error(`session_${op}: no active turn — call session_begin_turn first.`);
    }
    return this.state!.currentTurn;
  }
}

/** Synthetic success envelope returned by no-server-call shims. */
export function syntheticOk(detail: string): ReplResponse {
  return {
    type: 'result',
    payload: { ok: true, detail },
  };
}

async function querySessionHistoryHttpFallback(
  args: Record<string, unknown>,
): Promise<ReplResponse | null> {
  const fetchFn = globalThis.fetch;
  const apiKey = process.env.MCPSERVER_API_KEY;
  const workspacePath = process.env.MCPSERVER_WORKSPACE_PATH ?? process.env.MCP_WORKSPACE_PATH;
  const baseUrl = process.env.MCPSERVER_BASE_URL ?? process.env.MCP_SERVER_URL;
  if (typeof fetchFn !== 'function' || !apiKey || !workspacePath || !baseUrl) return null;

  const url = new URL(`${baseUrl.replace(/\/$/, '')}/mcpserver/sessionlog`);
  for (const key of ['agent', 'model', 'text', 'from', 'to', 'limit', 'offset']) {
    const value = args[key];
    if (value !== undefined && value !== null && String(value).length > 0) {
      url.searchParams.set(key, String(value));
    }
  }

  const response = await fetchFn(url, {
    headers: {
      'X-Api-Key': apiKey,
      'X-Workspace-Path': workspacePath,
    },
  });

  const body = await response.text().catch(() => '');
  if (!response.ok) {
    return {
      type: 'error',
      payload: {
        code: 'http_error',
        message: `session log query HTTP fallback returned HTTP ${response.status}${body ? `: ${body}` : ''}`,
      },
    };
  }

  const contentType = response.headers.get('content-type')?.split(';')[0] || 'application/json';
  let result: unknown = body;
  if (/json/i.test(contentType) && body) {
    try {
      result = JSON.parse(body);
    } catch {
      result = body;
    }
  }

  return {
    type: 'result',
    payload: {
      result,
      contentType,
    },
  };
}

async function submitSessionWithFailsafe(
  shim: SessionShim,
  bridge: ReplBridge,
): Promise<ReplResponse> {
  const payload = shim.buildSubmitPayload();
  const failsafePath = await cacheWrite('client.SessionLog.SubmitAsync', payload);

  try {
    const response = await bridge.invoke('client.SessionLog.SubmitAsync', payload);
    if (response.type !== 'error') {
      await cacheDelete(failsafePath);
      return response;
    }

    const errorPayload = response.payload as { message?: string; code?: string };
    return {
      type: 'error',
      payload: {
        ...errorPayload,
        message: `${errorPayload.message ?? 'Unknown error'} Local failsafe saved: ${failsafePath}`,
        failsafePath,
      },
    };
  } catch (error) {
    return {
      type: 'error',
      payload: {
        code: 'invoke_failed',
        message: `${error instanceof Error ? error.message : String(error)} Local failsafe saved: ${failsafePath}`,
        failsafePath,
      },
    };
  }
}

async function submitTurnWithFailsafe(
  shim: SessionShim,
  bridge: ReplBridge,
): Promise<ReplResponse> {
  const payload = shim.buildTurnPayload();
  const failsafePath = await cacheWrite('client.SessionLog.UpsertTurnAsync', payload);

  try {
    const response = await bridge.invoke('client.SessionLog.UpsertTurnAsync', payload);
    if (response.type !== 'error') {
      await cacheDelete(failsafePath);
      return response;
    }

    const errorPayload = response.payload as { message?: string; code?: string };
    return {
      type: 'error',
      payload: {
        ...errorPayload,
        message: `${errorPayload.message ?? 'Unknown error'} Local failsafe saved: ${failsafePath}`,
        failsafePath,
      },
    };
  } catch (error) {
    return {
      type: 'error',
      payload: {
        code: 'invoke_failed',
        message: `${error instanceof Error ? error.message : String(error)} Local failsafe saved: ${failsafePath}`,
        failsafePath,
      },
    };
  }
}

/**
 * Top-level dispatcher. Maps a session_* tool name to the right
 * combination of in-memory mutation + server call. Returns the response
 * the MCP tool should surface.
 */
export async function dispatchSessionTool(
  shim: SessionShim,
  bridge: ReplBridge,
  toolName: string,
  args: Record<string, unknown>,
): Promise<ReplResponse> {
  switch (toolName) {
    case 'session_bootstrap':
      shim.bootstrap();
      return syntheticOk('bootstrap');

    case 'session_open':
      shim.open(args as Parameters<SessionShim['open']>[0]);
      return syntheticOk('session opened');

    case 'session_begin_turn':
      shim.beginTurn(args as Parameters<SessionShim['beginTurn']>[0]);
      return syntheticOk('turn started');

    case 'session_update_turn':
      shim.updateTurn(args as Parameters<SessionShim['updateTurn']>[0]);
      return submitTurnWithFailsafe(shim, bridge);

    case 'session_append_dialog':
      shim.appendDialog(args as Parameters<SessionShim['appendDialog']>[0]);
      return submitTurnWithFailsafe(shim, bridge);

    case 'session_append_actions':
      shim.appendActions(args as Parameters<SessionShim['appendActions']>[0]);
      return submitTurnWithFailsafe(shim, bridge);

    case 'session_complete_turn':
      shim.completeTurn(args as Parameters<SessionShim['completeTurn']>[0]);
      return submitTurnWithFailsafe(shim, bridge);

    case 'session_fail_turn':
      shim.failTurn(args as Parameters<SessionShim['failTurn']>[0]);
      return submitTurnWithFailsafe(shim, bridge);

    case 'session_query_history':
      return (await querySessionHistoryHttpFallback(args)) ?? bridge.invoke('client.SessionLog.QueryAsync', args);

    case 'session_close':
      shim.close(args as Parameters<SessionShim['close']>[0]);
      return submitSessionWithFailsafe(shim, bridge);

    default:
      throw new Error(`Unknown session tool: ${toolName}`);
  }
}
