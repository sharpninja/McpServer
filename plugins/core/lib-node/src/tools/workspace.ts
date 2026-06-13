import * as path from 'path';
import type { ToolDescriptor as Tool } from './tool-descriptor.js';
import type { ReplBridge, ReplResponse } from '../transport/repl-bridge.js';
import { cacheDelete, cacheWrite } from '../cache/cache-manager.js';
import { fullBootstrap } from '../discovery/marker-resolver.js';

export const workspaceTools: Tool[] = [
  {
    name: 'workspace_ensure',
    description:
      'Ensure the current workspace is trusted and initialized in McpServer. Registers and initializes it only when missing or untrusted.',
    inputSchema: {
      type: 'object',
      properties: {
        workspacePath: {
          type: 'string',
          description: 'Absolute workspace root. Defaults to the Cline workspace root or current process directory.',
        },
        name: {
          type: 'string',
          description: 'Workspace display name. Defaults to the final path segment.',
        },
        todoPath: {
          type: 'string',
          description: 'Relative TODO file path. Defaults to docs/todo.yaml on the server.',
        },
      },
    },
  },
];

export function canHandleWorkspaceTool(name: string): boolean {
  return name === 'workspace_ensure';
}

export function stringArg(args: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = args[key];
    if (typeof value === 'string' && value.trim().length > 0) return value.trim();
  }
  return '';
}

export function normalizePath(value: string): string {
  return path.resolve(value).replace(/\//g, '\\').replace(/\\+$/, '').toLowerCase();
}

export function workspaceKey(workspacePath: string): string {
  return Buffer.from(workspacePath.trim(), 'utf8')
    .toString('base64')
    .replace(/=+$/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');
}

export function findWorkspace(listResponse: ReplResponse, workspacePath: string): Record<string, unknown> | null {
  const payload = listResponse.payload as { result?: unknown };
  const result = payload.result;
  const items =
    result && typeof result === 'object' && !Array.isArray(result)
      ? (result as { items?: unknown }).items
      : undefined;

  if (!Array.isArray(items)) return null;
  const wanted = normalizePath(workspacePath);
  for (const item of items) {
    if (!item || typeof item !== 'object') continue;
    const record = item as Record<string, unknown>;
    if (typeof record.workspacePath === 'string' && normalizePath(record.workspacePath) === wanted) {
      return record;
    }
  }

  return null;
}

export async function invokeOrThrow(bridge: ReplBridge, method: string, params: Record<string, unknown>): Promise<ReplResponse> {
  const response = await bridge.invoke(method, params);
  if (response.type === 'error') {
    const payload = response.payload as { code?: string; message?: string };
    throw new Error(`${payload.code ?? 'error'}: ${payload.message ?? 'Unknown error'}`);
  }

  return response;
}

async function invokeMutatingWithFailsafe(
  bridge: ReplBridge,
  method: string,
  params: Record<string, unknown>,
): Promise<ReplResponse> {
  const failsafePath = await cacheWrite(method, params);
  try {
    const response = await invokeOrThrow(bridge, method, params);
    await cacheDelete(failsafePath);
    return response;
  } catch (error) {
    throw new Error(`${error instanceof Error ? error.message : String(error)} Local failsafe saved: ${failsafePath}`);
  }
}

export async function handleWorkspaceTool(
  name: string,
  args: Record<string, unknown>,
  bridge: ReplBridge,
  defaultWorkspacePath?: string,
) {
  if (name !== 'workspace_ensure') throw new Error(`Unknown workspace tool: ${name}`);

  const workspacePath = path.resolve(
    stringArg(args, 'workspacePath', 'path') ||
      defaultWorkspacePath ||
      process.env.MCPSERVER_WORKSPACE_PATH ||
      process.env.MCP_WORKSPACE_PATH ||
      process.cwd(),
  );

  try {
    const marker = await fullBootstrap(workspacePath);
    process.env.MCPSERVER_BASE_URL = marker.baseUrl;
    process.env.MCPSERVER_API_KEY = marker.apiKey;
    process.env.MCPSERVER_WORKSPACE_PATH = marker.workspacePath;
    process.env.MCP_WORKSPACE_PATH = marker.workspacePath;
    process.env.MCPSERVER_WORKSPACE = marker.workspace;
    return {
      trusted: true,
      registered: true,
      initialized: true,
      created: false,
      workspacePath: marker.workspacePath,
      workspace: marker.workspace,
      markerFile: marker.markerFile,
      baseUrl: marker.baseUrl,
    };
  } catch (error) {
    const bootstrapError = error instanceof Error ? error.message : String(error);

    const listResponse = await invokeOrThrow(bridge, 'client.Workspace.ListAsync', {});
    const existing = findWorkspace(listResponse, workspacePath);
    const key = workspaceKey(workspacePath);

    let createResponse: ReplResponse | undefined;
    if (!existing) {
      const request: Record<string, unknown> = {
        workspacePath,
        name: stringArg(args, 'name') || path.basename(workspacePath),
      };
      const todoPath = stringArg(args, 'todoPath');
      if (todoPath) request.todoPath = todoPath;
      createResponse = await invokeMutatingWithFailsafe(bridge, 'client.Workspace.CreateAsync', { request });
    }

    const initResponse = await invokeMutatingWithFailsafe(bridge, 'client.Workspace.InitAsync', { key });

    const createPayload = createResponse?.payload as { result?: unknown } | undefined;
    return {
      trusted: false,
      bootstrapError,
      registered: true,
      initialized: true,
      created: !existing,
      workspacePath,
      workspaceKey: key,
      workspace: existing ?? createPayload?.result ?? null,
      init: initResponse.payload,
      markerReloadRequired: true,
    };
  }
}
