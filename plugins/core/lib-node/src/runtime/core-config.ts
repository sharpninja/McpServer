import type { ReplBridge } from '../transport/repl-bridge.js';

/**
 * Host configuration for @qbrainai/qbrain-ai-plugin-core.
 *
 * HostContext captures its own immutable copy. The legacy setCoreConfig()
 * helpers below still support standalone consumers; governed hosts require
 * explicit identity and workspace and do not publish process-wide values.
 */
export interface QBrainAiPluginCoreConfig {
  /** TR-QB-TRUST-001: enforce host-owned REPL launch settings and workspace binding. */
  governed?: boolean;
  /** Host-owned proxy-aware fetch; mandatory for governed marker bootstrap. */
  fetch?: typeof globalThis.fetch;
  /** Display agent name, e.g. Cline, OpenCode (session_open agent field, log prefixes). */
  agentName?: string;
  /** Lowercase plugin id, e.g. cline, cline-v2, opencode (cache path segments, error codes). */
  pluginId?: string;
  /** Session title override for session_open. */
  sessionTitle?: string;
  /** Absolute workspace required for governed hosts; legacy hosts may discover it. */
  workspacePath?: string;
  /** Exclusively owned ReplBridge; one is created when omitted. Cannot be shared by hosts. */
  bridge?: ReplBridge;
  /** Legacy only: set false to skip marker bootstrap. Governed hosts reject false. */
  autoBootstrap?: boolean;
  /** Legacy only: set false to skip failsafe replay. Governed hosts reject false. */
  autoFlushCache?: boolean;
  /** Positive finite transport request timeout in milliseconds. */
  toolTimeoutMs?: number;
  /** Opt-in pre-dispatch JSON-schema validation (tools/schema-validation.ts). */
  validateArguments?: boolean;
  /** Pinned absolute executable in governed mode; only legacy mode honors ambient overrides. */
  replCommand?: string;
}

/**
 * @deprecated 1.x alias of {@link QBrainAiPluginCoreConfig}, kept through 1.x for host plugins
 * built before the QBrainAi rename (TR-MCP-QBRAIN-003).
 */
export type McpServerPluginCoreConfig = QBrainAiPluginCoreConfig;

let activeConfig: QBrainAiPluginCoreConfig = {};

export function setCoreConfig(config: QBrainAiPluginCoreConfig): void {
  activeConfig = { ...config };
  publishCoreIdentity(activeConfig);
}

export function getCoreConfig(): QBrainAiPluginCoreConfig {
  return activeConfig;
}

export function coreAgentName(): string {
  return activeConfig.agentName || process.env.PLUGIN_AGENT_NAME || 'Cline';
}

export function corePluginId(): string {
  return activeConfig.pluginId || process.env.PLUGIN_TAG || 'cline-v2';
}

function publishCoreIdentity(config: QBrainAiPluginCoreConfig): void {
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
