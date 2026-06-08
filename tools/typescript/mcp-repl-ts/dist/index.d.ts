/**
 * @sharpninja/mcp-repl
 * The single shared TypeScript surface for McpServer agent plugins.
 * Used by Cline, Cline V2, OpenCode, and future TS-based plugins.
 */
export * from './types';
export { ReplBridge } from './transport/ReplBridge';
export * from './discovery/MarkerResolver';
export * from './cache/CacheManager';
export { McpAgentClient } from './client/McpAgentClient';
export { ReplClient, invokeMcpMethod } from './client/ReplClient';
