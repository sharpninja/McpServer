/** TEST-QB-004/005/006: trusted bootstrap uses the host's network stack and exact workspace. */
import { fullBootstrap, type MarkerContext } from '../src/discovery/marker-resolver.js';
import { createHmac } from 'crypto';
import { mkdtempSync, rmSync, writeFileSync } from 'fs';
import { tmpdir } from 'os';
import { join } from 'path';
import { dump } from 'js-yaml';

describe('context-owned bootstrap', () => {
  let root: string;
  const originalFetch = globalThis.fetch;
  const healthy: typeof fetch = async input => {
    const url = new URL(String(input));
    return new Response(JSON.stringify({ nonce: url.searchParams.get('nonce') }), { status: 200 });
  };
  // Additive API under test; the previous implementation silently ignores the second argument.
  const bootstrap = fullBootstrap as (workspace: string, options: { fetch?: typeof fetch; expectedWorkspacePath: string }) => Promise<MarkerContext>;

  function marker(workspacePath = root) {
    const fields = { port: '7147', baseUrl: 'https://bootstrap.invalid', apiKey: 'fixture-only-signing-key', workspace: 'fixture', workspacePath,
      pid: '', startedAt: '', markerWrittenAtUtc: '', serverStartedAtUtc: '' };
    const canonical = ['canonicalization=marker-v1', ...Object.entries(fields).map(([key, value]) => `${key}=${value}`)].join('\n') + '\n';
    const value = createHmac('sha256', fields.apiKey).update(canonical).digest('hex').toUpperCase();
    writeFileSync(join(root, 'AGENTS-README-FIRST.yaml'), dump({ ...fields, signature: { value } }));
  }

  beforeEach(() => {
    root = mkdtempSync(join(tmpdir(), 'qb-bootstrap-'));
    globalThis.fetch = jest.fn(healthy);
    marker();
  });
  afterEach(() => {
    globalThis.fetch = originalFetch;
    rmSync(root, { recursive: true, force: true });
  });

  test('uses the injected fetch and keeps redirects disabled with a bounded request', async () => {
    const fetcher = jest.fn(healthy);
    await expect(bootstrap(root, { fetch: fetcher, expectedWorkspacePath: root })).resolves.toMatchObject({ workspacePath: root });
    expect(fetcher).toHaveBeenCalledWith(expect.stringContaining('https://bootstrap.invalid/health?nonce='), expect.objectContaining({ redirect: 'error', signal: expect.any(AbortSignal) }));
    expect(globalThis.fetch).not.toHaveBeenCalled();
  });

  test('requires an injected network implementation for a governed workspace', async () => {
    await expect(bootstrap(root, { expectedWorkspacePath: root })).rejects.toThrow(/injected.*fetch/i);
    expect(globalThis.fetch).not.toHaveBeenCalled();
  });

  test('rejects a signed marker for a foreign workspace before making any HTTP request', async () => {
    marker(join(tmpdir(), 'foreign-workspace'));
    const fetcher = jest.fn(healthy);
    await expect(bootstrap(root, { fetch: fetcher, expectedWorkspacePath: root })).rejects.toThrow(/workspace/i);
    expect(fetcher).not.toHaveBeenCalled();
    expect(globalThis.fetch).not.toHaveBeenCalled();
  });

  test('rejects a wrong nonce returned by the injected transport', async () => {
    const fetcher = jest.fn(async () => new Response(JSON.stringify({ nonce: 'wrong' }), { status: 200 }));
    await expect(bootstrap(root, { fetch: fetcher, expectedWorkspacePath: root })).rejects.toThrow(/nonce/i);
    expect(globalThis.fetch).not.toHaveBeenCalled();
  });
});
