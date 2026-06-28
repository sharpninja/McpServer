import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as yaml from 'js-yaml';
import { corePluginId } from '../runtime/core-config.js';
import type { ReplBridge } from '../transport/repl-bridge.js';

const RETRY_CAP = 3;

interface CacheEntry {
  id?: string;
  timestamp?: string;
  method?: string;
  params?: Record<string, unknown>;
  retryCount?: number;
}

export interface CacheFlushResult {
  flushed: number;
  failed: number;
  pending: number;
}

function failsafeDir(): string {
  const override = process.env.MCPSERVER_FAILSAFE_DIR || process.env.MCP_FAILSAFE_DIR;
  if (override && override.trim().length > 0) return override;

  return path.join(os.homedir(), '.mcpServer', 'failsafe', corePluginId());
}

function safeSegment(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9._-]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 80);
}

async function ensureFailsafeDir(): Promise<string> {
  const dir = failsafeDir();
  await fs.promises.mkdir(dir, { recursive: true });
  return dir;
}

async function listCacheFiles(dir: string): Promise<string[]> {
  try {
    const names = await fs.promises.readdir(dir);
    return names
      .filter((name) => name.endsWith('.yaml'))
      .sort()
      .map((name) => path.join(dir, name));
  } catch (error) {
    if ((error as NodeJS.ErrnoException).code === 'ENOENT') return [];
    throw error;
  }
}

async function readEntry(filePath: string): Promise<CacheEntry | null> {
  try {
    const parsed = yaml.load(await fs.promises.readFile(filePath, 'utf8'));
    return parsed && typeof parsed === 'object' ? (parsed as CacheEntry) : null;
  } catch {
    return null;
  }
}

async function writeEntry(filePath: string, entry: CacheEntry): Promise<void> {
  await fs.promises.writeFile(filePath, yaml.dump(entry), 'utf8');
}

export async function cacheWrite(
  method: string,
  params?: Record<string, unknown>,
): Promise<string> {
  const dir = await ensureFailsafeDir();
  const id = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  const fileName = `${id}-${safeSegment(method) || 'operation'}.yaml`;
  const filePath = path.join(dir, fileName);

  await writeEntry(filePath, {
    id,
    timestamp: new Date().toISOString(),
    method,
    params,
    retryCount: 0,
  });

  return filePath;
}

export async function cacheDelete(filePath: string): Promise<void> {
  try {
    await fs.promises.rm(filePath, { force: true });
  } catch (error) {
    if ((error as NodeJS.ErrnoException).code !== 'ENOENT') throw error;
  }
}

export async function cacheStatus(): Promise<number> {
  return (await listCacheFiles(failsafeDir())).length;
}

export async function cacheFlush(bridge: ReplBridge): Promise<CacheFlushResult> {
  const files = await listCacheFiles(failsafeDir());
  let flushed = 0;
  let failed = 0;

  for (const filePath of files) {
    const entry = await readEntry(filePath);
    if (!entry?.method) {
      failed += 1;
      continue;
    }

    const retryCount = Number.isFinite(entry.retryCount) ? Number(entry.retryCount) : 0;
    if (retryCount >= RETRY_CAP) continue;

    try {
      const result = await bridge.invoke(entry.method, entry.params);
      if (result.type === 'error') {
        await writeEntry(filePath, { ...entry, retryCount: retryCount + 1 });
        failed += 1;
        continue;
      }

      await cacheDelete(filePath);
      flushed += 1;
    } catch {
      await writeEntry(filePath, { ...entry, retryCount: retryCount + 1 });
      failed += 1;
    }
  }

  return { flushed, failed, pending: await cacheStatus() };
}
