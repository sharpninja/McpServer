/**
 * @qbrainai/qbrain-ai-plugin-core
 *
 * Canonical shared transport, marker-trust, cache, and session-log
 * infrastructure for the QBrainAi Node plugins (cline v1, cline-v2,
 * opencode). Host plugins construct the core with their identity and keep
 * only SDK glue in their own repos (see lib-node/README.md).
 */
export {
  type QBrainAiPluginCoreConfig,
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
export { ReplBridge, type ReplResponse, type ReplBridgeOptions } from './transport/repl-bridge.js';
export { CoreRuntimeError, type CoreRuntimeFailureCode } from './runtime/core-runtime-error.js';
export {
  fullBootstrap,
  findMarkerFile,
  parseMarkerField,
  type MarkerContext,
  type BootstrapOptions,
} from './discovery/marker-resolver.js';
export type { RequiredAgentPluginBinding, VerifiedAgentPluginContract } from './discovery/agent-plugin-contract.js';
export { verifyPinnedPluginArtifact, type PinnedPluginArtifactOptions, type VerifiedPluginArtifact } from './discovery/plugin-artifact.js';
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
import type { QBrainAiPluginCoreConfig } from './runtime/core-config.js';

/** Factory: configure the core for a host plugin and return its context. */
export function createQBrainAiPluginCore(config: QBrainAiPluginCoreConfig = {}): HostContext {
  return new HostContext(config);
}

export {
  readWorkspaceVerificationPolicy,
  type VerificationPolicyBinding,
  type VerificationPolicyCheck,
  type WorkspaceVerificationPolicyDocument,
  type VerifiedWorkspacePolicy,
} from './runtime/verification-policy.js';
export { classifyOperationFailure, CoreOperationError, FINAL_OPERATION_CODES, type OperationFailure } from './runtime/operation-failure.js';

export {
  AuditOutbox, AuditStorageError,
  type AuditContext, type AuditEvent, type AuditReceipt, type AuditTransport,
  type AuditEnqueueResult, type AuditFlushResult, type AuditOutboxOptions, type AuditAliasRecord,
} from './cache/audit-outbox.js';
export { AuditAliasConsumer, type AuditAliasSource } from './cache/audit-alias-consumer.js';

/**
 * @deprecated 1.x alias of {@link createQBrainAiPluginCore}, kept through 1.x for host plugins
 * built before the QBrainAi rename (TR-MCP-QBRAIN-003). New code calls createQBrainAiPluginCore.
 */
export const createMcpServerPluginCore = createQBrainAiPluginCore;
