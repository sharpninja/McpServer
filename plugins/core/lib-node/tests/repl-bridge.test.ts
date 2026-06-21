/**
 * Ported from mcpserver-cline-v2-plugin/tests/repl-bridge.test.ts (the
 * timeout/termination spec) and extended with direct coverage of the
 * YAML-over-STDIO framing contract that AgentStdioProtocol.cs dispatches:
 * line-buffered documents terminated by a bare `---` separator, request /
 * result / error / event multiplexing by requestId, and the canonical
 * request-id shape. The cline-v2 plugin had no framing-level coverage; this
 * is new value the canonical core earns by owning the transport.
 */
import * as yaml from 'js-yaml';
import { ReplBridge, type ReplResponse } from '../src/transport/repl-bridge.js';

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

/** Frame a YAML envelope the way mcpserver-repl emits it: doc + `---` line. */
function feedEnvelope(bridge: ReplBridge, envelope: Record<string, unknown>): void {
  const text = yaml.dump(envelope, { lineWidth: -1 });
  for (const line of text.split('\n')) {
    if (line.length === 0) continue;
    feedLine(bridge, line);
  }
  feedLine(bridge, '---');
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
  test('writes a request envelope terminated by --- and resolves on the matching result', async () => {
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

    // One write happened; it is a YAML request envelope ending with `---\n`.
    expect(writes).toHaveLength(1);
    expect(writes[0].endsWith('---\n')).toBe(true);
    const envelope = yaml.load(writes[0].replace(/---\n$/, '')) as {
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

    (bridge as unknown as { proc: unknown }).proc = {
      exitCode: null,
      killed: false,
      kill,
    };
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
    ).terminateAfterTimeout('mcpserver-repl timed out');

    expect(kill).toHaveBeenCalledWith('SIGTERM');
    expect((bridge as unknown as { proc: unknown }).proc).toBeNull();
    expect(rejected[0].message).toBe('mcpserver-repl timed out');
    expect(pendingOf(bridge).size).toBe(0);

    jest.advanceTimersByTime(2000);

    expect(kill).toHaveBeenCalledWith('SIGKILL');
  });

  test('leaves the excepted requestId pending while clearing the rest', () => {
    const bridge = new ReplBridge();
    const kill = jest.fn();
    (bridge as unknown as { proc: unknown }).proc = { exitCode: null, killed: false, kill };
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
