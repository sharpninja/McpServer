import { McpResult, McpError, ReplResponse } from '../types';
/**
 * Shared ReplClient for talking to mcpserver-repl --agent-stdio.
 * Provides typed request/response and automatic envelope (de)serialization.
 * This is the core of the shared TS surface.
 */
export declare class ReplClient {
    private readonly workspacePath;
    private proc;
    private readonly timeoutMs;
    constructor(workspacePath: string, timeoutMs?: number);
    connect(): Promise<void>;
    sendRequest<TParams = any, TResult = any>(method: string, params?: TParams): Promise<ReplResponse>;
    close(): Promise<void>;
}
export declare function invokeMcpMethod<TParams = any, TResult = any>(workspacePath: string, method: string, params?: TParams): Promise<McpResult<TResult> | McpError>;
