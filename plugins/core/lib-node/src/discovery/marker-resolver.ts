import * as fs from 'fs';
import * as path from 'path';
import * as crypto from 'crypto';

const MARKER_FILENAME = 'AGENTS-README-FIRST.yaml';
const MAX_WALK_DEPTH = 20;

export interface MarkerContext {
  markerFile: string;
  baseUrl: string;
  apiKey: string;
  workspace: string;
  workspacePath: string;
  port: string;
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

/**
 * Extract a top-level YAML field using line-by-line parsing (no yq dependency).
 * Also handles nested fields under endpoints:.
 * Mirrors parse_marker_field() in lib/marker-resolver.sh.
 */
export function parseMarkerField(
  markerFile: string,
  fieldName: string,
): string | null {
  const lines = fs.readFileSync(markerFile, 'utf8').split('\n');

  // Try top-level field: "fieldName: value"
  for (const line of lines) {
    const m = line.match(new RegExp(`^${fieldName}:\\s*(.*)`));
    if (m) {
      return m[1].trim().replace(/^["']|["']$/g, '');
    }
  }

  // Try nested under endpoints:
  let inEndpoints = false;
  for (const line of lines) {
    if (/^endpoints:/.test(line)) {
      inEndpoints = true;
      continue;
    }
    if (inEndpoints && /^[^\s]/.test(line)) {
      inEndpoints = false;
    }
    if (inEndpoints) {
      const m = line.match(new RegExp(`^\\s+${fieldName}:\\s*(.*)`));
      if (m) {
        return m[1].trim().replace(/^["']|["']$/g, '');
      }
    }
  }

  return null;
}

function parseMarkerNestedField(
  markerFile: string,
  sectionName: string,
  fieldName: string,
): string | null {
  const lines = fs.readFileSync(markerFile, 'utf8').split('\n');
  let inSection = false;

  for (const line of lines) {
    if (new RegExp(`^${sectionName}:`).test(line)) {
      inSection = true;
      continue;
    }
    if (inSection && /^[^\s]/.test(line)) {
      break;
    }
    if (inSection) {
      const m = line.match(new RegExp(`^\\s+${fieldName}:\\s*(.*)`));
      if (m) {
        return m[1].trim().replace(/^["']|["']$/g, '');
      }
    }
  }

  return null;
}

/**
 * Build the canonical payload for HMAC verification.
 * Field order MUST match the bash version in lib/marker-resolver.sh (marker-v1).
 */
function buildCanonicalPayload(markerFile: string): string {
  const get = (f: string) => parseMarkerField(markerFile, f) ?? '';
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

  // Append endpoints in file order
  const rawLines = fs.readFileSync(markerFile, 'utf8').split('\n');
  let inEndpoints = false;
  for (const line of rawLines) {
    if (/^endpoints:/.test(line)) {
      inEndpoints = true;
      continue;
    }
    if (inEndpoints && /^[^\s]/.test(line)) break;
    if (inEndpoints) {
      const m = line.match(/^\s+([^:]+):\s*(.*)/);
      if (m) {
        const key = m[1].trim();
        const val = m[2].trim();
        lines.push(`endpoints.${key}=${val}`);
      }
    }
  }

  const agentPluginPolicy = parseMarkerNestedField(markerFile, 'agent_plugins', 'policy');
  const agentPluginDigest = parseMarkerNestedField(markerFile, 'agent_plugins', 'contract_digest');
  if (agentPluginPolicy !== null || agentPluginDigest !== null) {
    lines.push(`agentPlugins.policy=${agentPluginPolicy ?? ''}`);
    lines.push(`agentPlugins.contractDigest=${agentPluginDigest ?? ''}`);
  }

  // CRITICAL: Use LF line endings, same as bash echo -n behavior
  return lines.join('\n') + '\n';
}

/**
 * Extract the stored signature value from the marker file.
 */
function extractStoredSignature(markerFile: string): string | null {
  const rawLines = fs.readFileSync(markerFile, 'utf8').split('\n');
  let inSignature = false;
  for (const line of rawLines) {
    if (/^signature:/.test(line)) {
      inSignature = true;
      continue;
    }
    if (inSignature && /^[^\s]/.test(line)) break;
    if (inSignature) {
      const m = line.match(/^\s+value:\s*(.*)/);
      if (m) return m[1].trim();
    }
  }
  return null;
}

/**
 * Verify the HMAC-SHA256 signature in the marker file.
 * Mirrors verify_signature() in lib/marker-resolver.sh.
 */
export function verifySignature(markerFile: string): boolean {
  const apiKey = parseMarkerField(markerFile, 'apiKey');
  if (!apiKey) {
    process.stderr.write('[marker] No apiKey found in marker file\n');
    return false;
  }

  const storedSignature = extractStoredSignature(markerFile);
  if (!storedSignature) {
    process.stderr.write('[marker] No signature value found in marker file\n');
    return false;
  }

  const payload = buildCanonicalPayload(markerFile);
  const computed = crypto
    .createHmac('sha256', apiKey)
    .update(payload)
    .digest('hex')
    .toUpperCase();

  if (computed === storedSignature.toUpperCase()) {
    return true;
  }
  process.stderr.write(
    `[marker] Signature mismatch (computed=${computed}, stored=${storedSignature})\n`,
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
): Promise<MarkerContext> {
  const markerFile = findMarkerFile(startDir);
  if (!markerFile) {
    throw new Error('MCP_UNTRUSTED: No marker file found');
  }

  if (!verifySignature(markerFile)) {
    throw new Error('MCP_UNTRUSTED: Signature verification failed');
  }

  const baseUrl = parseMarkerField(markerFile, 'baseUrl') ?? '';
  const apiKey = parseMarkerField(markerFile, 'apiKey') ?? '';
  const workspace = parseMarkerField(markerFile, 'workspace') ?? '';
  const workspacePath = parseMarkerField(markerFile, 'workspacePath') ?? '';
  const port = parseMarkerField(markerFile, 'port') ?? '';

  // Health nonce check
  const nonce = `nonce-${Date.now()}-${process.pid}`;
  const healthUrl = `${baseUrl}/health?nonce=${nonce}`;
  try {
    const resp = await fetch(healthUrl);
    if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
    const body = await resp.json() as Record<string, unknown>;
    if (body.nonce !== nonce) {
      throw new Error('MCP_UNTRUSTED: Nonce verification failed');
    }
  } catch (e) {
    throw new Error(`MCP_UNTRUSTED: Health check failed: ${e}`);
  }

  return { markerFile, baseUrl, apiKey, workspace, workspacePath, port };
}
