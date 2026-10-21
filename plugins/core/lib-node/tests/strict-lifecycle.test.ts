/** TEST-QB-004/006: governed lifecycle failures stop execution with a stable failure code. */
import { HostContext } from '../src/runtime/host-context.js';
import { ReplBridge } from '../src/transport/repl-bridge.js';
import * as markers from '../src/discovery/marker-resolver.js';
import { cacheFlush } from '../src/cache/cache-manager.js';
import { tmpdir } from 'os';
import { join } from 'path';

jest.mock('../src/cache/cache-manager.js', () => ({
  cacheFlush: jest.fn(), cacheWrite: jest.fn(async () => '/isolated/audit.yaml'), cacheDelete: jest.fn(),
}));
const flush = jest.mocked(cacheFlush);

describe('governed lifecycle failures', () => {
  const config = { governed: true, workspacePath: join(tmpdir(), 'qb-strict'), agentName: 'ClineQb', pluginId: 'cline-qb' };
  beforeEach(() => {
    jest.restoreAllMocks();
    flush.mockReset();
    jest.spyOn(process.stderr, 'write').mockReturnValue(true);
  });

  test('bootstrap rejection is typed and cannot be swallowed by the compatibility method', async () => {
    jest.spyOn(markers, 'fullBootstrap').mockRejectedValue(new Error('synthetic invalid signature'));
    await expect(new HostContext(config).bootstrapBestEffort()).rejects.toMatchObject({ code: 'bootstrap_failed' });
  });

  test('direct bootstrap also exposes a typed rejection', async () => {
    jest.spyOn(markers, 'fullBootstrap').mockRejectedValue(new Error('synthetic invalid nonce'));
    await expect(new HostContext(config).bootstrap()).rejects.toMatchObject({ code: 'bootstrap_failed' });
  });

  test('replay exceptions are typed and cannot be swallowed', async () => {
    flush.mockRejectedValue(new Error('synthetic disk failure'));
    await expect(new HostContext(config).flushCacheBestEffort()).rejects.toMatchObject({ code: 'failsafe_replay_failed' });
  });

  test.each([{ flushed: 0, failed: 1, pending: 1 }, { flushed: 0, failed: 0, pending: 1 }])('unfinished replay blocks and remains retryable: %j', async (result) => {
    flush.mockResolvedValueOnce(result).mockResolvedValueOnce({ flushed: 1, failed: 0, pending: 0 });
    const context = new HostContext(config);
    await expect(context.flushCacheBestEffort()).rejects.toMatchObject({ code: 'failsafe_replay_failed' });
    await expect(context.flushCacheBestEffort()).resolves.toBeUndefined();
    expect(flush).toHaveBeenCalledTimes(2);
  });

  test('session tool transport errors expose the same typed failure to governed callers', async () => {
    const bridge = new ReplBridge(config);
    jest.spyOn(bridge, 'invoke').mockResolvedValue({ type: 'error', payload: { code: 'offline', message: 'synthetic offline' } });
    await expect(new HostContext({ ...config, bridge }).dispatchTool('session_query_history', {})).rejects.toMatchObject({ code: 'session_failed' });
  });

  test('automatic session audit errors are typed too', async () => {
    const bridge = new ReplBridge(config);
    jest.spyOn(bridge, 'invoke').mockRejectedValue(new Error('synthetic offline'));
    const context = new HostContext({ ...config, bridge });
    await context.startSession({ prompt: 'fixture' });
    await expect(context.appendToolAction({ toolName: 'fixture' }, 'completed')).rejects.toMatchObject({ code: 'session_failed' });
  });

  test('legacy best-effort bootstrap still warns and returns', async () => {
    jest.spyOn(markers, 'fullBootstrap').mockRejectedValue(new Error('synthetic offline'));
    await expect(new HostContext({ workspacePath: config.workspacePath }).bootstrapBestEffort()).resolves.toBeUndefined();
    expect(process.stderr.write).toHaveBeenCalled();
  });
});
