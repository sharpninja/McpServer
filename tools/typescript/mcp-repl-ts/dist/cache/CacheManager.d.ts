import type { ReplBridge } from '../transport/ReplBridge';
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
 * Write a pending REPL command before attempting the live MCP call.
 * Mirrors cache_write() in lib/cache-manager.sh.
 */
export declare function cacheWrite(method: string, params?: Record<string, unknown>): Promise<string>;
/** Delete a pending command after the server acknowledges it. */
export declare function cacheDelete(filepath: string): Promise<void>;
/**
 * Return the count of pending YAML files.
 * Mirrors cache_status() in lib/cache-manager.sh.
 */
export declare function cacheStatus(): Promise<number>;
/**
 * Replay all pending commands via bridge.invoke(), deleting on success
 * and incrementing retryCount on failure (max MAX_RETRIES).
 * Mirrors cache_flush() in lib/cache-manager.sh.
 */
export declare function cacheFlush(bridge: ReplBridge): Promise<FlushResult>;
