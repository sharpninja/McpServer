/**
 * @sharpninja/mcpserver-plugin-core
 *
 * Canonical shared transport, marker-trust, cache, and session-log
 * infrastructure for the McpServer Node plugins (cline v1, cline-v2,
 * opencode). Host plugins construct the core with their identity and keep
 * only SDK glue in their own repos (see lib-node/README.md).
 */
export {
  type McpServerPluginCoreConfig,
  setCoreConfig,
  getCoreConfig,
  coreAgentName,
  corePluginId,
} from './runtime/core-config.js';
export {
  HostContext,
  allToolDescriptors,
  utcStamp,
  slug,
  asRecord,
  stringValue,
  contextWorkspacePath,
  contextPrompt,
  contextModel,
  toolName,
  toolInput,
  toolError,
  contextLogger,
  setMarkerEnvironment,
} from './runtime/host-context.js';
export { ReplBridge, type ReplResponse } from './transport/repl-bridge.js';
export {
  fullBootstrap,
  findMarkerFile,
  parseMarkerField,
  type MarkerContext,
} from './discovery/marker-resolver.js';
export { cacheWrite, cacheDelete, cacheFlush, cacheStatus } from './cache/cache-manager.js';
export type { ToolDescriptor, ToolResult } from './tools/tool-descriptor.js';
export { todoTools, canHandleTodoTool, handleTodoTool } from './tools/todo.js';
export {
  sessionTools,
  canHandleSessionTool,
  handleSessionTool,
  getSessionShimState,
} from './tools/session.js';
export { memoryTools, canHandleMemoryTool, handleMemoryTool } from './tools/memory.js';
export {
  requirementsTools,
  canHandleRequirementsTool,
  handleRequirementsTool,
} from './tools/requirements.js';
export { graphragTools, canHandleGraphragTool, handleGraphragTool } from './tools/graphrag.js';
export { workspaceTools, canHandleWorkspaceTool, handleWorkspaceTool } from './tools/workspace.js';
export { usecaseTools, canHandleUseCaseTool, handleUseCaseTool } from './tools/usecase.js';
export { validateToolArguments } from './tools/schema-validation.js';

import { HostContext } from './runtime/host-context.js';
import type { McpServerPluginCoreConfig } from './runtime/core-config.js';

/** Factory: configure the core for a host plugin and return its context. */
export function createMcpServerPluginCore(config: McpServerPluginCoreConfig = {}): HostContext {
  return new HostContext(config);
}
