export interface ReplResponse {
    type: 'result' | 'error' | 'event';
    payload: Record<string, unknown>;
}
/**
 * Persistent bridge to mcpserver-repl --agent-stdio.
 * Multiplexes concurrent JSON-over-STDIO requests by requestId.
 */
export declare class ReplBridge {
    private proc;
    private pending;
    private buffer;
    private docBuffer;
    /** Generate a request ID matching ^req-\d{8}T\d{6}Z-[a-z0-9]+$ */
    static generateRequestId(slug?: string): string;
    /** Ensure the REPL process is running, restarting if it crashed. */
    ensure(): Promise<void>;
    private terminateAfterTimeout;
    private onLine;
    private parseDocument;
    /**
     * Send a single-line JSON envelope and await the matching result/error envelope.
     */
    invoke(method: string, params?: Record<string, unknown>): Promise<ReplResponse>;
    /**
     * Invoke a streaming method, calling onEvent for each progress event.
     */
    invokeStreaming(method: string, params: Record<string, unknown>, onEvent: (event: ReplResponse) => void): Promise<ReplResponse>;
    /** Gracefully terminate the REPL process. */
    close(): Promise<void>;
}
