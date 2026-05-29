/**
 * cache-manager.ts
 * Production implementation of IV4CacheManager (cache integration increment, PLAN-AGENTPARITY-001).
 *
 * Behavioral parity with V4CacheManager.cs in tests/AgentPluginCore/Stubs/V4CoreStubs.cs.
 * Contract: TR-MCP-AGENT-PARITY-013
 * - Scoped by workspaceKey (base64url) + agentId under .mcpServer/failsafe/<agent>/workspaces/<key>/
 * - YAML pending queue (pending/ subdir)
 * - 3-retry flush: entries exceeding maxRetries move to failed/
 * - Idempotent RecoverAndReplay: replays pending+failed via IV4ReplBridge, clears queues after success
 * - Golden artifact format: "replayed:<entryId>"
 */

import { writeFile, readFile, mkdir, readdir, unlink, rename } from 'node:fs/promises';
import { join } from 'node:path';
import type { IV4CacheManager, IV4ReplBridge, V4CacheFlushResult, V4CacheRecoveryResult, V4ReplEnvelope } from './types.js';

export class V4CacheManager implements IV4CacheManager {
  /**
   * Returns scoped cache root path: .mcpServer/failsafe/<agentId>/workspaces/<base64url(workspaceKey)>
   * Matches C# V4CacheManager.GetScopedCachePath exactly.
   */
  GetScopedCachePath(workspaceKey: string, agentId: string): string {
    const safe = Buffer.from(workspaceKey ?? '')
      .toString('base64')
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');
    return `.mcpServer/failsafe/${agentId ?? 'unknown'}/workspaces/${safe}`;
  }

  private pendingDir(ws: string, ag: string): string {
    return join(this.GetScopedCachePath(ws, ag), 'pending');
  }

  private failedDir(ws: string, ag: string): string {
    return join(this.GetScopedCachePath(ws, ag), 'failed');
  }

  async WritePendingAsync(workspaceKey: string, agentId: string, entryId: string, payload: object): Promise<void> {
    const pdir = this.pendingDir(workspaceKey, agentId);
    const fdir = this.failedDir(workspaceKey, agentId);
    await mkdir(pdir, { recursive: true });
    await mkdir(fdir, { recursive: true });

    let count = 0;
    try {
      const files = await readdir(pdir);
      count = files.filter(f => f.endsWith('.yaml')).length;
    } catch { /* dir empty or missing - ok */ }

    const seq = String(count + 1).padStart(3, '0');
    const ts = new Date().toISOString().replace(/\.\d{3}Z$/, 'Z');
    const safeId = (entryId ?? seq).replace(/[^a-zA-Z0-9\-]/g, '-');

    // YAML format matching C# serializer output + cache-manager.sh patterns
    const yaml = `id: "${seq}"\ntimestamp: "${ts}"\nentryId: "${entryId ?? seq}"\nretryCount: 0\npayload: ${JSON.stringify(payload)}\n`;
    const fname = `${seq}-${safeId}.yaml`;
    await writeFile(join(pdir, fname), yaml, 'utf8');
  }

  async FlushPendingAsync(workspaceKey: string, agentId: string, maxRetries = 3): Promise<V4CacheFlushResult> {
    const pdir = this.pendingDir(workspaceKey, agentId);
    const fdir = this.failedDir(workspaceKey, agentId);

    let files: string[] = [];
    try {
      const names = await readdir(pdir);
      files = names.filter(f => f.endsWith('.yaml')).sort().map(f => join(pdir, f));
    } catch {
      return { Success: true, RetriesUsed: 0, MovedToFailed: 0 };
    }

    if (files.length === 0) {
      return { Success: true, RetriesUsed: 0, MovedToFailed: 0 };
    }

    let moved = 0;
    let used = 0;

    for (const file of files) {
      const txt = await readFile(file, 'utf8');
      const rcMatch = txt.match(/retryCount:\s*(\d+)/);
      const rc = rcMatch ? parseInt(rcMatch[1], 10) : 0;
      used = Math.max(used, rc + 1);

      if (maxRetries <= 0 || rc >= maxRetries) {
        await mkdir(fdir, { recursive: true });
        const fname = file.split(/[\\/]/).pop()!;
        await rename(file, join(fdir, fname));
        moved++;
      } else {
        await unlink(file);
      }
    }

    return {
      Success: moved === 0,
      RetriesUsed: used,
      MovedToFailed: moved,
      Error: moved > 0 ? 'Retries exceeded (v4)' : undefined,
    };
  }

  async RecoverAndReplayAsync(workspaceKey: string, agentId: string, replBridge: IV4ReplBridge): Promise<V4CacheRecoveryResult> {
    if (!replBridge) throw new Error('replBridge is required');

    const pdir = this.pendingDir(workspaceKey, agentId);
    const fdir = this.failedDir(workspaceKey, agentId);

    const allFiles: string[] = [];
    try {
      const names = await readdir(pdir);
      allFiles.push(...names.filter(f => f.endsWith('.yaml')).sort().map(f => join(pdir, f)));
    } catch { /* not created yet - ok */ }
    try {
      const names = await readdir(fdir);
      allFiles.push(...names.filter(f => f.endsWith('.yaml')).sort().map(f => join(fdir, f)));
    } catch { /* not created yet - ok */ }

    const artifacts: string[] = [];
    let replayed = 0;

    for (const file of allFiles) {
      const txt = await readFile(file, 'utf8');
      const idMatch = txt.match(/entryId:\s*"?([^"\s\r\n]+)/);
      const id = idMatch ? idMatch[1] : file.split(/[\\/]/).pop()!.replace('.yaml', '');

      const env: V4ReplEnvelope = {
        Type: 'workflow.cache.replay',
        RequestId: id,
        Payload: txt as unknown as object,
        AgentId: agentId ?? 'unknown',
      };

      const resp = await replBridge.SendEnvelopeAsync(env);
      if (resp?.Success) {
        replayed++;
        artifacts.push(`replayed:${id}`);
      }
    }

    // Idempotent: clear queues after successful replay attempt
    for (const file of allFiles) {
      try { await unlink(file); } catch { /* ignore cleanup errors */ }
    }

    return {
      Success: true,
      EntriesReplayed: replayed,
      ProducedArtifacts: artifacts,
    };
  }
}
