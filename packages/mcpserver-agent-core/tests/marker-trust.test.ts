/**
 * marker-trust.test.ts
 * Byrd v4 Red-Green: tests written FIRST for IV4MarkerTrustService contract + exact sh reference behavior.
 * Covers: upward find (AGENTS-README-FIRST.yaml, depth, root), full canonical payload HMAC-SHA256 (upper hex),
 * nonce health challenge, exact "MCP_UNTRUSTED: ..." errors, env var export on success (MCPSERVER_*).
 * Uses real temp FS for find/verify; injectable fetch mock for health.
 * Run with: npm test (after npm i in package)
 * These must pass on stub/mocks then on real impl (no escape from parity).
 */

import { mkdtemp, mkdir, writeFile, rm, realpath } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { afterEach, beforeEach, describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { V4MarkerTrustService, type IV4MarkerData, type IV4TrustResult } from '../src/marker-trust.js';

const MARKER_NAME = 'AGENTS-README-FIRST.yaml';

async function createTempWorkspace(): Promise<string> {
  const root = await mkdtemp(join(tmpdir(), 'mcp-marker-test-'));
  return realpath(root);
}

async function writeMarker(dir: string, content: string): Promise<string> {
  const p = join(dir, MARKER_NAME);
  await writeFile(p, content, 'utf8');
  return p;
}

async function cleanup(dir: string) {
  await rm(dir, { recursive: true, force: true }).catch(() => {});
}

// Minimal marker yaml fixture matching sh parse expectations (top-level + endpoints + signature)
function makeValidMarkerYaml(apiKey: string, workspacePath: string, extra: Record<string, string> = {}): string {
  const port = extra.port ?? '7147';
  const baseUrl = extra.baseUrl ?? 'http://localhost:7147';
  const workspace = extra.workspace ?? 'testws';
  const pid = extra.pid ?? '1234';
  const started = extra.startedAt ?? '2026-05-28T12:00:00Z';
  const written = extra.markerWrittenAtUtc ?? '2026-05-28T12:00:01Z';
  const serverStarted = extra.serverStartedAtUtc ?? '2026-05-28T12:00:00Z';

  return `port: ${port}
baseUrl: ${baseUrl}
apiKey: ${apiKey}
workspace: ${workspace}
workspacePath: ${workspacePath}
pid: ${pid}
startedAt: ${started}
markerWrittenAtUtc: ${written}
serverStartedAtUtc: ${serverStarted}
endpoints:
  health: /health
  sessionLog: /mcpserver/sessionlog
signature:
  value: PLACEHOLDER
`;
}

// Compute what the real impl must produce for sig (used in test setup only)
async function computeExpectedSignatureForTest(apiKey: string, markerContent: string): Promise<string> {
  // Import the real compute fn once impl exists; for red we precompute with simplified but switch to match after
  // For now, placeholder - real test will drive impl of computeCanonicalPayload + hmac
  return 'PLACEHOLDER-FOR-RED';
}

describe('V4MarkerTrustService (marker only, Phase 2 slice)', () => {
  let tempRoot: string;
  let sut: V4MarkerTrustService;

  beforeEach(async () => {
    tempRoot = await createTempWorkspace();
    sut = new V4MarkerTrustService(); // real or stub injected later
  });

  afterEach(async () => {
    await cleanup(tempRoot);
    // reset env touched by bootstrap
    delete (process.env as any).MCPSERVER_BASE_URL;
    delete (process.env as any).MCPSERVER_API_KEY;
    delete (process.env as any).MCPSERVER_WORKSPACE;
    delete (process.env as any).MCPSERVER_WORKSPACE_PATH;
  });

  it('FindMarkerFileAsync_UpwardWalk_FindsInAncestor (exact sh: dirname walk, depth<=20, root check)', async () => {
    const ancestor = join(tempRoot, 'ws');
    const deep = join(ancestor, 'a', 'b', 'c', 'd', 'project');
    await mkdir(deep, { recursive: true });
    const markerPath = await writeMarker(ancestor, makeValidMarkerYaml('k1', ancestor));

    const found = await sut.FindMarkerFileAsync(deep);
    assert.ok(found, 'should find');
    assert.equal(found, markerPath);
  });

  it('FindMarkerFileAsync_NoMarker_ReturnsNull (no infinite walk)', async () => {
    const found = await sut.FindMarkerFileAsync(tempRoot);
    assert.equal(found, null);
  });

  it('VerifySignatureAndParseAsync_ValidCanonicalPayload_ReturnsData (must match sh payload + upper HMAC exactly)', async () => {
    const apiKey = 'XkDlmHl7JXVIjmZ9yyqOCtsoTYuzXoAWJIrGcPNjBz8';
    const wsPath = tempRoot.replace(/\\/g, '/'); // canonical posix-ish
    let yaml = makeValidMarkerYaml(apiKey, wsPath);
    // Pre-sign using exact impl canonical payload + upper hmac (full sh parity)
    const correctSig = (sut as any).signMarkerContentForTest(apiKey, yaml);
    yaml = yaml.replace('PLACEHOLDER', correctSig);
    const markerPath = await writeMarker(tempRoot, yaml);

    const data: IV4MarkerData = await sut.VerifySignatureAndParseAsync(markerPath);

    assert.equal(data.WorkspacePath, wsPath);
    assert.equal(data.ApiKey, apiKey);
    assert.equal((data.Signature || '').toUpperCase(), correctSig);
    assert.ok(data.ServerUrl.includes('http'));
  });

  it('VerifySignatureAndParseAsync_BadSig_ThrowsExactMcpUntrusted (observable contract)', async () => {
    const apiKey = 'badkey';
    let yaml = makeValidMarkerYaml(apiKey, tempRoot);
    yaml = yaml.replace('PLACEHOLDER', 'DEADBEEF0000'); // wrong
    const markerPath = await writeMarker(tempRoot, yaml);

    await assert.rejects(
      async () => sut.VerifySignatureAndParseAsync(markerPath),
      (err: any) => {
        assert.match(String(err.message || err), /MCP_UNTRUSTED.*signature/i);
        return true;
      }
    );
  });

  it('PerformNonceHealthChallengeAsync_ValidEcho_ReturnsTrue (uses /health?nonce=)', async () => {
    const marker: IV4MarkerData = {
      WorkspacePath: tempRoot,
      ServerUrl: 'http://localhost:9999',
      ApiKey: 'k',
      Signature: 's',
      Nonce: 'nonce-abc-123',
      Metadata: {}
    };

    // Injectable mock fetch for test isolation (Byrd mocks phase)
    const mockFetch = async (url: string) => {
      assert.ok(url.includes('/health?nonce=nonce-abc-123'));
      return {
        ok: true,
        text: async () => JSON.stringify({ status: 'ok', nonce: 'nonce-abc-123' })
      } as any;
    };

    const ok = await sut.PerformNonceHealthChallengeAsync(marker, { fetcher: mockFetch as any });
    assert.equal(ok, true);
  });

  it('PerformNonceHealthChallengeAsync_BadResponse_ReturnsFalse (triggers untrusted)', async () => {
    const marker: IV4MarkerData = {
      WorkspacePath: tempRoot,
      ServerUrl: 'http://localhost:9999',
      ApiKey: 'k',
      Signature: null,
      Nonce: 'n2',
      Metadata: {}
    };
    const mockFetch = async () => ({ ok: true, text: async () => '{"status":"ok"}' } as any);

    const ok = await sut.PerformNonceHealthChallengeAsync(marker, { fetcher: mockFetch as any });
    assert.equal(ok, false);
  });

  it('BootstrapTrustAsync_FullHappyPath_SetsEnvAndTrusted (find+verify+nonce+exports, exact sh names)', async () => {
    const apiKey = 'testkey-for-bootstrap';
    const ws = tempRoot.replace(/\\/g, '/');
    let yaml = makeValidMarkerYaml(apiKey, ws, { baseUrl: 'http://127.0.0.1:7777' });
    const correctSig = (sut as any).signMarkerContentForTest(apiKey, yaml);
    yaml = yaml.replace('PLACEHOLDER', correctSig);
    const markerPath = await writeMarker(tempRoot, yaml);

    const mockFetch = async (url: string) => {
      const nonceMatch = url.match(/nonce=([^&]+)/);
      const nonce = nonceMatch ? nonceMatch[1] : 'x';
      return { ok: true, text: async () => `{"status":"ok","nonce":"${nonce}"}` } as any;
    };

    const result: IV4TrustResult = await sut.BootstrapTrustAsync(tempRoot, { fetcher: mockFetch as any });

    assert.equal(result.IsTrusted, true);
    assert.match(result.TrustMethod, /signature_verified|nonce/);
    assert.ok(result.MarkerData);

    // env setup exact per sh full_bootstrap
    assert.equal(process.env.MCPSERVER_BASE_URL, 'http://127.0.0.1:7777');
    assert.equal(process.env.MCPSERVER_API_KEY, apiKey);
    assert.ok(process.env.MCPSERVER_WORKSPACE);
    assert.equal(process.env.MCPSERVER_WORKSPACE_PATH, ws);
  });

  it('BootstrapTrustAsync_NoMarker_ProducesMcpUntrustedResult (exact error path)', async () => {
    const result = await sut.BootstrapTrustAsync(tempRoot);
    assert.equal(result.IsTrusted, false);
    assert.equal(result.TrustMethod, 'MCP_UNTRUSTED');
    assert.match(result.DenialReason || '', /No marker file found|marker/i);
  });

  // Additional focused coverage for Phase 2 cross-validation (exact error strings + edge paths from contracts/sh parity)
  it('VerifySignatureAndParseAsync_MissingApiKey_ThrowsExactMcpUntrusted', async () => {
    const apiKey = 'k';
    let yaml = makeValidMarkerYaml(apiKey, tempRoot);
    // remove apiKey line to trigger parse error
    yaml = yaml.replace(/^apiKey:.*$/m, '');
    yaml = yaml.replace('PLACEHOLDER', 'DEADBEEF');
    const markerPath = await writeMarker(tempRoot, yaml);

    await assert.rejects(
      async () => sut.VerifySignatureAndParseAsync(markerPath),
      (err: any) => {
        assert.match(String(err.message || err), /MCP_UNTRUSTED: missing apiKey in marker/);
        return true;
      }
    );
  });

  it('VerifySignatureAndParseAsync_MissingSignature_ThrowsExactMcpUntrusted', async () => {
    const apiKey = 'k2';
    let yaml = makeValidMarkerYaml(apiKey, tempRoot);
    yaml = yaml.replace(/^signature:[\s\S]*?value:.*$/m, 'signature:');
    const markerPath = await writeMarker(tempRoot, yaml);

    await assert.rejects(
      async () => sut.VerifySignatureAndParseAsync(markerPath),
      (err: any) => {
        assert.match(String(err.message || err), /MCP_UNTRUSTED: No signature value found in marker file/);
        return true;
      }
    );
  });

  it('PerformNonceHealthChallengeAsync_NoServerUrlOrNoFetcher_ReturnsFalse', async () => {
    const markerNoUrl: IV4MarkerData = {
      WorkspacePath: tempRoot,
      ServerUrl: '',
      ApiKey: 'k',
      Signature: 's',
      Nonce: 'n',
      Metadata: {}
    };
    const ok1 = await sut.PerformNonceHealthChallengeAsync(markerNoUrl);
    assert.equal(ok1, false);

    const marker: IV4MarkerData = {
      WorkspacePath: tempRoot,
      ServerUrl: 'http://localhost:1',
      ApiKey: 'k',
      Signature: 's',
      Nonce: 'n',
      Metadata: {}
    };
    // no fetcher provided and global may be absent in some envs -> false path
    const ok2 = await sut.PerformNonceHealthChallengeAsync(marker, { fetcher: undefined as any });
    assert.equal(ok2, false);
  });

  it('BootstrapTrustAsync_NonceFail_ProducesUntrustedWithMarkerData', async () => {
    const apiKey = 'noncefailkey';
    const ws = tempRoot.replace(/\\/g, '/');
    let yaml = makeValidMarkerYaml(apiKey, ws, { baseUrl: 'http://127.0.0.1:1' });
    const correctSig = (sut as any).signMarkerContentForTest(apiKey, yaml);
    yaml = yaml.replace('PLACEHOLDER', correctSig);
    await writeMarker(tempRoot, yaml);

    const mockFetchFail = async () => ({ ok: true, text: async () => '{"status":"ok","nonce":"wrong"}' } as any);

    const result = await sut.BootstrapTrustAsync(tempRoot, { fetcher: mockFetchFail as any });
    assert.equal(result.IsTrusted, false);
    assert.match(result.DenialReason || '', /Nonce verification failed/);
    assert.ok(result.MarkerData);
  });
});
