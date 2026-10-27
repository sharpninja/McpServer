/**
 * Ported from mcpserver-cline-v2-plugin/tests/repl-bridge.test.ts (the
 * timeout/termination spec) and extended with direct coverage of the
 * YAML-over-STDIO framing contract that AgentStdioProtocol.cs dispatches:
 * line-buffered documents terminated by a bare `---` separator, request /
 * result / error / event multiplexing by requestId, and the canonical
 * request-id shape. The cline-v2 plugin had no framing-level coverage; this
 * is new value the canonical core earns by owning the transport.
 */
import { spawn, type ChildProcess } from 'child_process';
import { EventEmitter } from 'events';
import { mkdtempSync, writeFileSync } from 'fs';
import { tmpdir } from 'os';
import { dirname, join } from 'path';
import { PassThrough } from 'stream';
import * as yaml from 'js-yaml';
import { ReplBridge, resolveReplCommand, type ReplResponse } from '../src/transport/repl-bridge.js';

jest.mock('child_process', () => ({
  spawn: jest.fn(),
}));

const spawnMock = spawn as jest.MockedFunction<typeof spawn>;

type PendingMap = Map<
  string,
  {
    resolve: (value: ReplResponse) => void;
    reject: (reason: Error) => void;
    events: ReplResponse[];
    onEvent?: (event: ReplResponse) => void;
    timer?: ReturnType<typeof setTimeout>;
  }
>;

function pendingOf(bridge: ReplBridge): PendingMap {
  return (bridge as unknown as { pending: PendingMap }).pending;
}

function feedLine(bridge: ReplBridge, line: string): void {
  (bridge as unknown as { onLine: (line: string) => void }).onLine(line);
}

/** Frame a YAML envelope the way qbrain-ai-repl emits it: doc + `---` line. */
function feedEnvelope(bridge: ReplBridge, envelope: Record<string, unknown>): void {
  const text = yaml.dump(envelope, { lineWidth: -1 });
  for (const line of text.split('\n')) {
    if (line.length === 0) continue;
    feedLine(bridge, line);
  }
  feedLine(bridge, '---');
}

function createMockChildProcess(): ChildProcess {
  const child = new EventEmitter() as ChildProcess;
  Object.assign(child, {
    stdin: new PassThrough(),
    stdout: new PassThrough(),
    stderr: new PassThrough(),
    exitCode: null,
    signalCode: null,
    killed: false,
    kill: jest.fn(),
  });
  return child;
}

describe('ReplBridge.generateRequestId', () => {
  test('matches the canonical req-<stamp>-<slug>-<rand> shape', () => {
    const id = ReplBridge.generateRequestId('beginTurn');
    expect(id).toMatch(/^req-\d{8}T\d{6}Z-[a-z0-9]+-[0-9a-f]{4}$/);
  });

  test('sanitizes slugs to lowercase alphanumerics and falls back to req', () => {
    expect(ReplBridge.generateRequestId('Complete.Turn!')).toMatch(/^req-\d{8}T\d{6}Z-completeturn-[0-9a-f]{4}$/);
    expect(ReplBridge.generateRequestId('***')).toMatch(/^req-\d{8}T\d{6}Z-req-[0-9a-f]{4}$/);
  });
});

describe('ReplBridge process launch', () => {
  const originalEnv = { ...process.env };
  const originalCwd = process.cwd();

  beforeEach(() => {
    spawnMock.mockReset();
    process.env = { ...originalEnv };
    process.chdir(originalCwd);
  });

  afterAll(() => {
    process.env = originalEnv;
    process.chdir(originalCwd);
  });

  test('TEST-MCP-PLUGIN-PSONLY-001 starts qbrain-ai-repl with the MCP workspace cwd instead of the Node process cwd', async () => {
    const workspace = originalCwd;
    const wrongCurrentDirectory = dirname(originalCwd);
    process.chdir(wrongCurrentDirectory);
    process.env.MCP_WORKSPACE_PATH = workspace;
    process.env.MCP_AGENT_NAME = 'Cline';
    process.env.PATH = mkdtempSync(join(tmpdir(), 'repl-empty-'));
    delete process.env.MCPSERVER_REPL_BIN;
    delete process.env.MCPSERVER_REPL_COMMAND;
    spawnMock.mockReturnValue(createMockChildProcess());

    const bridge = new ReplBridge();
    await bridge.ensure();

    expect(spawnMock).toHaveBeenCalledWith(
      'qbrain-ai-repl',
      ['--agent-stdio', '--agent', 'Cline'],
      expect.objectContaining({
        cwd: workspace,
        stdio: ['pipe', 'pipe', 'pipe'],
        env: expect.objectContaining({
          MCP_WORKSPACE_PATH: workspace,
        }),
      }),
    );
  });

  test('uses mcpserver-repl when qbrain-ai-repl is not installed', async () => {
    const bin = mkdtempSync(join(tmpdir(), 'repl-legacy-'));
    writeFileSync(join(bin, 'mcpserver-repl'), '');
    process.env.PATH = bin;
    delete process.env.MCPSERVER_REPL_BIN;
    delete process.env.MCPSERVER_REPL_COMMAND;
    spawnMock.mockReturnValue(createMockChildProcess());

    const bridge = new ReplBridge();
    await bridge.ensure();

    expect(spawnMock).toHaveBeenCalledWith(
      'mcpserver-repl',
      expect.any(Array),
      expect.any(Object),
    );
    expect(resolveReplCommand()).toBe('mcpserver-repl');
  });

  test('prefers qbrain-ai-repl when both REPL commands are installed', async () => {
    const bin = mkdtempSync(join(tmpdir(), 'repl-both-'));
    writeFileSync(join(bin, 'mcpserver-repl'), '');
    writeFileSync(join(bin, 'qbrain-ai-repl'), '');
    process.env.PATH = bin;
    delete process.env.MCPSERVER_REPL_BIN;
    delete process.env.MCPSERVER_REPL_COMMAND;
    spawnMock.mockReturnValue(createMockChildProcess());

    const bridge = new ReplBridge();
    await bridge.ensure();

    expect(spawnMock.mock.calls[0][0]).toBe('qbrain-ai-repl');
  });
});

describe('ReplBridge framing (NDJSON + --- terminator contract)', () => {
  test('buffers lines until the --- separator, then resolves the matching pending request', () => {
    const bridge = new ReplBridge();
    let resolved: ReplResponse | undefined;
    pendingOf(bridge).set('req-1', {
      resolve: (value) => {
        resolved = value;
      },
      reject: () => undefined,
      events: [],
    });

    // Lines accumulate; nothing resolves before the terminator.
    feedLine(bridge, 'type: result');
    feedLine(bridge, 'payload:');
    feedLine(bridge, '  requestId: req-1');
    feedLine(bridge, '  ok: true');
    expect(resolved).toBeUndefined();

    feedLine(bridge, '---');
    expect(resolved).toEqual({ type: 'result', payload: { requestId: 'req-1', ok: true } });
    // Resolved requests are removed from the pending map.
    expect(pendingOf(bridge).has('req-1')).toBe(false);
  });

  test('routes concurrent documents to their own requestId without cross-talk', () => {
    const bridge = new ReplBridge();
    const got: Record<string, ReplResponse> = {};
    for (const id of ['req-a', 'req-b']) {
      pendingOf(bridge).set(id, {
        resolve: (value) => {
          got[id] = value;
        },
        reject: () => undefined,
        events: [],
      });
    }

    feedEnvelope(bridge, { type: 'result', payload: { requestId: 'req-b', value: 2 } });
    feedEnvelope(bridge, { type: 'result', payload: { requestId: 'req-a', value: 1 } });

    expect(got['req-a']).toEqual({ type: 'result', payload: { requestId: 'req-a', value: 1 } });
    expect(got['req-b']).toEqual({ type: 'result', payload: { requestId: 'req-b', value: 2 } });
  });

  test('event envelopes accumulate and stream without resolving the request', () => {
    const bridge = new ReplBridge();
    const events: ReplResponse[] = [];
    let final: ReplResponse | undefined;
    pendingOf(bridge).set('req-stream', {
      resolve: (value) => {
        final = value;
      },
      reject: () => undefined,
      events: [],
      onEvent: (event) => events.push(event),
    });

    feedEnvelope(bridge, { type: 'event', payload: { requestId: 'req-stream', step: 'one' } });
    feedEnvelope(bridge, { type: 'event', payload: { requestId: 'req-stream', step: 'two' } });
    expect(final).toBeUndefined();
    expect(events).toHaveLength(2);
    expect(pendingOf(bridge).get('req-stream')!.events).toHaveLength(2);

    feedEnvelope(bridge, { type: 'result', payload: { requestId: 'req-stream', done: true } });
    expect(final).toEqual({ type: 'result', payload: { requestId: 'req-stream', done: true } });
    expect(pendingOf(bridge).has('req-stream')).toBe(false);
  });

  test('error envelopes resolve (not reject) the pending request so the handler can inspect them', () => {
    const bridge = new ReplBridge();
    let resolved: ReplResponse | undefined;
    pendingOf(bridge).set('req-err', {
      resolve: (value) => {
        resolved = value;
      },
      reject: () => {
        throw new Error('should not reject on error envelope');
      },
      events: [],
    });

    feedEnvelope(bridge, {
      type: 'error',
      payload: { requestId: 'req-err', code: 'method_not_found', message: 'nope' },
    });

    expect(resolved).toEqual({
      type: 'error',
      payload: { requestId: 'req-err', code: 'method_not_found', message: 'nope' },
    });
  });

  test('documents without a requestId and unknown requestIds are dropped silently', () => {
    const bridge = new ReplBridge();
    const seen: ReplResponse[] = [];
    pendingOf(bridge).set('req-known', {
      resolve: (value) => seen.push(value),
      reject: () => undefined,
      events: [],
    });

    // No requestId -> broadcast event, ignored.
    feedEnvelope(bridge, { type: 'event', payload: { step: 'broadcast' } });
    // Unknown requestId -> no matching pending entry.
    feedEnvelope(bridge, { type: 'result', payload: { requestId: 'req-unknown', value: 9 } });

    expect(seen).toHaveLength(0);
    expect(pendingOf(bridge).has('req-known')).toBe(true);
  });

  test('malformed YAML between separators is skipped without throwing or resolving', () => {
    const bridge = new ReplBridge();
    let resolved = false;
    pendingOf(bridge).set('req-1', {
      resolve: () => {
        resolved = true;
      },
      reject: () => undefined,
      events: [],
    });

    // ": :" is invalid YAML; the bridge logs and discards the buffer.
    expect(() => {
      feedLine(bridge, ': : not : valid');
      feedLine(bridge, '---');
    }).not.toThrow();
    expect(resolved).toBe(false);
  });

  test('a payload-less document does not resolve anything', () => {
    const bridge = new ReplBridge();
    let resolved = false;
    pendingOf(bridge).set('req-1', {
      resolve: () => {
        resolved = true;
      },
      reject: () => undefined,
      events: [],
    });

    feedEnvelope(bridge, { type: 'result' });
    expect(resolved).toBe(false);
    expect(pendingOf(bridge).has('req-1')).toBe(true);
  });
});

describe('ReplBridge.invoke envelope', () => {
  test('writes a JSON request envelope and resolves on the matching result', async () => {
    const bridge = new ReplBridge();
    const writes: string[] = [];
    // Pretend the process is already alive so ensure() short-circuits.
    (bridge as unknown as { proc: unknown }).proc = {
      exitCode: null,
      killed: false,
      stdin: { write: (chunk: string) => writes.push(chunk) },
    };

    const promise = bridge.invoke('client.SessionLog.QueryAsync', { agent: 'Cline' });

    // invoke() awaits ensure() before writing, so let the microtask queue
    // drain before inspecting the stdin write.
    await Promise.resolve();
    await Promise.resolve();

    // One write happened; it is a JSON request envelope ending with a newline.
    expect(writes).toHaveLength(1);
    expect(writes[0].endsWith('\n')).toBe(true);
    const envelope = JSON.parse(writes[0]) as {
      type: string;
      payload: { requestId: string; method: string; params: Record<string, unknown> };
    };
    expect(envelope.type).toBe('request');
    expect(envelope.payload.method).toBe('client.SessionLog.QueryAsync');
    expect(envelope.payload.params).toEqual({ agent: 'Cline' });
    expect(envelope.payload.requestId).toMatch(/^req-\d{8}T\d{6}Z-/);

    // Server replies with the matching requestId -> promise resolves.
    feedEnvelope(bridge, {
      type: 'result',
      payload: { requestId: envelope.payload.requestId, result: { items: [] } },
    });

    await expect(promise).resolves.toEqual({
      type: 'result',
      payload: { requestId: envelope.payload.requestId, result: { items: [] } },
    });
  });
});

describe('ReplBridge timeout handling', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  test('terminates the REPL process and rejects pending requests after a timeout', () => {
    const bridge = new ReplBridge();
    const kill = jest.fn();
    const timer = setTimeout(() => undefined, 5000);
    const rejected: Error[] = [];

    (bridge as unknown as { proc: unknown }).proc = Object.assign(createMockChildProcess(), { kill });
    pendingOf(bridge).set('req-other', {
      resolve: jest.fn(),
      reject: (error: Error) => rejected.push(error),
      events: [],
      timer,
    });

    (
      bridge as unknown as {
        terminateAfterTimeout: (message: string, exceptRequestId?: string) => void;
      }
    ).terminateAfterTimeout('qbrain-ai-repl timed out');

    expect(kill).toHaveBeenCalledWith('SIGTERM');
    expect((bridge as unknown as { proc: unknown }).proc).toBeNull();
    expect(rejected[0].message).toBe('qbrain-ai-repl timed out');
    expect(rejected[0]).toMatchObject({ detailCode: 'transport_timeout', retryable: true });
    expect(pendingOf(bridge).size).toBe(0);

    jest.advanceTimersByTime(2000);

    expect(kill).toHaveBeenCalledWith('SIGKILL');
  });

  test('leaves the excepted requestId pending while clearing the rest', () => {
    const bridge = new ReplBridge();
    const kill = jest.fn();
    (bridge as unknown as { proc: unknown }).proc = Object.assign(createMockChildProcess(), { kill });
    const keptReject = jest.fn();
    const droppedReject = jest.fn();
    pendingOf(bridge).set('req-keep', { resolve: jest.fn(), reject: keptReject, events: [] });
    pendingOf(bridge).set('req-drop', { resolve: jest.fn(), reject: droppedReject, events: [] });

    (
      bridge as unknown as {
        terminateAfterTimeout: (message: string, exceptRequestId?: string) => void;
      }
    ).terminateAfterTimeout('timed out', 'req-keep');

    expect(droppedReject).toHaveBeenCalledTimes(1);
    expect(keptReject).not.toHaveBeenCalled();
    expect(pendingOf(bridge).size).toBe(0);
  });
});

/** TEST-QB-003: retired REPL children cannot affect a replacement transport. */
describe('ReplBridge process ownership during recovery', () => {
  beforeEach(() => { jest.useFakeTimers(); spawnMock.mockReset(); });
  afterEach(() => { jest.clearAllTimers(); jest.useRealTimers(); });

  async function requestId(child: ChildProcess): Promise<string> {
    await Promise.resolve();
    await Promise.resolve();
    return JSON.parse((child.stdin as PassThrough).read().toString()).payload.requestId;
  }
  function reply(child: ChildProcess, id: string, type = 'result'): void {
    (child.stdout as PassThrough).write(yaml.dump({ type, payload: { requestId: id, ok: true } }) + '---\n');
  }

  test.each(['process', 'stdin', 'stdout', 'stderr'] as const)('handles %s errors without crashing or stranding requests', async channel => {
    const failed = createMockChildProcess();
    const replacement = createMockChildProcess();
    spawnMock.mockReturnValueOnce(failed).mockReturnValueOnce(replacement);
    const bridge = new ReplBridge({ timeoutMs: 100 });
    const pending = bridge.invoke('first').catch(error => error);
    await requestId(failed);
    const emitter = channel === 'process' ? failed : failed[channel]!;
    expect(() => emitter.emit('error', new Error('transport unavailable'))).not.toThrow();
    expect(await pending).toEqual(new Error('qbrain-ai-repl transport failed: transport unavailable'));
    expect(pendingOf(bridge).size).toBe(0);

    const next = bridge.invoke('next').catch(error => error);
    const id = await requestId(replacement);
    expect(() => emitter.emit('error', new Error('late pipe failure'))).not.toThrow();
    expect(() => failed.emit('error', new Error('late failure'))).not.toThrow();
    reply(replacement, id);
    expect(await next).toEqual({ type: 'result', payload: { requestId: id, ok: true } });
    expect(replacement.kill).not.toHaveBeenCalled();
  });

  test('a real missing executable rejects without an unhandled spawn error', async () => {
    jest.useRealTimers();
    spawnMock.mockImplementationOnce(jest.requireActual<typeof import('child_process')>('child_process').spawn);
    const bridge = new ReplBridge({ command: join(tmpdir(), `qb-missing-repl-${process.pid}-${Date.now()}`), timeoutMs: 1000 });
    await expect(bridge.invoke('probe')).rejects.toThrow(/qbrain-ai-repl transport (failed|unavailable)/);
    expect(pendingOf(bridge).size).toBe(0);
    await bridge.close();
  });

  test('late output and exit from a timed-out child cannot settle a replacement request', async () => {
    const old = createMockChildProcess();
    const replacement = createMockChildProcess();
    spawnMock.mockReturnValueOnce(old).mockReturnValueOnce(replacement);
    const bridge = new ReplBridge({ timeoutMs: 100 });
    const first = bridge.invoke('first').catch(error => error);
    await requestId(old);
    await jest.advanceTimersByTimeAsync(100);
    expect(await first).toBeInstanceOf(Error);

    const onEvent = jest.fn();
    const next = bridge.invokeStreaming('next', {}, onEvent).catch(error => error);
    const id = await requestId(replacement);
    reply(old, id, 'event');
    old.emit('exit', null, 'SIGTERM');
    reply(replacement, id);
    expect(onEvent).not.toHaveBeenCalled();
    expect(await next).toEqual({ type: 'result', payload: { requestId: id, ok: true } });
    await bridge.ensure();
    expect(spawnMock).toHaveBeenCalledTimes(2);
  });

  test('escalates after SIGTERM was delivered but the child is still alive', async () => {
    const child = createMockChildProcess();
    jest.mocked(child.kill).mockImplementation(() => { Object.assign(child, { killed: true }); return true; });
    spawnMock.mockReturnValue(child);
    const bridge = new ReplBridge({ timeoutMs: 100 });
    const pending = bridge.invoke('hang').catch(error => error);
    await requestId(child);
    await jest.advanceTimersByTimeAsync(100);
    expect(await pending).toBeInstanceOf(Error);
    expect(child.kill).toHaveBeenCalledWith('SIGTERM');
    await jest.advanceTimersByTimeAsync(2000);
    expect(child.kill).toHaveBeenCalledWith('SIGKILL');
  });

  test('close owns only its original child and promptly rejects its outstanding requests', async () => {
    const old = createMockChildProcess();
    const replacement = createMockChildProcess();
    spawnMock.mockReturnValueOnce(old).mockReturnValueOnce(replacement);
    const bridge = new ReplBridge({ timeoutMs: 100 });
    const rejected = jest.fn();
    const first = bridge.invoke('first').catch(rejected);
    await requestId(old);
    const closing = bridge.close();
    await jest.advanceTimersByTimeAsync(0);
    expect(rejected).toHaveBeenCalledTimes(1);
    await first;

    const next = bridge.invoke('next').catch(error => error);
    const id = await requestId(replacement);
    old.emit('exit', 0, null);
    await closing;
    reply(replacement, id);
    expect(await next).toEqual({ type: 'result', payload: { requestId: id, ok: true } });
    expect(replacement.kill).not.toHaveBeenCalled();
  });
});
