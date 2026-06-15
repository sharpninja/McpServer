/**
 * Shared host-plugin runtime logic extracted from the cline-v2 / opencode
 * plugin.ts files (Phase 2 reconciliation). Everything here is host-neutral;
 * the per-repo plugin.ts keeps only its SDK wiring (@cline/core createTool /
 * registerTool, opencode zod shape conversion + wrapResult, cline v1 MCP SDK
 * server + content envelope).
 */
import * as path from 'path';
import { ReplBridge } from '../transport/repl-bridge.js';
import { fullBootstrap, type MarkerContext } from '../discovery/marker-resolver.js';
import { cacheFlush } from '../cache/cache-manager.js';
import { todoTools, canHandleTodoTool, handleTodoTool } from '../tools/todo.js';
import { sessionTools, canHandleSessionTool, handleSessionTool } from '../tools/session.js';
import { memoryTools, canHandleMemoryTool, handleMemoryTool } from '../tools/memory.js';
import { brainSlotTools, canHandleBrainSlotTool, handleBrainSlotTool } from '../tools/brain-slots.js';
import {
  requirementsTools,
  canHandleRequirementsTool,
  handleRequirementsTool,
} from '../tools/requirements.js';
import { graphragTools, canHandleGraphragTool, handleGraphragTool } from '../tools/graphrag.js';
import {
  workspaceTools,
  canHandleWorkspaceTool,
  handleWorkspaceTool,
} from '../tools/workspace.js';
import { validateToolArguments } from '../tools/schema-validation.js';
import type { ToolDescriptor, ToolResult } from '../tools/tool-descriptor.js';
import {
  getCoreConfig,
  setCoreConfig,
  type McpServerPluginCoreConfig,
} from './core-config.js';

export const allToolDescriptors: ToolDescriptor[] = [
  ...workspaceTools,
  ...todoTools,
  ...sessionTools,
  ...memoryTools,
  ...brainSlotTools,
  ...requirementsTools,
  ...graphragTools,
];

export function utcStamp(date = new Date()): string {
  return (
    date.getUTCFullYear().toString() +
    (date.getUTCMonth() + 1).toString().padStart(2, '0') +
    date.getUTCDate().toString().padStart(2, '0') +
    'T' +
    date.getUTCHours().toString().padStart(2, '0') +
    date.getUTCMinutes().toString().padStart(2, '0') +
    date.getUTCSeconds().toString().padStart(2, '0') +
    'Z'
  );
}

export function slug(value: string): string {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '').slice(0, 32) || 'run';
}

export function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === 'object' && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : {};
}

export function stringValue(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : undefined;
}

export function contextWorkspacePath(value: unknown): string | undefined {
  const record = asRecord(value);
  const direct =
    stringValue(record.workspacePath) ||
    stringValue(record.workspaceRoot) ||
    stringValue(record.cwd) ||
    stringValue(record.rootPath);
  if (direct) return direct;

  const workspaceInfo = asRecord(record.workspaceInfo);
  return stringValue(workspaceInfo.rootPath) || stringValue(workspaceInfo.workspacePath);
}

export function contextPrompt(value: unknown, agentName?: string): string {
  const record = asRecord(value);
  const snapshot = asRecord(record.snapshot);
  return (
    stringValue(record.prompt) ||
    stringValue(record.input) ||
    stringValue(record.queryText) ||
    stringValue(snapshot.prompt) ||
    stringValue(snapshot.input) ||
    stringValue(snapshot.queryText) ||
    `${agentName ?? coreAgent()} run`
  );
}

export function contextModel(value: unknown): string | undefined {
  const record = asRecord(value);
  const snapshot = asRecord(record.snapshot);
  return (
    stringValue(record.model) ||
    stringValue(record.modelId) ||
    stringValue(snapshot.model) ||
    stringValue(snapshot.modelId)
  );
}

export function toolName(value: unknown): string {
  const record = asRecord(value);
  const toolCall = asRecord(record.toolCall);
  const tool = asRecord(record.tool);
  return (
    stringValue(toolCall.name) ||
    stringValue(tool.name) ||
    stringValue(record.toolName) ||
    stringValue(record.name) ||
    'unknown_tool'
  );
}

export function toolInput(value: unknown): unknown {
  const record = asRecord(value);
  if (Object.prototype.hasOwnProperty.call(record, 'input')) return record.input;
  const toolCall = asRecord(record.toolCall);
  if (Object.prototype.hasOwnProperty.call(toolCall, 'input')) return toolCall.input;
  return undefined;
}

export function toolError(value: unknown): string | undefined {
  const record = asRecord(value);
  const error = record.error;
  if (error instanceof Error) return error.message;
  if (typeof error === 'string' && error.length > 0) return error;
  const toolCall = asRecord(record.toolCall);
  const callError = toolCall.error;
  if (callError instanceof Error) return callError.message;
  if (typeof callError === 'string' && callError.length > 0) return callError;
  return undefined;
}

export function contextLogger(value: unknown): {
  warn?: (message: string) => void;
  error?: (message: string) => void;
} {
  // Return an empty object (never undefined) so callers can safely do
  // contextLogger(context).warn?.(...) on the best-effort swallow paths even
  // when the host supplies no logger.
  return (asRecord(value).logger as {
    warn?: (message: string) => void;
    error?: (message: string) => void;
  }) ?? {};
}

export function setMarkerEnvironment(marker: MarkerContext, agentName: string): void {
  process.env.MCPSERVER_BASE_URL = marker.baseUrl;
  process.env.MCPSERVER_API_KEY = marker.apiKey;
  process.env.MCPSERVER_WORKSPACE_PATH = marker.workspacePath;
  process.env.MCP_WORKSPACE_PATH = marker.workspacePath;
  process.env.MCPSERVER_WORKSPACE = marker.workspace;
  process.env.PLUGIN_AGENT_NAME = agentName;
}

function coreAgent(): string {
  return getCoreConfig().agentName || process.env.PLUGIN_AGENT_NAME || 'Cline';
}

function corePlugin(): string {
  return getCoreConfig().pluginId || process.env.PLUGIN_TAG || 'cline-v2';
}

/**
 * Host-neutral session/run controller: marker bootstrap, failsafe replay,
 * tool dispatch, and the startSession/completeSession/appendToolAction
 * session-audit choreography that was duplicated (modulo agent-name strings
 * and error codes) across cline-v2 and opencode plugin.ts.
 */
export class HostContext {
  private readonly config: McpServerPluginCoreConfig;
  readonly bridge: ReplBridge;
  private setupWorkspacePath: string | undefined;
  private bootstrappedWorkspace: string | undefined;
  private activeSessionId: string | undefined;
  private activeRequestId: string | undefined;
  private actionOrder = 0;
  private cacheFlushed = false;

  constructor(config: McpServerPluginCoreConfig = {}) {
    this.config = config;
    setCoreConfig(config);
    this.bridge = config.bridge ?? new ReplBridge();
    this.setupWorkspacePath = config.workspacePath;
    if (config.replCommand && !process.env.MCPSERVER_REPL_COMMAND) {
      process.env.MCPSERVER_REPL_COMMAND = config.replCommand;
    }
  }

  get agentName(): string {
    return this.config.agentName || coreAgent();
  }

  get pluginId(): string {
    return this.config.pluginId || corePlugin();
  }

  get workspacePath(): string | undefined {
    return this.setupWorkspacePath;
  }

  setWorkspacePath(value: string | undefined): void {
    if (value) this.setupWorkspacePath = value;
  }

  resolveWorkspacePath(context?: unknown): string {
    const workspacePath =
      this.config.workspacePath ||
      contextWorkspacePath(context) ||
      this.setupWorkspacePath ||
      process.env.MCPSERVER_WORKSPACE_PATH ||
      process.env.MCP_WORKSPACE_PATH ||
      process.cwd();
    this.setupWorkspacePath = workspacePath;
    return workspacePath;
  }

  async bootstrap(context?: unknown): Promise<MarkerContext | null> {
    const workspacePath = this.resolveWorkspacePath(context);

    if (this.config.autoBootstrap === false) return null;
    if (
      this.bootstrappedWorkspace &&
      path.resolve(this.bootstrappedWorkspace) === path.resolve(workspacePath)
    ) {
      return null;
    }

    const marker = await fullBootstrap(workspacePath);
    setMarkerEnvironment(marker, this.agentName);
    this.bootstrappedWorkspace = marker.workspacePath;
    return marker;
  }

  async bootstrapBestEffort(context?: unknown): Promise<void> {
    try {
      await this.bootstrap(context);
    } catch (error) {
      const message = `[mcpserver-${this.pluginId}] marker bootstrap failed; continuing with failsafe behavior: ${
        error instanceof Error ? error.message : String(error)
      }`;
      contextLogger(context).warn?.(message);
      process.stderr.write(`${message}\n`);
    }
  }

  async flushCacheBestEffort(context?: unknown): Promise<void> {
    if (this.config.autoFlushCache === false || this.cacheFlushed) return;
    try {
      const result = await cacheFlush(this.bridge);
      this.cacheFlushed = true;
      if (result.flushed > 0 || result.failed > 0) {
        process.stderr.write(
          `[mcpserver-${this.pluginId}] failsafe replay flushed=${result.flushed} failed=${result.failed} pending=${result.pending}\n`,
        );
      }
    } catch (error) {
      const message = `[mcpserver-${this.pluginId}] failsafe replay failed: ${
        error instanceof Error ? error.message : String(error)
      }`;
      contextLogger(context).warn?.(message);
      process.stderr.write(`${message}\n`);
    }
  }

  async dispatchTool(name: string, args: Record<string, unknown>): Promise<ToolResult> {
    if (this.config.validateArguments) {
      validateToolArguments(name, args, allToolDescriptors);
    }
    if (canHandleWorkspaceTool(name)) {
      return handleWorkspaceTool(name, args, this.bridge, this.setupWorkspacePath);
    }
    if (canHandleTodoTool(name)) return handleTodoTool(name, args, this.bridge);
    if (canHandleSessionTool(name)) return handleSessionTool(name, args, this.bridge);
    if (canHandleMemoryTool(name)) return handleMemoryTool(name, args, this.bridge);
    if (canHandleBrainSlotTool(name)) return handleBrainSlotTool(name, args, this.bridge);
    if (canHandleRequirementsTool(name)) return handleRequirementsTool(name, args, this.bridge);
    if (canHandleGraphragTool(name)) return handleGraphragTool(name, args, this.bridge);
    throw new Error(`Unknown tool: ${name}`);
  }

  private async invokeSession(name: string, args: Record<string, unknown>): Promise<void> {
    await handleSessionTool(name, args, this.bridge);
  }

  async startSession(context?: unknown): Promise<void> {
    const stamp = utcStamp();
    const prompt = contextPrompt(context, this.agentName);
    this.activeSessionId =
      this.activeSessionId ?? `${this.agentName}-${stamp}-${slug(this.setupWorkspacePath ?? 'workspace')}`;
    this.activeRequestId = `req-${stamp}-${slug(prompt)}`;
    await this.invokeSession('session_bootstrap', {});
    await this.invokeSession('session_open', {
      agent: this.agentName,
      sessionId: this.activeSessionId,
      title: this.config.sessionTitle ?? prompt.slice(0, 120),
      model: contextModel(context),
    });
    await this.invokeSession('session_begin_turn', {
      requestId: this.activeRequestId,
      queryTitle: prompt.slice(0, 120),
      queryText: prompt,
    });
  }

  async completeSession(context?: unknown): Promise<void> {
    if (!this.activeSessionId || !this.activeRequestId) return;
    const record = asRecord(context);
    const result = asRecord(record.result);
    const error = toolError(context) || stringValue(result.error);
    if (error) {
      await this.invokeSession('session_fail_turn', {
        errorMessage: error,
        errorCode: `${this.pluginId.replace(/-/g, '_')}_run_failed`,
      });
      await this.invokeSession('session_close', {
        agent: this.agentName,
        sessionId: this.activeSessionId,
        status: 'failed',
      });
    } else {
      await this.invokeSession('session_complete_turn', {
        response:
          stringValue(result.output) ||
          stringValue(record.response) ||
          `${this.agentName} run completed.`,
      });
      await this.invokeSession('session_close', {
        agent: this.agentName,
        sessionId: this.activeSessionId,
        status: 'completed',
      });
    }
    this.activeRequestId = undefined;
  }

  async appendToolAction(
    context: unknown,
    status: 'pending' | 'completed',
    error?: string,
  ): Promise<void> {
    if (!this.activeRequestId) return;
    const name = toolName(context);
    const input = toolInput(context);
    await this.invokeSession('session_append_actions', {
      actions: [
        {
          order: ++this.actionOrder,
          type: 'design_decision',
          status,
          description: error
            ? `${this.agentName} tool ${name} failed: ${error}`
            : `${this.agentName} tool ${name} ${status === 'pending' ? 'started' : 'completed'}`,
        },
      ],
    });
    await this.invokeSession('session_append_dialog', {
      dialogItems: [
        {
          timestamp: new Date().toISOString(),
          role: 'tool',
          category: error ? 'tool_result' : status === 'pending' ? 'tool_call' : 'tool_result',
          content: JSON.stringify({ tool: name, input, status, ...(error ? { error } : {}) }),
        },
      ],
    });
  }
}
