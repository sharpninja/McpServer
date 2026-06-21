/**
 * marker-trust.ts
 * Production implementation of IV4MarkerTrustService (marker slice only).
 * Exact behavioral parity with mcpserver-codex-plugin/lib/marker-resolver.sh
 * (find upward, parse_marker_field, canonical payload construction, openssl hmac-sha256 upper hex,
 * nonce health, MCP_UNTRUSTED exact strings + env exports on success).
 * 
 * DI for fs/fetch enables pure unit tests (Byrd mocks).
 * No other v4 contracts implemented in this minimal slice.
 */

import { createHmac } from 'node:crypto';
import { readFile, stat } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import type { IV4MarkerData, IV4TrustResult, IV4MarkerTrustService, MarkerTrustOptions } from './types.js';

const MARKER_FILENAME = 'AGENTS-README-FIRST.yaml';
const MAX_WALK_DEPTH = 20;

function normalizeLine(line: string): string {
  return line.replace(/\r$/, '');
}

async function defaultFileExists(p: string): Promise<boolean> {
  try {
    const s = await stat(p);
    return s.isFile();
  } catch {
    return false;
  }
}

async function defaultReadText(p: string): Promise<string> {
  return readFile(p, 'utf8');
}

export class V4MarkerTrustService implements IV4MarkerTrustService {
  private readonly _fsExists: (p: string) => Promise<boolean>;
  private readonly _fsRead: (p: string) => Promise<string>;
  private readonly _fetch: typeof fetch;

  constructor(options: MarkerTrustOptions = {}) {
    this._fsExists = options.fs?.stat ? (p) => options.fs!.stat!(p).then(s => s.isFile()).catch(() => false) : defaultFileExists;
    this._fsRead = options.fs?.readFile ? (p: string) => options.fs!.readFile!(p, 'utf8') : defaultReadText;
    this._fetch = options.fetcher ?? (globalThis.fetch as typeof fetch);
  }

  async FindMarkerFileAsync(startPath: string, _ct?: AbortSignal): Promise<string | null> {
    let dir = resolve(startPath);
    const seen = new Set<string>();
    let depth = 0;

    while (dir && dir !== '/' && !seen.has(dir) && depth < MAX_WALK_DEPTH) {
      seen.add(dir);
      const candidate = join(dir, MARKER_FILENAME);
      if (await this._fsExists(candidate)) {
        return candidate;
      }
      const parent = dirname(dir);
      if (parent === dir) break;
      dir = parent;
      depth++;
    }

    // root check (sh behavior)
    const rootCandidate = '/' + MARKER_FILENAME;
    if (await this._fsExists(rootCandidate)) {
      return rootCandidate;
    }
    return null;
  }

  async VerifySignatureAndParseAsync(markerPath: string, _ct?: AbortSignal): Promise<IV4MarkerData> {
    const content = await this._fsRead(markerPath);
    const data = this.parseMarkerYaml(content);

    if (!data.ApiKey) {
      throw new Error('MCP_UNTRUSTED: missing apiKey in marker');
    }
    if (!data.Signature) {
      throw new Error('MCP_UNTRUSTED: No signature value found in marker file');
    }

    const payload = this.buildCanonicalPayload(content);
    const computed = this.computeHmacUpper(data.ApiKey, payload);

    if (computed !== data.Signature.toUpperCase()) {
      throw new Error(`MCP_UNTRUSTED: Signature verification failed (computed=${computed}, stored=${data.Signature})`);
    }

    return data;
  }

  async PerformNonceHealthChallengeAsync(marker: IV4MarkerData, options?: { fetcher?: typeof fetch }): Promise<boolean> {
    if (!marker || !marker.ServerUrl) {
      return false;
    }
    const nonce = marker.Nonce || `nonce-${Date.now()}-${Math.floor(Math.random() * 100000)}`;
    const base = marker.ServerUrl.replace(/\/$/, '');
    const url = `${base}/health?nonce=${encodeURIComponent(nonce)}`;

    const fetchImpl = options?.fetcher ?? this._fetch;
    if (!fetchImpl) {
      return false;
    }

    try {
      const res = await fetchImpl(url, { method: 'GET' } as any);
      if (!res || !res.ok) return false;
      const text = await res.text();
      return text.includes(`"nonce":"${nonce}"`) || text.includes(`"nonce": "${nonce}"`);
    } catch {
      return false;
    }
  }

  async BootstrapTrustAsync(workspacePath: string, options?: { fetcher?: typeof fetch }): Promise<IV4TrustResult> {
    const markerPath = await this.FindMarkerFileAsync(workspacePath);
    if (!markerPath) {
      return {
        IsTrusted: false,
        TrustMethod: 'MCP_UNTRUSTED',
        DenialReason: 'MCP_UNTRUSTED: No marker file found'
      };
    }

    try {
      const markerData = await this.VerifySignatureAndParseAsync(markerPath);

      const nonceOk = await this.PerformNonceHealthChallengeAsync(markerData, options);
      if (!nonceOk) {
        return {
          IsTrusted: false,
          TrustMethod: 'MCP_UNTRUSTED',
          DenialReason: 'MCP_UNTRUSTED: Nonce verification failed',
          MarkerData: markerData
        };
      }

      // Success: set exact env vars per sh full_bootstrap
      const baseUrl = markerData.ServerUrl;
      (process.env as any).MCPSERVER_BASE_URL = baseUrl;
      (process.env as any).MCPSERVER_API_KEY = markerData.ApiKey;
      (process.env as any).MCPSERVER_WORKSPACE = markerData.Metadata['workspace'] || '';
      (process.env as any).MCPSERVER_WORKSPACE_PATH = markerData.WorkspacePath;

      return {
        IsTrusted: true,
        TrustMethod: 'signature_verified+nonce',
        MarkerData: markerData
      };
    } catch (err: any) {
      const msg = String(err?.message || err);
      return {
        IsTrusted: false,
        TrustMethod: 'MCP_UNTRUSTED',
        DenialReason: msg.startsWith('MCP_UNTRUSTED') ? msg : `MCP_UNTRUSTED: ${msg}`
      };
    }
  }

  // --- exact ports of sh helpers ---

  private parseMarkerField(content: string, fieldName: string): string | null {
    const lines = content.split(/\r?\n/);
    // top-level first
    for (const raw of lines) {
      const line = normalizeLine(raw);
      const m = new RegExp(`^${fieldName}:[\\s]*(\\S.*)?$`).exec(line);
      if (m) {
        let v = (m[1] || '').trim();
        v = v.replace(/^"(.*)"$/, '$1').replace(/^'(.*)'$/, '$1');
        return v || null;
      }
    }
    // nested under endpoints:
    let inEndpoints = false;
    for (const raw of lines) {
      const line = normalizeLine(raw);
      if (/^endpoints:/.test(line)) {
        inEndpoints = true;
        continue;
      }
      if (inEndpoints) {
        if (/^[^\s]/.test(line)) break;
        const m = new RegExp(`^[\\s]+${fieldName}:[\\s]*(\\S.*)?$`).exec(line);
        if (m) {
          let v = (m[1] || '').trim();
          v = v.replace(/^"(.*)"$/, '$1').replace(/^'(.*)'$/, '$1');
          return v || null;
        }
      }
    }
    return null;
  }

  private parseMarkerYaml(content: string): IV4MarkerData {
    const lines = content.split(/\r?\n/);
    const dict: Record<string, string> = {};

    // top level simple parse (sufficient for marker fields)
    for (const raw of lines) {
      const line = normalizeLine(raw);
      const m = /^([A-Za-z0-9_.-]+):[\s]*(.*)$/.exec(line);
      if (m) {
        let v = m[2].trim();
        v = v.replace(/^"(.*)"$/, '$1').replace(/^'(.*)'$/, '$1');
        dict[m[1]] = v;
      }
    }

    // endpoints.*
    let inEndpoints = false;
    for (const raw of lines) {
      const line = normalizeLine(raw);
      if (/^endpoints:/.test(line)) { inEndpoints = true; continue; }
      if (inEndpoints && /^[ \t]/.test(line)) {
        const m = /^[ \t]+([A-Za-z0-9_.-]+):[\s]*(.*)$/.exec(line);
        if (m) {
          let v = m[2].trim().replace(/^"(.*)"$/, '$1').replace(/^'(.*)'$/, '$1');
          dict[`endpoints.${m[1]}`] = v;
        }
      } else if (inEndpoints && /^[^ \t]/.test(line)) {
        break;
      }
    }

    // agent_plugins (optional, for future)
    let inAgent = false;
    for (const raw of lines) {
      const line = normalizeLine(raw);
      if (/^agent_plugins:/.test(line)) { inAgent = true; continue; }
      if (inAgent && /^[ \t]/.test(line)) {
        const m = /^[ \t]+(policy|contract_digest):[\s]*(.*)$/.exec(line);
        if (m) {
          let v = m[2].trim().replace(/^"(.*)"$/, '$1').replace(/^'(.*)'$/, '$1');
          dict[`agentPlugins.${m[1] === 'contract_digest' ? 'contractDigest' : m[1]}`] = v;
        }
      } else if (inAgent && /^[^ \t]/.test(line)) {
        break;
      }
    }

    // signature value (indented under signature:)
    let storedSig = '';
    let inSig = false;
    for (const raw of lines) {
      const line = normalizeLine(raw);
      if (/^signature:/.test(line)) { inSig = true; continue; }
      if (inSig) {
        if (/^[^\s]/.test(line)) break;
        const m = /^[ \t]+value:[\s]*(.*)$/.exec(line);
        if (m) {
          storedSig = m[1].trim().replace(/^"(.*)"$/, '$1').replace(/^'(.*)'$/, '$1');
          break;
        }
      }
    }

    const baseUrl = dict['baseUrl'] || dict['serverUrl'] || 'http://localhost:5177';
    const wsPath = dict['workspacePath'] || process.cwd();

    return {
      WorkspacePath: wsPath,
      ServerUrl: baseUrl,
      ApiKey: dict['apiKey'] || '',
      Signature: storedSig || null,
      Nonce: dict['nonce'] || null,
      Metadata: { ...dict }
    };
  }

  private buildCanonicalPayload(markerContent: string): string {
    const lines = markerContent.split(/\r?\n/);
    let payload = '';

    const add = (k: string, v: string) => {
      payload += `${k}=${v}\n`;
    };

    const apiKey = this.parseMarkerField(markerContent, 'apiKey') || '';
    const port = this.parseMarkerField(markerContent, 'port') || '';
    const baseUrl = this.parseMarkerField(markerContent, 'baseUrl') || '';
    const workspace = this.parseMarkerField(markerContent, 'workspace') || '';
    const workspacePath = this.parseMarkerField(markerContent, 'workspacePath') || '';
    const pid = this.parseMarkerField(markerContent, 'pid') || '';
    const startedAt = this.parseMarkerField(markerContent, 'startedAt') || '';
    const markerWritten = this.parseMarkerField(markerContent, 'markerWrittenAtUtc') || '';
    const serverStarted = this.parseMarkerField(markerContent, 'serverStartedAtUtc') || '';

    add('canonicalization', 'marker-v1');
    add('port', port);
    add('baseUrl', baseUrl);
    add('apiKey', apiKey);
    add('workspace', workspace);
    add('workspacePath', workspacePath);
    add('pid', pid);
    add('startedAt', startedAt);
    add('markerWrittenAtUtc', markerWritten);
    add('serverStartedAtUtc', serverStarted);

    // endpoints section (exact sh logic)
    let inEndpoints = false;
    for (const raw of lines) {
      const line = normalizeLine(raw);
      if (/^endpoints:/.test(line)) {
        inEndpoints = true;
        continue;
      }
      if (inEndpoints) {
        if (/^[^\s]/.test(line)) break;
        const m = /^[ \t]+([^:]+):[ \t]*(.*)$/.exec(line);
        if (m) {
          const key = m[1].trim();
          let val = m[2].trim().replace(/^"(.*)"$/, '$1').replace(/^'(.*)'$/, '$1');
          add(`endpoints.${key}`, val);
        }
      }
    }

    // agent_plugins if present (exact)
    const agentPolicy = this.parseMarkerField(markerContent, 'policy'); // will not hit top, but under section we parse crude
    // Use raw section parse for agent_plugins.policy / contractDigest
    let inAgent = false;
    let agentPolicyVal = '';
    let agentDigestVal = '';
    for (const raw of lines) {
      const line = normalizeLine(raw);
      if (/^agent_plugins:/.test(line)) { inAgent = true; continue; }
      if (inAgent && /^[ \t]/.test(line)) {
        const pm = /^[ \t]+policy:[ \t]*(.*)$/.exec(line);
        if (pm) agentPolicyVal = pm[1].trim().replace(/^"(.*)"$/, '$1').replace(/^'(.*)'$/, '$1');
        const dm = /^[ \t]+contract_digest:[ \t]*(.*)$/.exec(line);
        if (dm) agentDigestVal = dm[1].trim().replace(/^"(.*)"$/, '$1').replace(/^'(.*)'$/, '$1');
      } else if (inAgent && /^[^\s]/.test(line)) {
        break;
      }
    }
    if (agentPolicyVal || agentDigestVal) {
      add('agentPlugins.policy', agentPolicyVal);
      add('agentPlugins.contractDigest', agentDigestVal);
    }

    return payload;
  }

  private computeHmacUpper(key: string, data: string): string {
    const hmac = createHmac('sha256', key);
    hmac.update(data, 'utf8'); // echo -n equivalent (data already has the \n terminators)
    const hex = hmac.digest('hex');
    return hex.toUpperCase();
  }

  // Test helpers (exported for red-green fixture signing; not part of public contract surface)
  public computeCanonicalPayloadForTest(markerContent: string): string {
    return this.buildCanonicalPayload(markerContent);
  }

  public signMarkerContentForTest(apiKey: string, markerContent: string): string {
    const payload = this.buildCanonicalPayload(markerContent);
    return this.computeHmacUpper(apiKey, payload);
  }
}

// Re-export for convenience
export type { IV4MarkerData, IV4TrustResult, IV4MarkerTrustService, MarkerTrustOptions } from './types.js';
