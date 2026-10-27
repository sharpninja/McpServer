import * as fs from 'fs';
import * as path from 'path';
import * as crypto from 'crypto';
import { JSON_SCHEMA, load } from 'js-yaml';
import { checkAgentPluginContractSnapshot, type RequiredAgentPluginBinding, type VerifiedAgentPluginContract } from './agent-plugin-contract.js';

const MARKER_FILENAME = 'AGENTS-README-FIRST.yaml';
const MAX_WALK_DEPTH = 20;

export interface MarkerContext {
  markerFile: string;
  baseUrl: string;
  apiKey: string;
  workspace: string;
  workspacePath: string;
  port: string;
  /** Present only for an explicitly required, authenticated agent contract. */
  readonly agentPluginContract?: VerifiedAgentPluginContract;
}

/** TR-QB-TRUST-001: network and workspace binding supplied by the owning host. */
export interface BootstrapOptions {
  /** Host's proxy-aware fetch implementation; required when workspace binding is requested. */
  fetch?: typeof globalThis.fetch;
  /** Reject a marker for a different execution workspace before contacting its server. */
  expectedWorkspacePath?: string;
  /** Host-owned exact identity and artifact pin. Requires explicit workspace binding and fetch. */
  requiredAgentPlugin?: RequiredAgentPluginBinding;
}

/**
 * Walk up from startDir looking for AGENTS-README-FIRST.yaml.
 * Returns the full path or null if not found.
 * Mirrors find_marker_file() in lib/marker-resolver.sh.
 */
export function findMarkerFile(startDir: string): string | null {
  let dir = path.resolve(startDir);
  for (let depth = 0; depth < MAX_WALK_DEPTH; depth++) {
    const candidate = path.join(dir, MARKER_FILENAME);
    if (fs.existsSync(candidate)) {
      return candidate;
    }
    const parent = path.dirname(dir);
    if (parent === dir) break; // filesystem root
    dir = parent;
  }
  // Check root
  if (fs.existsSync('/' + MARKER_FILENAME)) return '/' + MARKER_FILENAME;
  return null;
}

/** Decode YAML scalars before authentication; quote escapes are not signed data. */
export function parseMarkerField(markerFile: string, fieldName: string): string | null {
  return markerField(parseMarkerSnapshot(fs.readFileSync(markerFile, 'utf8')), fieldName);
}

type MarkerDocument = Record<string, unknown>;
function mapping(value: unknown): MarkerDocument | null {
  return value !== null && typeof value === 'object' && !Array.isArray(value)
    ? value as MarkerDocument : null;
}
function scalar(value: unknown): string | null {
  return typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean'
    ? String(value) : null;
}
function parseMarkerSnapshot(raw: string): MarkerDocument {
  try {
    // Keep timestamps as strings and reject duplicate keys. Never expose YAML error snippets.
    const document = mapping(load(raw, { schema: JSON_SCHEMA }));
    if (!document) throw new Error();
    return document;
  } catch {
    throw new Error('MCP_UNTRUSTED: Invalid marker YAML');
  }
}
function markerField(document: MarkerDocument, fieldName: string): string | null {
  return scalar(document[fieldName]) ?? scalar(mapping(document.endpoints)?.[fieldName]);
}
function nestedField(document: MarkerDocument, section: string, field: string): string | null {
  return scalar(mapping(document[section])?.[field]);
}

/**
 * Build the canonical payload for HMAC verification.
 * Field order MUST match the bash version in lib/marker-resolver.sh (marker-v1).
 */
function buildCanonicalPayload(document: MarkerDocument): string {
  const get = (f: string) => markerField(document, f) ?? '';
  const lines: string[] = [
    'canonicalization=marker-v1',
    `port=${get('port')}`,
    `baseUrl=${get('baseUrl')}`,
    `apiKey=${get('apiKey')}`,
    `workspace=${get('workspace')}`,
    `workspacePath=${get('workspacePath')}`,
    `pid=${get('pid')}`,
    `startedAt=${get('startedAt')}`,
    `markerWrittenAtUtc=${get('markerWrittenAtUtc')}`,
    `serverStartedAtUtc=${get('serverStartedAtUtc')}`,
  ];

  // Preserve marker-v1 endpoint emission order, using decoded scalar values.
  for (const [key, value] of Object.entries(mapping(document.endpoints) ?? {})) {
    const decoded = scalar(value);
    if (decoded === null) throw new Error('MCP_UNTRUSTED: Invalid endpoint scalar');
    lines.push(`endpoints.${key}=${decoded}`);
  }

  const agentPluginPolicy = nestedField(document, 'agent_plugins', 'policy');
  const agentPluginDigest = nestedField(document, 'agent_plugins', 'contract_digest');
  if (agentPluginPolicy !== null || agentPluginDigest !== null) {
    lines.push(`agentPlugins.policy=${agentPluginPolicy ?? ''}`);
    lines.push(`agentPlugins.contractDigest=${agentPluginDigest ?? ''}`);
  }

  // CRITICAL: Use LF line endings, same as bash echo -n behavior
  return lines.map(line => line.replace(/\r\n|[\r\n\f\u0085\u2028\u2029]/g, '\n')).join('\n') + '\n';
}

/**
 * Verify the HMAC-SHA256 signature in the marker file.
 * Mirrors verify_signature() in lib/marker-resolver.sh.
 */
export function verifySignature(markerFile: string): boolean {
  try {
    return verifySignatureSnapshot(parseMarkerSnapshot(fs.readFileSync(markerFile, 'utf8')));
  } catch {
    return false;
  }
}

function verifySignatureSnapshot(document: MarkerDocument): boolean {
  const apiKey = markerField(document, 'apiKey');
  if (!apiKey) {
    process.stderr.write('[marker] No apiKey found in marker file\n');
    return false;
  }

  const storedSignature = nestedField(document, 'signature', 'value');
  if (!storedSignature) {
    process.stderr.write('[marker] No signature value found in marker file\n');
    return false;
  }

  const payload = buildCanonicalPayload(document);
  const computed = crypto
    .createHmac('sha256', apiKey)
    .update(payload)
    .digest('hex')
    .toUpperCase();

  if (computed === storedSignature.toUpperCase()) {
    return true;
  }
  process.stderr.write(
    '[marker] Signature mismatch\n',
  );
  return false;
}

/**
 * Orchestrate: find marker -> parse -> verify -> health nonce check.
 * Returns MarkerContext or throws.
 * Mirrors full_bootstrap() in lib/marker-resolver.sh.
 */
export async function fullBootstrap(
  startDir: string = process.cwd(),
  options: BootstrapOptions = {},
): Promise<MarkerContext> {
  const markerFile = findMarkerFile(startDir);
  if (!markerFile) {
    throw new Error('MCP_UNTRUSTED: No marker file found');
  }

  const snapshot = fs.readFileSync(markerFile, 'utf8');
  const document = parseMarkerSnapshot(snapshot);
  if (!verifySignatureSnapshot(document)) {
    throw new Error('MCP_UNTRUSTED: Signature verification failed');
  }

  const baseUrl = markerField(document, 'baseUrl') ?? '';
  const apiKey = markerField(document, 'apiKey') ?? '';
  const workspace = markerField(document, 'workspace') ?? '';
  const workspacePath = markerField(document, 'workspacePath') ?? '';
  const port = markerField(document, 'port') ?? '';

  if (options.requiredAgentPlugin && options.expectedWorkspacePath === undefined) {
    throw new Error('MCP_UNTRUSTED: Required agent contract needs explicit workspace binding');
  }
  if (options.expectedWorkspacePath !== undefined) {
    if (typeof options.fetch !== 'function') throw new Error('MCP_UNTRUSTED: Governed bootstrap requires injected fetch');
    if (!path.isAbsolute(workspacePath) || !path.isAbsolute(options.expectedWorkspacePath) || path.resolve(workspacePath) !== path.resolve(options.expectedWorkspacePath)) {
      throw new Error('MCP_UNTRUSTED: Marker workspace does not match the execution workspace');
    }
  }
  const agentPluginContract = options.requiredAgentPlugin
    ? checkAgentPluginContractSnapshot(snapshot, { ...options.requiredAgentPlugin }) : undefined;
  const fetcher = options.fetch ?? globalThis.fetch;

  // Health nonce check
  const nonce = `nonce-${crypto.randomUUID()}`;
  const healthUrl = `${baseUrl}/health?nonce=${nonce}`;
  try {
    const resp = await fetcher(healthUrl, { redirect: 'error', signal: AbortSignal.timeout(15000) });
    if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
    const body = await resp.json() as Record<string, unknown>;
    if (body.nonce !== nonce) {
      throw new Error('MCP_UNTRUSTED: Nonce verification failed');
    }
  } catch (e) {
    throw new Error(`MCP_UNTRUSTED: Health check failed: ${e}`);
  }

  if (fs.readFileSync(markerFile, 'utf8') !== snapshot) {
    throw new Error('MCP_UNTRUSTED: Marker changed during bootstrap; retry against the current signed marker');
  }
  return { markerFile, baseUrl, apiKey, workspace, workspacePath, port,
    ...(agentPluginContract ? { agentPluginContract } : {}) };
}
