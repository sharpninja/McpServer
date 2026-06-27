import type { ReplBridge } from '../transport/repl-bridge.js';

/**
 * Process-wide configuration for @sharpninja/mcpserver-plugin-core.
 *
 * Host plugins call setCoreConfig() (usually via createMcpServerPluginCore)
 * before dispatching tools so the shared modules pick up the host identity
 * instead of hardcoded 'Cline'/'cline-v2' strings. Environment variables
 * (PLUGIN_AGENT_NAME / PLUGIN_TAG) act as fallbacks so the modules also work
 * when imported standalone.
 */
export interface McpServerPluginCoreConfig {
  /** Display agent name, e.g. Cline, OpenCode (session_open agent field, log prefixes). */
  agentName?: string;
  /** Lowercase plugin id, e.g. cline, cline-v2, opencode (cache path segments, error codes). */
  pluginId?: string;
  /** Session title override for session_open. */
  sessionTitle?: string;
  /** Workspace root override; defaults to env/marker discovery. */
  workspacePath?: string;
  /** Shared ReplBridge instance; one is created when omitted. */
  bridge?: ReplBridge;
  /** Set false to skip marker bootstrap. */
  autoBootstrap?: boolean;
  /** Set false to skip failsafe replay on run start. */
  autoFlushCache?: boolean;
  /** Per-tool timeout in ms (advisory; hosts enforce). */
  toolTimeoutMs?: number;
  /** Opt-in pre-dispatch JSON-schema validation (tools/schema-validation.ts). */
  validateArguments?: boolean;
  /** Override for the REPL spawn command (MCPSERVER_REPL_COMMAND also honored). */
  replCommand?: string;
}

let activeConfig: McpServerPluginCoreConfig = {};

export function setCoreConfig(config: McpServerPluginCoreConfig): void {
  activeConfig = { ...config };
  publishCoreIdentity(activeConfig);
}

export function getCoreConfig(): McpServerPluginCoreConfig {
  return activeConfig;
}

export function coreAgentName(): string {
  return activeConfig.agentName || process.env.PLUGIN_AGENT_NAME || 'Cline';
}

export function corePluginId(): string {
  return activeConfig.pluginId || process.env.PLUGIN_TAG || 'cline-v2';
}

function publishCoreIdentity(config: McpServerPluginCoreConfig): void {
  const agentName = config.agentName || process.env.PLUGIN_AGENT_NAME || 'Cline';
  const pluginId = config.pluginId || process.env.PLUGIN_TAG || 'cline-v2';
  process.env.PLUGIN_AGENT_NAME = agentName;
  process.env.PLUGIN_AGENT_DEFAULT = agentName;
  process.env.PLUGIN_TAG = pluginId;
  process.env.MCP_AGENT_NAME = agentName;
  process.env.MCP_AGENT_ID = agentName;
  process.env.MCP_SESSION_AGENT = agentName;
  process.env.MCP_SESSION_MODEL = pluginId;
  process.env.CT2R_SOURCE_TYPE = agentName;
  process.env.CT2R_MODEL = pluginId;
  process.env.CT2R_TAGS = pluginId;
}
