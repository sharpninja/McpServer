/**
 * Fetches TODO items from the FunWasHad MCP server (GET /mcp/todo).
 * Uses Node http(s) so the extension works in all VS Code/Electron environments (no global fetch).
 */

import * as vscode from 'vscode';
import * as http from 'http';
import * as https from 'https';
import { spawn } from 'child_process';
import { log } from './logger';

export interface TodoFlatTask {
  task: string;
  done: boolean;
}

export interface TodoFlatItem {
  id: string;
  title: string;
  section: string;
  priority: string;
  done: boolean;
  estimate?: string;
  note?: string;
  description?: string[];
  technicalDetails?: string[];
  implementationTasks?: TodoFlatTask[];
  completedDate?: string;
  doneSummary?: string;
  remaining?: string;
  dependsOn?: string[];
  functionalRequirements?: string[];
  technicalRequirements?: string[];
  priorityNote?: string;
  reference?: string;
}

export interface TodoQueryResult {
  items: TodoFlatItem[];
  totalCount: number;
}

const defaultBaseUrl = 'http://localhost:7147';

export function getMcpBaseUrl(): string {
  try {
    const cfg = vscode.workspace.getConfiguration('fwhMcpTodo');
    const url = cfg.get<string>('mcpBaseUrl');
    const base = (url && url.trim()) || defaultBaseUrl;
    log('getMcpBaseUrl()', { base });
    return base;
  } catch (e) {
    log('getMcpBaseUrl() catch', String(e));
    return defaultBaseUrl;
  }
}

/**
 * GET /mcp/todo with optional query params. Returns items and totalCount.
 * Throws on non-2xx or network error.
 */
export function fetchTodoList(options?: {
  keyword?: string;
  priority?: string;
  section?: string;
  id?: string;
  done?: boolean;
}): Promise<TodoQueryResult> {
  const base = getMcpBaseUrl().replace(/\/$/, '');
  const params = new URLSearchParams();
  if (options?.keyword) params.set('keyword', options.keyword);
  if (options?.priority) params.set('priority', options.priority);
  if (options?.section) params.set('section', options.section);
  if (options?.id) params.set('id', options.id);
  if (options?.done !== undefined) params.set('done', String(options.done));
  const qs = params.toString();
  const path = qs ? `/mcp/todo?${qs}` : '/mcp/todo';
  const url = new URL(path, base);
  const fullUrl = url.toString();
  log('fetchTodoList() request', { fullUrl, options: options ?? null });

  const isHttps = url.protocol === 'https:';
  const lib = isHttps ? https : http;

  return new Promise((resolve, reject) => {
    const req = lib.get(
      {
        host: url.hostname,
        port: url.port || (isHttps ? 443 : 80),
        path: url.pathname + url.search,
        method: 'GET',
        headers: { Accept: 'application/json' },
        rejectUnauthorized: false,
      },
      (res) => {
        const chunks: Buffer[] = [];
        res.on('data', (chunk) => chunks.push(chunk));
        res.on('end', () => {
          const body = Buffer.concat(chunks).toString('utf8');
          log('fetchTodoList() response', { statusCode: res.statusCode, bodyLength: body.length });

          if (res.statusCode && res.statusCode >= 200 && res.statusCode < 300) {
            try {
              const data = JSON.parse(body) as {
                items?: TodoFlatItem[];
                totalCount?: number;
                Items?: TodoFlatItem[];
                TotalCount?: number;
              };
              const items = Array.isArray(data.items) ? data.items : (data.Items ?? []);
              const totalCount =
                typeof data.totalCount === 'number' ? data.totalCount : (data.TotalCount ?? items.length);
              log('fetchTodoList() parsed', { itemCount: items.length, totalCount });
              resolve({ items, totalCount });
            } catch (e) {
              log('fetchTodoList() JSON parse error', String(e));
              reject(new Error('MCP todo: invalid JSON'));
            }
          } else {
            log('fetchTodoList() HTTP error', { statusCode: res.statusCode, statusMessage: res.statusMessage });
            reject(new Error(`MCP todo: ${res.statusCode ?? 'unknown'} ${res.statusMessage ?? ''}`));
          }
        });
      }
    );
    req.on('error', (err: NodeJS.ErrnoException) => {
      const code = err.code ?? '';
      const msg = err.message || String(err);
      log('fetchTodoList() request error', { code, message: msg, full: String(err) });
      reject(err);
    });
  });
}

/**
 * GET /mcp/todo/{id}. Returns a single todo or null if not found.
 */
export function fetchTodoById(id: string): Promise<TodoFlatItem | null> {
  const base = getMcpBaseUrl().replace(/\/$/, '');
  const path = `/mcp/todo/${encodeURIComponent(id)}`;
  const url = new URL(path, base);
  log('fetchTodoById() request', { id, url: url.toString() });

  const isHttps = url.protocol === 'https:';
  const lib = isHttps ? https : http;

  return new Promise((resolve, reject) => {
    const req = lib.get(
      {
        host: url.hostname,
        port: url.port || (isHttps ? 443 : 80),
        path: url.pathname + url.search,
        method: 'GET',
        headers: { Accept: 'application/json' },
        rejectUnauthorized: false,
      },
      (res) => {
        const chunks: Buffer[] = [];
        res.on('data', (chunk) => chunks.push(chunk));
        res.on('end', () => {
          const body = Buffer.concat(chunks).toString('utf8');
          log('fetchTodoById() response', { statusCode: res.statusCode });
          if (res.statusCode === 404) {
            resolve(null);
            return;
          }
          if (res.statusCode && res.statusCode >= 200 && res.statusCode < 300) {
            try {
              const data = JSON.parse(body) as Record<string, unknown>;
              const rawTasks = (data.implementationTasks ?? data.ImplementationTasks) as Array<{ task?: string; done?: boolean; Task?: string; Done?: boolean }> | undefined;
              const implementationTasks = rawTasks?.map((t) => ({ task: t.task ?? t.Task ?? '', done: t.done ?? t.Done ?? false }));
              const item: TodoFlatItem = {
                id: (data.id ?? data.Id ?? id) as string,
                title: (data.title ?? data.Title ?? '') as string,
                section: (data.section ?? data.Section ?? '') as string,
                priority: (data.priority ?? data.Priority ?? '') as string,
                done: (data.done ?? data.Done ?? false) as boolean,
                estimate: (data.estimate ?? data.Estimate) as string | undefined,
                note: (data.note ?? data.Note) as string | undefined,
                description: (data.description ?? data.Description) as string[] | undefined,
                technicalDetails: (data.technicalDetails ?? data.TechnicalDetails) as string[] | undefined,
                implementationTasks,
                completedDate: (data.completedDate ?? data.CompletedDate) as string | undefined,
                doneSummary: (data.doneSummary ?? data.DoneSummary) as string | undefined,
                remaining: (data.remaining ?? data.Remaining) as string | undefined,
                dependsOn: (data.dependsOn ?? data.DependsOn) as string[] | undefined,
                reference: (data.reference ?? data.Reference) as string | undefined,
              };
              resolve(item);
            } catch (e) {
              log('fetchTodoById() JSON parse error', String(e));
              reject(new Error('MCP todo: invalid JSON'));
            }
          } else {
            reject(new Error(`MCP todo: ${res.statusCode ?? 'unknown'} ${res.statusMessage ?? ''}`));
          }
        });
      }
    );
    req.on('error', (err: NodeJS.ErrnoException) => {
      log('fetchTodoById() request error', { code: err.code, message: err.message });
      reject(err);
    });
  });
}

/** Body for PUT /mcp/todo/{id}. Only provided fields are updated. */
export interface TodoUpdateBody {
  title?: string;
  priority?: string;
  section?: string;
  done?: boolean;
  estimate?: string;
  description?: string[];
  technicalDetails?: string[];
  implementationTasks?: TodoFlatTask[];
  note?: string;
  completedDate?: string;
  doneSummary?: string;
  remaining?: string;
  dependsOn?: string[];
  functionalRequirements?: string[];
  technicalRequirements?: string[];
}

/** Result of create/update/delete. */
export interface TodoMutationResult {
  success: boolean;
  error?: string;
  item?: TodoFlatItem;
}

/**
 * PUT /mcp/todo/{id}. Updates an existing todo. Body fields are optional.
 */
export function updateTodo(id: string, body: TodoUpdateBody): Promise<TodoMutationResult> {
  const base = getMcpBaseUrl().replace(/\/$/, '');
  const path = `/mcp/todo/${encodeURIComponent(id)}`;
  const url = new URL(path, base);
  const payload = JSON.stringify({
    Title: body.title,
    Priority: body.priority,
    Section: body.section,
    Done: body.done,
    Estimate: body.estimate,
    Description: body.description,
    TechnicalDetails: body.technicalDetails,
    ImplementationTasks: body.implementationTasks?.map((t) => ({ Task: t.task, Done: t.done })),
    Note: body.note,
    CompletedDate: body.completedDate,
    DoneSummary: body.doneSummary,
    Remaining: body.remaining,
    DependsOn: body.dependsOn,
    FunctionalRequirements: body.functionalRequirements,
    TechnicalRequirements: body.technicalRequirements,
  });
  log('updateTodo() request', { id, url: url.toString(), bodyKeys: Object.keys(body) });

  const isHttps = url.protocol === 'https:';
  const lib = isHttps ? https : http;

  return new Promise((resolve, reject) => {
    const req = lib.request(
      {
        host: url.hostname,
        port: url.port || (isHttps ? 443 : 80),
        path: url.pathname + url.search,
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json', 'Content-Length': Buffer.byteLength(payload, 'utf8') },
        rejectUnauthorized: false,
      },
      (res) => {
        const chunks: Buffer[] = [];
        res.on('data', (chunk) => chunks.push(chunk));
        res.on('end', () => {
          const respBody = Buffer.concat(chunks).toString('utf8');
          log('updateTodo() response', { statusCode: res.statusCode });
          if (res.statusCode && res.statusCode >= 200 && res.statusCode < 300) {
            try {
              const data = (respBody ? JSON.parse(respBody) : {}) as { Success?: boolean; Error?: string; Item?: TodoFlatItem };
              resolve({
                success: data.Success ?? (data as unknown as { success?: boolean }).success ?? true,
                error: data.Error ?? (data as unknown as { error?: string }).error,
                item: data.Item ?? (data as unknown as { item?: TodoFlatItem }).item,
              });
            } catch {
              resolve({ success: true });
            }
          } else {
            try {
              const errData = respBody ? JSON.parse(respBody) : {};
              const errMsg = (errData as { Error?: string }).Error ?? (errData as { error?: string }).error ?? res.statusMessage ?? String(res.statusCode);
              resolve({ success: false, error: errMsg });
            } catch {
              resolve({ success: false, error: res.statusMessage ?? String(res.statusCode) });
            }
          }
        });
      }
    );
    req.on('error', (err: NodeJS.ErrnoException) => {
      log('updateTodo() request error', { code: err.code, message: err.message });
      reject(err);
    });
    req.write(payload, 'utf8');
    req.end();
  });
}

/**
 * Checks if the MCP server is reachable. If not, attempts to start it
 * via Start-McpServer.ps1 and waits for it to become healthy.
 */
export async function ensureMcpServerRunning(): Promise<boolean> {
  if (await isHealthy()) return false;

  log('MCP server is not running. Attempting to start...');

  const scriptPath = findStartScript();
  if (!scriptPath) {
    log('Could not find scripts/Start-McpServer.ps1. MCP server must be started manually.');
    return false;
  }

  log(`Starting MCP server via ${scriptPath}`);
  const path = await import('path');
  const workDir = path.dirname(path.dirname(scriptPath));
  const child = spawn('pwsh', ['-NoProfile', '-File', scriptPath], {
    cwd: workDir,
    windowsHide: true,
    detached: true,
    stdio: 'ignore',
  });
  child.unref();

  // Wait for the server to become healthy (up to ~30 seconds)
  for (let i = 0; i < 10; i++) {
    await new Promise((r) => setTimeout(r, 3000));
    if (await isHealthy()) {
      log('MCP server started successfully.');
      return true;
    }
  }

  log('MCP server did not become healthy within 30 seconds.');
  return false;
}

function isHealthy(): Promise<boolean> {
  const base = getMcpBaseUrl().replace(/\/$/, '');
  const url = new URL('/health', base);
  const isHttps = url.protocol === 'https:';
  const lib = isHttps ? https : http;

  return new Promise((resolve) => {
    const req = lib.get(
      {
        host: url.hostname,
        port: url.port || (isHttps ? 443 : 80),
        path: '/health',
        timeout: 3000,
        headers: { Accept: 'application/json' },
      },
      (res) => {
        res.resume();
        resolve(res.statusCode !== undefined && res.statusCode >= 200 && res.statusCode < 300);
      }
    );
    req.on('error', () => resolve(false));
    req.on('timeout', () => {
      req.destroy();
      resolve(false);
    });
  });
}

function findStartScript(): string | null {
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const path = require('path') as typeof import('path');
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const fs = require('fs') as typeof import('fs');

  // Extension only activates when FunWasHad.sln is in the workspace,
  // so the workspace root is the repo root.
  const folders = vscode.workspace.workspaceFolders;
  if (folders) {
    for (const folder of folders) {
      const candidate = path.join(folder.uri.fsPath, 'scripts', 'Start-McpServer.ps1');
      if (fs.existsSync(candidate)) return candidate;
    }
  }

  return null;
}
