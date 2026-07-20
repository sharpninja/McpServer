/**
 * New coverage for runtime/host-context.ts + the createMcpServerPluginCore
 * factory. This replaces the cline-v2 plugin.test.ts (which exercised the
 * @cline/core AgentPlugin contract, manifest, and registerTool wiring -
 * host glue that stays in the consuming repo). Here we test the host-neutral
 * core directly: config-driven identity threading (agentName / pluginId),
 * dispatchTool routing across the tool families, the opt-in schema
 * validation gate, and the startSession / appendToolAction / completeSession
 * session-audit choreography.
 */
import { createMcpServerPluginCore, HostContext, allToolDescriptors, contextLogger } from '../src/index.js';
import type { ReplBridge, ReplResponse } from '../src/transport/repl-bridge.js';
import { __resetSessionShimForTests } from '../src/tools/session.js';

class FakeBridge {
  calls: Array<{ method: string; params?: Record<string, unknown> }> = [];
  closed = false;
  nextResponse: ReplResponse = { type: 'result', payload: { ok: true } };

  async invoke(method: string, params?: Record<string, unknown>): Promise<ReplResponse> {
    this.calls.push({ method, params });
    return this.nextResponse;
  }

  async close(): Promise<void> {
    this.closed = true;
  }
}

function asBridge(fake: FakeBridge): ReplBridge {
  return fake as unknown as ReplBridge;
}

function newCore(fake = new FakeBridge(), overrides: Record<string, unknown> = {}) {
  const context = createMcpServerPluginCore({
    agentName: 'Cline',
    pluginId: 'cline-v2',
    bridge: asBridge(fake),
    workspacePath: 'F:\\GitHub\\FeatureFlags',
    autoBootstrap: false,
    autoFlushCache: false,
    ...overrides,
  });
  return { context, fake };
}

const identityEnvKeys = [
  'PLUGIN_AGENT_NAME',
  'PLUGIN_AGENT_DEFAULT',
  'PLUGIN_TAG',
  'MCP_AGENT_NAME',
  'MCP_AGENT_ID',
  'MCP_SESSION_AGENT',
  'MCP_SESSION_MODEL',
  'CT2R_SOURCE_TYPE',
  'CT2R_MODEL',
  'CT2R_TAGS',
] as const;

function snapshotIdentityEnv(): Record<string, string | undefined> {
  return Object.fromEntries(identityEnvKeys.map((key) => [key, process.env[key]]));
}

function restoreIdentityEnv(snapshot: Record<string, string | undefined>): void {
  for (const key of identityEnvKeys) {
    if (snapshot[key] === undefined) {
      delete process.env[key];
    } else {
      process.env[key] = snapshot[key];
    }
  }
}

beforeEach(() => {
  __resetSessionShimForTests();
});

describe('createMcpServerPluginCore', () => {
  test('returns a HostContext that reflects the configured identity', () => {
    const { context } = newCore();
    expect(context).toBeInstanceOf(HostContext);
    expect(context.agentName).toBe('Cline');
    expect(context.pluginId).toBe('cline-v2');
    expect(context.workspacePath).toBe('F:\\GitHub\\FeatureFlags');
  });

  test('threads a different agentName / pluginId through the same factory', () => {
    const { context } = newCore(new FakeBridge(), { agentName: 'OpenCode', pluginId: 'opencode' });
    expect(context.agentName).toBe('OpenCode');
    expect(context.pluginId).toBe('opencode');
  });

  test('TEST-MCP-PLUGIN-PSONLY-001 forces configured Node host identity over inherited environment', () => {
    const snapshot = snapshotIdentityEnv();
    try {
      for (const key of identityEnvKeys) {
        process.env[key] = 'WrongAgent';
      }

      const { context } = newCore(new FakeBridge(), { agentName: 'OpenCode', pluginId: 'opencode' });

      expect(context.agentName).toBe('OpenCode');
      expect(context.pluginId).toBe('opencode');
      expect(process.env.PLUGIN_AGENT_NAME).toBe('OpenCode');
      expect(process.env.PLUGIN_AGENT_DEFAULT).toBe('OpenCode');
      expect(process.env.MCP_AGENT_NAME).toBe('OpenCode');
      expect(process.env.MCP_AGENT_ID).toBe('OpenCode');
      expect(process.env.MCP_SESSION_AGENT).toBe('OpenCode');
      expect(process.env.CT2R_SOURCE_TYPE).toBe('OpenCode');
      expect(process.env.PLUGIN_TAG).toBe('opencode');
      expect(process.env.MCP_SESSION_MODEL).toBe('opencode');
      expect(process.env.CT2R_MODEL).toBe('opencode');
      expect(process.env.CT2R_TAGS).toBe('opencode');
    } finally {
      restoreIdentityEnv(snapshot);
    }
  });

  test('exposes the full tool descriptor catalog spanning every family', () => {
    const names = allToolDescriptors.map((tool) => tool.name);
    expect(names).toEqual(expect.arrayContaining([
      'workspace_ensure',
      'todo_query',
      'todo_internal_status',
      'session_query_history',
      'memory_list',
      'req_generate_document',
      'graphrag_query',
    ]));
    // No duplicate tool names across the merged families.
    expect(new Set(names).size).toBe(names.length);
  });
});

describe('HostContext.dispatchTool', () => {
  test('routes a todo tool through the retained workflow method and returns plain JSON', async () => {
    const fake = new FakeBridge();
    fake.nextResponse = { type: 'result', payload: { result: { items: [], totalCount: 0 } } };
    const { context } = newCore(fake);

    const result = await context.dispatchTool('todo_query', { id: 'MCP-TODO-001' });

    expect(result).toEqual({ result: { items: [], totalCount: 0 } });
    expect(result).not.toHaveProperty('content');
    expect(fake.calls).toEqual([{ method: 'workflow.todo.query', params: { id: 'MCP-TODO-001' } }]);
  });

  test('routes a session tool through the session shim (no workflow.sessionlog.* leak)', async () => {
    const { context, fake } = newCore();
    await context.dispatchTool('session_open', {
      agent: 'Cline',
      sessionId: 'Cline-x-001',
      title: 'demo',
    });
    // session_open is local-only: no bridge traffic.
    expect(fake.calls).toHaveLength(0);
  });

  test('routes a requirements tool through workflow.requirements.*', async () => {
    const fake = new FakeBridge();
    fake.nextResponse = { type: 'result', payload: { result: { items: [] } } };
    const { context } = newCore(fake);

    await context.dispatchTool('req_generate_document', { format: 'wiki', docType: 'all' });

    expect(fake.calls[0]).toEqual({
      method: 'workflow.requirements.generateDocument',
      params: { format: 'wiki', docType: 'all' },
    });
  });

  test('throws on an unknown tool name', async () => {
    const { context } = newCore();
    await expect(context.dispatchTool('not_a_tool', {})).rejects.toThrow(/Unknown tool: not_a_tool/);
  });

  test('opt-in validateArguments rejects schema-invalid input before dispatch', async () => {
    const fake = new FakeBridge();
    const { context } = newCore(fake, { validateArguments: true });

    // session_open requires agent/sessionId/title; omitting them must fail
    // the pre-dispatch validator and never reach the bridge.
    await expect(context.dispatchTool('session_open', {})).rejects.toThrow(/schema_validation_failed/);
    expect(fake.calls).toHaveLength(0);
  });

  test('validateArguments allows well-formed input through', async () => {
    const fake = new FakeBridge();
    const { context } = newCore(fake, { validateArguments: true });
    await expect(
      context.dispatchTool('session_open', { agent: 'Cline', sessionId: 'Cline-x-1', title: 't' }),
    ).resolves.toBeDefined();
  });
});

describe('HostContext session choreography', () => {
  test('startSession runs bootstrap/open/begin_turn locally (no bridge traffic)', async () => {
    const { context, fake } = newCore();
    await context.startSession({ prompt: 'Implement the core suite', modelId: 'test-model' });
    // bootstrap + open + begin_turn are all local shim mutations.
    expect(fake.calls).toHaveLength(0);
  });

  test('appendToolAction upserts the active turn with an action + dialog item', async () => {
    const { context, fake } = newCore();
    await context.startSession({ prompt: 'do work' });

    await context.appendToolAction(
      { toolCall: { name: 'todo_query', input: { done: false } } },
      'completed',
    );

    // append_actions and append_dialog each upsert the turn through the
    // real client.SessionLog.UpsertTurnAsync route.
    const upserts = fake.calls.filter((c) => c.method === 'client.SessionLog.UpsertTurnAsync');
    expect(upserts.length).toBeGreaterThanOrEqual(2);
    const withAction = upserts.find((c) => {
      const turn = (c.params as { turn?: { actions?: unknown[] } }).turn;
      return Array.isArray(turn?.actions) && turn!.actions!.length > 0;
    });
    expect(withAction).toBeDefined();
  });

  test('completeSession completes the turn and submits the session on success', async () => {
    const { context, fake } = newCore();
    await context.startSession({ prompt: 'do work' });

    await context.completeSession({ result: { output: 'all done' } });

    const methods = fake.calls.map((c) => c.method);
    // The completed turn is upserted, then the whole session is submitted.
    expect(methods).toContain('client.SessionLog.UpsertTurnAsync');
    expect(methods).toContain('client.SessionLog.SubmitAsync');

    const submit = fake.calls.find((c) => c.method === 'client.SessionLog.SubmitAsync')!;
    const payload = submit.params as { sessionLog: { status: string } };
    expect(payload.sessionLog.status).toBe('completed');
  });

  test('completeSession fails the turn and closes the session as failed on error', async () => {
    const { context, fake } = newCore();
    await context.startSession({ prompt: 'do work' });

    await context.completeSession({ error: 'boom' });

    const submit = fake.calls.find((c) => c.method === 'client.SessionLog.SubmitAsync')!;
    const payload = submit.params as { sessionLog: { status: string; turns: Array<{ status: string; errorCode?: string }> } };
    expect(payload.sessionLog.status).toBe('failed');
    const failedTurn = payload.sessionLog.turns.find((t) => t.status === 'failed');
    expect(failedTurn).toBeDefined();
    // Error code is derived from the configured pluginId.
    expect(failedTurn!.errorCode).toBe('cline_v2_run_failed');
  });

  test('completeSession is a no-op when no session was started', async () => {
    const { context, fake } = newCore();
    await context.completeSession({ result: { output: 'x' } });
    expect(fake.calls).toHaveLength(0);
  });

  test('contextLogger returns an empty object (never undefined) when no logger is supplied', () => {
    // Regression: best-effort paths do contextLogger(ctx).warn?.(...); a bare
    // undefined return crashed on the .warn access (opencode bootstrap/flush).
    expect(contextLogger(undefined)).toEqual({});
    expect(contextLogger({})).toEqual({});
    expect(() => contextLogger({}).warn?.('x')).not.toThrow();
    const real = { logger: { warn: () => undefined, error: () => undefined } };
    expect(contextLogger(real)).toBe(real.logger);
  });
});
