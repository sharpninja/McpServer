/** TEST-QB-005/007/008: conversation continuity and separate action/decision records. */
import { HostContext } from '../src/runtime/host-context.js';
import { ReplBridge, type ReplResponse } from '../src/transport/repl-bridge.js';
import { getSessionShimState } from '../src/tools/session.js';
import { tmpdir } from 'os';
import { join } from 'path';

jest.mock('../src/cache/cache-manager.js', () => ({
  cacheWrite: jest.fn(async () => '/isolated/audit.yaml'), cacheDelete: jest.fn(),
}));

describe('governed conversation audit', () => {
  function fixture() {
    const config = { governed: true, agentName: 'ClineQb', pluginId: 'cline-qb', workspacePath: join(tmpdir(), 'qb-conversation') };
    const bridge = new ReplBridge(config);
    const calls: Array<{ method: string; params?: Record<string, unknown> }> = [];
    jest.spyOn(bridge, 'invoke').mockImplementation(async (method, params): Promise<ReplResponse> => {
      calls.push({ method, params: structuredClone(params) });
      return { type: 'result', payload: { ok: true } };
    });
    return { host: new HostContext({ ...config, bridge, validateArguments: true }), bridge, calls };
  }
  afterEach(() => { jest.useRealTimers(); jest.restoreAllMocks(); });

  test.each([false, true])('finishing a turn preserves the conversation (failure=%s)', async (failure) => {
    const { host, bridge, calls } = fixture();
    await host.startSession({ prompt: 'first turn' });
    await host.completeSession(failure ? { error: 'synthetic failure' } : { response: 'finished' });
    expect(getSessionShimState(bridge)?.status).toBe('in_progress');
    expect(getSessionShimState(bridge)?.turns[0].status).toBe(failure ? 'failed' : 'completed');
    expect(calls.some(call => call.method === 'client.SessionLog.SubmitAsync')).toBe(false);
  });

  test('repeated same-second prompts retain both turns with distinct IDs and per-turn action order', async () => {
    jest.useFakeTimers().setSystemTime(new Date('2026-10-09T12:00:00Z'));
    const { host, bridge } = fixture();
    const requestIds: string[] = [];
    const sessionIds: string[] = [];
    for (let i = 0; i < 2; i++) {
      await host.startSession({ prompt: 'same prompt' });
      await host.appendToolAction({ toolName: 'read_file' }, 'completed');
      requestIds.push(getSessionShimState(bridge)!.currentTurn!.requestId);
      sessionIds.push(getSessionShimState(bridge)!.sessionId);
      await host.completeSession({ response: 'finished' });
    }
    const state = getSessionShimState(bridge)!;
    expect(state.turns).toHaveLength(2);
    expect(new Set(requestIds).size).toBe(2);
    expect(new Set(sessionIds).size).toBe(1);
    expect(state.turns.map(turn => turn.actions[0].order)).toEqual([1, 1]);
  });

  test('simultaneous conversations in one workspace have distinct session IDs', async () => {
    jest.useFakeTimers().setSystemTime(new Date('2026-10-09T12:00:00Z'));
    const a = fixture();
    const b = fixture();
    await Promise.all([a.host.startSession({ prompt: 'same' }), b.host.startSession({ prompt: 'same' })]);
    expect(getSessionShimState(a.bridge)!.sessionId).not.toBe(getSessionShimState(b.bridge)!.sessionId);
  });

  test('an overlapping turn cannot overwrite the active turn', async () => {
    const { host, bridge } = fixture();
    await host.startSession({ prompt: 'first' });
    await expect(host.startSession({ prompt: 'second' })).rejects.toMatchObject({ code: 'session_failed' });
    expect(getSessionShimState(bridge)?.currentTurn?.queryText).toBe('first');
  });

  test('automatic tool entries are actions and do not invent design decisions', async () => {
    const { host, bridge, calls } = fixture();
    await host.startSession({ prompt: 'inspect' });
    await host.appendToolAction({ toolName: 'read_file', input: { path: 'a.ts' } }, 'pending');
    await host.appendToolAction({ toolName: 'read_file', input: { path: 'a.ts' } }, 'completed');
    const turn = getSessionShimState(bridge)!.currentTurn!;
    expect(turn.actions.map(action => action.type)).toEqual(['tool_execution', 'tool_execution']);
    expect(turn.dialogItems.map(item => item.category)).toEqual(['tool_call', 'tool_result']);
    expect(calls.at(-1)?.params?.turn).not.toHaveProperty('designDecisions');
  });

  test('explicit decisions append independently and survive wire serialization', async () => {
    const { host, bridge, calls } = fixture();
    await host.startSession({ prompt: 'decide' });
    await host.appendToolAction({ toolName: 'read_file' }, 'completed');
    await host.dispatchTool('session_append_decisions', { decisions: ['Use private transport credentials.'] });
    await host.dispatchTool('session_append_decisions', { decisions: ['Keep the conversation open across turns.'] });
    expect(calls.at(-1)?.params?.turn).toMatchObject({ designDecisions: ['Use private transport credentials.', 'Keep the conversation open across turns.'] });
    expect(getSessionShimState(bridge)!.currentTurn!.actions).toHaveLength(1);
  });
});
