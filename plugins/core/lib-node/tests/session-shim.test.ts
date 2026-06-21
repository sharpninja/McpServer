/**
 * Ported from mcpserver-cline-v2-plugin/tests/session-shim.test.ts.
 *
 * Regression suite for the workflow.sessionlog.* shim in
 * src/tools/session-shim.ts. The module layout is identical between the
 * cline-v2 plugin and this canonical core package, so the imports port
 * unchanged. The shim itself is host-neutral (no agentName hardcoding), so
 * the specs carry over verbatim.
 */

import { SessionShim, dispatchSessionTool, syntheticOk } from '../src/tools/session-shim.js';
import type { ReplBridge, ReplResponse } from '../src/transport/repl-bridge.js';
import { cacheFlush } from '../src/cache/cache-manager.js';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

/** Minimal ReplBridge stub that records every invoke() call. */
class FakeBridge {
  calls: Array<{ method: string; params?: Record<string, unknown> }> = [];
  nextResponse: ReplResponse = { type: 'result', payload: { ok: true } };

  async invoke(method: string, params?: Record<string, unknown>): Promise<ReplResponse> {
    this.calls.push({ method, params });
    return this.nextResponse;
  }
}

function asBridge(fake: FakeBridge): ReplBridge {
  return fake as unknown as ReplBridge;
}

function freshShim(): SessionShim {
  return new SessionShim();
}

describe('SessionShim', () => {
  describe('local lifecycle (no server call)', () => {
    test('bootstrap is a no-op', () => {
      const shim = freshShim();
      shim.bootstrap();
      expect(shim.getState()).toBeNull();
    });

    test('open initializes state', () => {
      const shim = freshShim();
      shim.open({ agent: 'Cline', sessionId: 'Cline-x-001', title: 'demo' });
      expect(shim.getState()).toEqual({
        sourceType: 'Cline',
        sessionId: 'Cline-x-001',
        title: 'demo',
        model: undefined,
        status: 'in_progress',
        turns: [],
      });
    });

    test('beginTurn sets currentTurn', () => {
      const shim = freshShim();
      shim.open({ agent: 'Cline', sessionId: 'Cline-x-001', title: 'demo' });
      shim.beginTurn({ requestId: 'req-001', queryTitle: 'do x', queryText: 'please do x' });
      expect(shim.getState()!.currentTurn).toMatchObject({
        requestId: 'req-001',
        queryTitle: 'do x',
        queryText: 'please do x',
        status: 'in_progress',
        actions: [],
        dialogItems: [],
      });
    });

    test('beginTurn without open throws', () => {
      const shim = freshShim();
      expect(() => shim.beginTurn({ requestId: 'r', queryTitle: 't', queryText: 'x' })).toThrow(
        /no active session/,
      );
    });
  });

  describe('mutation operations', () => {
    function primed(): SessionShim {
      const shim = freshShim();
      shim.open({ agent: 'Cline', sessionId: 'Cline-x-001', title: 'demo' });
      shim.beginTurn({ requestId: 'req-001', queryTitle: 't', queryText: 'q' });
      return shim;
    }

    test('updateTurn mutates only provided fields', () => {
      const shim = primed();
      shim.updateTurn({ response: 'done', tags: ['a', 'b'] });
      const turn = shim.getState()!.currentTurn!;
      expect(turn.response).toBe('done');
      expect(turn.tags).toEqual(['a', 'b']);
      expect(turn.interpretation).toBeUndefined();
    });

    test('appendDialog accumulates dialog items across calls', () => {
      const shim = primed();
      shim.appendDialog({
        dialogItems: [
          { timestamp: '2026-04-19T00:00:00Z', role: 'model', content: 'thinking', category: 'reasoning' },
        ],
      });
      shim.appendDialog({
        dialogItems: [
          { timestamp: '2026-04-19T00:00:01Z', role: 'tool', content: 'ran X', category: 'tool_call' },
        ],
      });
      expect(shim.getState()!.currentTurn!.dialogItems).toHaveLength(2);
    });

    test('appendActions accumulates action entries', () => {
      const shim = primed();
      shim.appendActions({
        actions: [
          { order: 1, description: 'edit a', type: 'edit', status: 'completed', filePath: 'a.ts' },
        ],
      });
      shim.appendActions({
        actions: [
          { order: 2, description: 'edit b', type: 'edit', status: 'completed', filePath: 'b.ts' },
        ],
      });
      expect(shim.getState()!.currentTurn!.actions).toHaveLength(2);
    });

    test('completeTurn marks turn completed and pops it from currentTurn', () => {
      const shim = primed();
      shim.completeTurn({ response: 'all done' });
      expect(shim.getState()!.currentTurn).toBeUndefined();
      expect(shim.getState()!.turns).toHaveLength(1);
      expect(shim.getState()!.turns[0]).toMatchObject({ status: 'completed', response: 'all done' });
    });

    test('failTurn marks turn failed and records error fields', () => {
      const shim = primed();
      shim.failTurn({ errorMessage: 'boom', errorCode: 'oops' });
      expect(shim.getState()!.turns[0]).toMatchObject({
        status: 'failed',
        errorMessage: 'boom',
        errorCode: 'oops',
      });
    });

    test('close on un-opened session is idempotent', () => {
      const shim = freshShim();
      shim.close({ agent: 'Cline', sessionId: 'Cline-x-002' });
      expect(shim.getState()!.status).toBe('completed');
    });
  });

  describe('buildSubmitPayload', () => {
    test('throws when no session is open', () => {
      const shim = freshShim();
      expect(() => shim.buildSubmitPayload()).toThrow(/No active session/);
    });

    test('emits sessionLog envelope with all completed + in-progress turns', () => {
      const shim = freshShim();
      shim.open({ agent: 'Cline', sessionId: 'Cline-x-001', title: 'demo', model: 'claude-opus' });
      shim.beginTurn({ requestId: 'req-1', queryTitle: 't1', queryText: 'q1' });
      shim.completeTurn({ response: 'r1' });
      shim.beginTurn({ requestId: 'req-2', queryTitle: 't2', queryText: 'q2' });
      shim.appendActions({
        actions: [
          { order: 1, description: 'edit', type: 'edit', status: 'completed', filePath: 'x.ts' },
        ],
      });

      const payload = shim.buildSubmitPayload() as { sessionLog: Record<string, unknown> };
      expect(payload.sessionLog).toMatchObject({
        sourceType: 'Cline',
        sessionId: 'Cline-x-001',
        title: 'demo',
        model: 'claude-opus',
        status: 'in_progress',
      });
      const turns = payload.sessionLog.turns as Array<Record<string, unknown>>;
      expect(turns).toHaveLength(2);
      expect(turns[0]).toMatchObject({ requestId: 'req-1', status: 'completed', response: 'r1' });
      expect(turns[1]).toMatchObject({ requestId: 'req-2', status: 'in_progress' });
      expect(turns[1].actions).toHaveLength(1);
    });

    test('omits empty optional fields from serialized turns', () => {
      const shim = freshShim();
      shim.open({ agent: 'Cline', sessionId: 'Cline-x-001', title: 'demo' });
      shim.beginTurn({ requestId: 'req-1', queryTitle: 't', queryText: 'q' });

      const payload = shim.buildSubmitPayload() as { sessionLog: { turns: Record<string, unknown>[] } };
      const turn = payload.sessionLog.turns[0];
      expect(turn).not.toHaveProperty('actions');
      expect(turn).not.toHaveProperty('dialogItems');
      expect(turn).not.toHaveProperty('errorMessage');
      expect(turn).not.toHaveProperty('tags');
    });
  });
});

describe('dispatchSessionTool', () => {
  function setup() {
    const shim = freshShim();
    const fake = new FakeBridge();
    return { shim, fake, bridge: asBridge(fake) };
  }

  test('session_bootstrap returns synthetic ok without server call', async () => {
    const { shim, fake, bridge } = setup();
    const r = await dispatchSessionTool(shim, bridge, 'session_bootstrap', {});
    expect(r.type).toBe('result');
    expect(fake.calls).toHaveLength(0);
  });

  test('session_open and session_begin_turn make no server call', async () => {
    const { shim, fake, bridge } = setup();
    await dispatchSessionTool(shim, bridge, 'session_open', {
      agent: 'Cline',
      sessionId: 'Cline-x-001',
      title: 'demo',
    });
    await dispatchSessionTool(shim, bridge, 'session_begin_turn', {
      requestId: 'req-1',
      queryTitle: 't',
      queryText: 'q',
    });
    expect(fake.calls).toHaveLength(0);
    expect(shim.getState()!.currentTurn!.requestId).toBe('req-1');
  });

  test('session_complete_turn invokes UpsertTurnAsync with the completed turn', async () => {
    const { shim, fake, bridge } = setup();
    await dispatchSessionTool(shim, bridge, 'session_open', {
      agent: 'Cline',
      sessionId: 'Cline-x-001',
      title: 'demo',
    });
    await dispatchSessionTool(shim, bridge, 'session_begin_turn', {
      requestId: 'req-1',
      queryTitle: 't',
      queryText: 'q',
    });
    await dispatchSessionTool(shim, bridge, 'session_complete_turn', { response: 'done' });

    expect(fake.calls).toHaveLength(1);
    expect(fake.calls[0].method).toBe('client.SessionLog.UpsertTurnAsync');
    const payload = fake.calls[0].params as { agent: string; sessionId: string; turn: Record<string, unknown> };
    expect(payload).toMatchObject({ agent: 'Cline', sessionId: 'Cline-x-001' });
    expect(payload.turn).toMatchObject({
      requestId: 'req-1',
      status: 'completed',
      response: 'done',
    });
  });

  test('session_update_turn keeps local failsafe when UpsertTurnAsync returns error', async () => {
    const { shim, fake, bridge } = setup();
    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-session-failsafe-'));
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;
    fake.nextResponse = {
      type: 'error',
      payload: { code: 'offline', message: 'server unavailable' },
    };

    try {
      await dispatchSessionTool(shim, bridge, 'session_open', {
        agent: 'Cline',
        sessionId: 'Cline-x-001',
        title: 'demo',
      });
      await dispatchSessionTool(shim, bridge, 'session_begin_turn', {
        requestId: 'req-1',
        queryTitle: 't',
        queryText: 'q',
      });

      const response = await dispatchSessionTool(shim, bridge, 'session_update_turn', { response: 'draft' });

      expect(response.type).toBe('error');
      expect((response.payload as { failsafePath?: string }).failsafePath).toContain(failsafeDir);
      const files = fs.readdirSync(failsafeDir).filter((file) => file.endsWith('.yaml'));
      expect(files).toHaveLength(1);
      expect(fs.readFileSync(path.join(failsafeDir, files[0]), 'utf8')).toContain(
        'client.SessionLog.UpsertTurnAsync',
      );
    } finally {
      if (oldFailsafeDir === undefined) delete process.env.MCPSERVER_FAILSAFE_DIR;
      else process.env.MCPSERVER_FAILSAFE_DIR = oldFailsafeDir;
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });

  test('cacheFlush accepts nested YAML sessionLog failsafe entries', async () => {
    const fake = new FakeBridge();
    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-session-yaml-failsafe-'));
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;

    fs.writeFileSync(
      path.join(failsafeDir, '001-client-SessionLog-SubmitAsync.yaml'),
      `id: '001'
timestamp: '2026-05-15T00:00:00.000Z'
method: client.SessionLog.SubmitAsync
params:
  sessionLog:
    sourceType: Cline
    sessionId: Cline-20260515T000000Z-yaml-recovery
    title: YAML recovery
    status: completed
    turns:
      - requestId: req-20260515T000100Z-yaml-recovery
        queryTitle: YAML import
        queryText: |
          Preserve this nested YAML turn.
        status: completed
        response: >-
          folded response should become text, not the literal marker.
        actions:
          - order: 1
            description: validated nested yaml failsafe
            type: edit
            status: completed
retryCount: 0
`,
    );

    try {
      const result = await cacheFlush(asBridge(fake));

      expect(result).toEqual({ flushed: 1, failed: 0, pending: 0 });
      expect(fake.calls).toHaveLength(1);
      expect(fake.calls[0].method).toBe('client.SessionLog.SubmitAsync');
      const payload = fake.calls[0].params as { sessionLog: { turns: Record<string, unknown>[] } };
      expect(payload.sessionLog.turns[0]).toMatchObject({
        requestId: 'req-20260515T000100Z-yaml-recovery',
        response: 'folded response should become text, not the literal marker.',
      });
    } finally {
      if (oldFailsafeDir === undefined) delete process.env.MCPSERVER_FAILSAFE_DIR;
      else process.env.MCPSERVER_FAILSAFE_DIR = oldFailsafeDir;
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });

  test('session_append_actions invokes UpsertTurnAsync with current turn including actions', async () => {
    const { shim, fake, bridge } = setup();
    await dispatchSessionTool(shim, bridge, 'session_open', {
      agent: 'Cline',
      sessionId: 'Cline-x-001',
      title: 'demo',
    });
    await dispatchSessionTool(shim, bridge, 'session_begin_turn', {
      requestId: 'req-1',
      queryTitle: 't',
      queryText: 'q',
    });
    await dispatchSessionTool(shim, bridge, 'session_append_actions', {
      actions: [
        { order: 1, description: 'edit', type: 'edit', status: 'completed', filePath: 'a.ts' },
      ],
    });

    expect(fake.calls).toHaveLength(1);
    expect(fake.calls[0].method).toBe('client.SessionLog.UpsertTurnAsync');
    const turn = (fake.calls[0].params as { turn: Record<string, unknown> }).turn;
    expect(turn.actions).toHaveLength(1);
  });

  test('session_query_history is a direct passthrough to QueryAsync', async () => {
    const { shim, fake, bridge } = setup();
    await dispatchSessionTool(shim, bridge, 'session_query_history', { agent: 'Cline', limit: 5 });
    expect(fake.calls).toHaveLength(1);
    expect(fake.calls[0].method).toBe('client.SessionLog.QueryAsync');
    expect(fake.calls[0].params).toEqual({ agent: 'Cline', limit: 5 });
  });

  test('session_query_history uses marker-auth HTTP fallback when available', async () => {
    const { shim, fake, bridge } = setup();
    const oldFetch = globalThis.fetch;
    const oldApiKey = process.env.MCPSERVER_API_KEY;
    const oldWorkspacePath = process.env.MCPSERVER_WORKSPACE_PATH;
    const oldBaseUrl = process.env.MCPSERVER_BASE_URL;

    process.env.MCPSERVER_API_KEY = 'test-api-key';
    process.env.MCPSERVER_WORKSPACE_PATH = 'F:\\GitHub\\FeatureFlags';
    process.env.MCPSERVER_BASE_URL = 'http://127.0.0.1:8765';
    globalThis.fetch = jest.fn(async () => ({
      ok: true,
      status: 200,
      headers: { get: () => 'application/json' },
      text: async () => '{"items":[],"totalCount":0}',
    })) as unknown as typeof fetch;

    try {
      const response = await dispatchSessionTool(shim, bridge, 'session_query_history', {
        agent: 'Cline',
        limit: 5,
      });

      expect(fake.calls).toHaveLength(0);
      expect(response.type).toBe('result');
      expect(response.payload).toEqual({
        result: { items: [], totalCount: 0 },
        contentType: 'application/json',
      });
      const call = (globalThis.fetch as jest.Mock).mock.calls[0];
      expect(String(call[0])).toBe('http://127.0.0.1:8765/mcpserver/sessionlog?agent=Cline&limit=5');
      expect(call[1]).toEqual({
        headers: {
          'X-Api-Key': 'test-api-key',
          'X-Workspace-Path': 'F:\\GitHub\\FeatureFlags',
        },
      });
    } finally {
      globalThis.fetch = oldFetch;
      if (oldApiKey === undefined) delete process.env.MCPSERVER_API_KEY;
      else process.env.MCPSERVER_API_KEY = oldApiKey;
      if (oldWorkspacePath === undefined) delete process.env.MCPSERVER_WORKSPACE_PATH;
      else process.env.MCPSERVER_WORKSPACE_PATH = oldWorkspacePath;
      if (oldBaseUrl === undefined) delete process.env.MCPSERVER_BASE_URL;
      else process.env.MCPSERVER_BASE_URL = oldBaseUrl;
    }
  });

  test('session_close invokes SubmitAsync with final session status', async () => {
    const { shim, fake, bridge } = setup();
    await dispatchSessionTool(shim, bridge, 'session_open', {
      agent: 'Cline',
      sessionId: 'Cline-x-001',
      title: 'demo',
    });
    await dispatchSessionTool(shim, bridge, 'session_close', {
      agent: 'Cline',
      sessionId: 'Cline-x-001',
      status: 'completed',
    });
    expect(fake.calls).toHaveLength(1);
    expect(fake.calls[0].method).toBe('client.SessionLog.SubmitAsync');
    const payload = fake.calls[0].params as { sessionLog: { status: string } };
    expect(payload.sessionLog.status).toBe('completed');
  });

  test('regression guard: no workflow.sessionlog.* method ever reaches the bridge', async () => {
    const { shim, fake, bridge } = setup();
    // Cycle through every tool that mutates state. After all of them, no
    // call should carry a method starting with 'workflow.sessionlog.'.
    await dispatchSessionTool(shim, bridge, 'session_bootstrap', {});
    await dispatchSessionTool(shim, bridge, 'session_open', {
      agent: 'Cline',
      sessionId: 'Cline-x-001',
      title: 'demo',
    });
    await dispatchSessionTool(shim, bridge, 'session_begin_turn', {
      requestId: 'req-1',
      queryTitle: 't',
      queryText: 'q',
    });
    await dispatchSessionTool(shim, bridge, 'session_update_turn', { response: 'r' });
    await dispatchSessionTool(shim, bridge, 'session_append_dialog', {
      dialogItems: [
        { timestamp: '2026-04-19T00:00:00Z', role: 'model', content: 'm', category: 'reasoning' },
      ],
    });
    await dispatchSessionTool(shim, bridge, 'session_append_actions', {
      actions: [
        { order: 1, description: 'd', type: 'edit', status: 'completed', filePath: 'x.ts' },
      ],
    });
    await dispatchSessionTool(shim, bridge, 'session_complete_turn', { response: 'done' });
    await dispatchSessionTool(shim, bridge, 'session_close', {
      agent: 'Cline',
      sessionId: 'Cline-x-001',
      status: 'completed',
    });

    for (const call of fake.calls) {
      expect(call.method).not.toMatch(/^workflow\.sessionlog\./);
    }
  });

  test('unknown tool name throws', async () => {
    const { shim, bridge } = setup();
    await expect(dispatchSessionTool(shim, bridge, 'nope', {})).rejects.toThrow(/Unknown session tool/);
  });
});

describe('syntheticOk', () => {
  test('returns a result envelope with ok payload', () => {
    const r = syntheticOk('test');
    expect(r.type).toBe('result');
    expect((r.payload as { ok: boolean }).ok).toBe(true);
  });
});
