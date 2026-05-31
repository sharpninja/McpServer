import * as fs from 'fs';
import * as path from 'path';
import * as yaml from 'js-yaml';
import type { ReplBridge } from '../transport/repl-bridge.js';

const MAX_RETRIES = 3;

export interface PendingEntry {
  id: string;
  timestamp: string;
  method: string;
  params: Record<string, unknown>;
  retryCount: number;
}

export interface FlushResult {
  flushed: number;
  failed: number;
  pending: number;
}

/**
 * Returns Base64URL encoding of workspacePath, matching V4CacheManager.GetScopedCachePath
 * in @sharpninja/mcpserver-agent-core (TR-MCP-AGENT-PARITY-013).
 */
function getWorkspaceKeyV4(workspacePath: string): string {
  return Buffer.from(workspacePath)
    .toString('base64')
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
}

function getPendingDir(): string {
  if (process.env.MCPSERVER_FAILSAFE_DIR) {
    return process.env.MCPSERVER_FAILSAFE_DIR;
  }

  const workspacePath = process.env.MCPSERVER_WORKSPACE_PATH ?? process.env.MCP_WORKSPACE_PATH;
  if (workspacePath) {
    const key = getWorkspaceKeyV4(workspacePath);
    return path.join(workspacePath, '.mcpServer', 'failsafe', 'cline', 'workspaces', key);
  }

  const cacheDir =
    process.env.MCPSERVER_CACHE_DIR ??
    path.join(process.cwd(), 'cache');
  return path.join(cacheDir, 'pending');
}

function ensurePendingDir(pendingDir: string): void {
  fs.mkdirSync(pendingDir, { recursive: true });
}

/**
 * Write a pending REPL command before attempting the live MCP call.
 * Mirrors cache_write() in lib/cache-manager.sh.
 */
export async function cacheWrite(
  method: string,
  params: Record<string, unknown> = {},
): Promise<string> {
  const pendingDir = getPendingDir();
  ensurePendingDir(pendingDir);

  const existing = fs
    .readdirSync(pendingDir)
    .filter((f) => f.endsWith('.yaml'));
  const seq = (existing.length + 1).toString().padStart(3, '0');
  const slug = method.replace(/\./g, '-');
  const filename = `${seq}-${slug}.yaml`;
  const filepath = path.join(pendingDir, filename);

  const entry: PendingEntry = {
    id: seq,
    timestamp: new Date().toISOString(),
    method,
    params,
    retryCount: 0,
  };

  fs.writeFileSync(filepath, yaml.dump(entry));
  return filepath;
}

/** Delete a pending command after the server acknowledges it. */
export async function cacheDelete(filepath: string): Promise<void> {
  if (filepath && fs.existsSync(filepath)) {
    fs.unlinkSync(filepath);
  }
}

/**
 * Return the count of pending YAML files.
 * Mirrors cache_status() in lib/cache-manager.sh.
 */
export async function cacheStatus(): Promise<number> {
  const pendingDir = getPendingDir();
  ensurePendingDir(pendingDir);
  return fs.readdirSync(pendingDir).filter((f) => f.endsWith('.yaml')).length;
}

/**
 * Replay all pending commands via bridge.invoke(), deleting on success
 * and incrementing retryCount on failure (max MAX_RETRIES).
 * Mirrors cache_flush() in lib/cache-manager.sh.
 */
export async function cacheFlush(bridge: ReplBridge): Promise<FlushResult> {
  const pendingDir = getPendingDir();
  ensurePendingDir(pendingDir);

  const files = fs
    .readdirSync(pendingDir)
    .filter((f) => f.endsWith('.yaml'))
    .sort()
    .map((f) => path.join(pendingDir, f));

  let flushed = 0;
  let failed = 0;

  for (const file of files) {
    if (!fs.existsSync(file)) continue;

    let entry: PendingEntry;
    try {
      entry = yaml.load(fs.readFileSync(file, 'utf8')) as PendingEntry;
    } catch {
      continue;
    }

    if ((entry.retryCount ?? 0) >= MAX_RETRIES) continue;

    try {
      const response = await bridge.invoke(entry.method, entry.params);
      if (response.type === 'error') {
        const payload = response.payload as { message?: string; code?: string };
        throw new Error(`${payload.code ?? 'error'}: ${payload.message ?? 'Unknown error'}`);
      }
      fs.unlinkSync(file);
      flushed++;
    } catch {
      entry.retryCount = (entry.retryCount ?? 0) + 1;
      fs.writeFileSync(file, yaml.dump(entry));
      failed++;
    }
  }

  const pending = await cacheStatus();
  return { flushed, failed, pending };
}
