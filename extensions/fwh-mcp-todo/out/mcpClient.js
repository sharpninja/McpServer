"use strict";
/**
 * Fetches TODO items from the FunWasHad MCP server (GET /mcp/todo).
 * Uses Node http(s) so the extension works in all VS Code/Electron environments (no global fetch).
 */
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.getMcpBaseUrl = getMcpBaseUrl;
exports.fetchTodoList = fetchTodoList;
exports.fetchTodoById = fetchTodoById;
exports.updateTodo = updateTodo;
exports.ensureMcpServerRunning = ensureMcpServerRunning;
const vscode = __importStar(require("vscode"));
const http = __importStar(require("http"));
const https = __importStar(require("https"));
const child_process_1 = require("child_process");
const logger_1 = require("./logger");
const defaultBaseUrl = 'http://localhost:7147';
function getMcpBaseUrl() {
    try {
        const cfg = vscode.workspace.getConfiguration('fwhMcpTodo');
        const url = cfg.get('mcpBaseUrl');
        const base = (url && url.trim()) || defaultBaseUrl;
        (0, logger_1.log)('getMcpBaseUrl()', { base });
        return base;
    }
    catch (e) {
        (0, logger_1.log)('getMcpBaseUrl() catch', String(e));
        return defaultBaseUrl;
    }
}
/**
 * GET /mcp/todo with optional query params. Returns items and totalCount.
 * Throws on non-2xx or network error.
 */
function fetchTodoList(options) {
    const base = getMcpBaseUrl().replace(/\/$/, '');
    const params = new URLSearchParams();
    if (options?.keyword)
        params.set('keyword', options.keyword);
    if (options?.priority)
        params.set('priority', options.priority);
    if (options?.section)
        params.set('section', options.section);
    if (options?.id)
        params.set('id', options.id);
    if (options?.done !== undefined)
        params.set('done', String(options.done));
    const qs = params.toString();
    const path = qs ? `/mcp/todo?${qs}` : '/mcp/todo';
    const url = new URL(path, base);
    const fullUrl = url.toString();
    (0, logger_1.log)('fetchTodoList() request', { fullUrl, options: options ?? null });
    const isHttps = url.protocol === 'https:';
    const lib = isHttps ? https : http;
    return new Promise((resolve, reject) => {
        const req = lib.get({
            host: url.hostname,
            port: url.port || (isHttps ? 443 : 80),
            path: url.pathname + url.search,
            method: 'GET',
            headers: { Accept: 'application/json' },
            rejectUnauthorized: false,
        }, (res) => {
            const chunks = [];
            res.on('data', (chunk) => chunks.push(chunk));
            res.on('end', () => {
                const body = Buffer.concat(chunks).toString('utf8');
                (0, logger_1.log)('fetchTodoList() response', { statusCode: res.statusCode, bodyLength: body.length });
                if (res.statusCode && res.statusCode >= 200 && res.statusCode < 300) {
                    try {
                        const data = JSON.parse(body);
                        const items = Array.isArray(data.items) ? data.items : (data.Items ?? []);
                        const totalCount = typeof data.totalCount === 'number' ? data.totalCount : (data.TotalCount ?? items.length);
                        (0, logger_1.log)('fetchTodoList() parsed', { itemCount: items.length, totalCount });
                        resolve({ items, totalCount });
                    }
                    catch (e) {
                        (0, logger_1.log)('fetchTodoList() JSON parse error', String(e));
                        reject(new Error('MCP todo: invalid JSON'));
                    }
                }
                else {
                    (0, logger_1.log)('fetchTodoList() HTTP error', { statusCode: res.statusCode, statusMessage: res.statusMessage });
                    reject(new Error(`MCP todo: ${res.statusCode ?? 'unknown'} ${res.statusMessage ?? ''}`));
                }
            });
        });
        req.on('error', (err) => {
            const code = err.code ?? '';
            const msg = err.message || String(err);
            (0, logger_1.log)('fetchTodoList() request error', { code, message: msg, full: String(err) });
            reject(err);
        });
    });
}
/**
 * GET /mcp/todo/{id}. Returns a single todo or null if not found.
 */
function fetchTodoById(id) {
    const base = getMcpBaseUrl().replace(/\/$/, '');
    const path = `/mcp/todo/${encodeURIComponent(id)}`;
    const url = new URL(path, base);
    (0, logger_1.log)('fetchTodoById() request', { id, url: url.toString() });
    const isHttps = url.protocol === 'https:';
    const lib = isHttps ? https : http;
    return new Promise((resolve, reject) => {
        const req = lib.get({
            host: url.hostname,
            port: url.port || (isHttps ? 443 : 80),
            path: url.pathname + url.search,
            method: 'GET',
            headers: { Accept: 'application/json' },
            rejectUnauthorized: false,
        }, (res) => {
            const chunks = [];
            res.on('data', (chunk) => chunks.push(chunk));
            res.on('end', () => {
                const body = Buffer.concat(chunks).toString('utf8');
                (0, logger_1.log)('fetchTodoById() response', { statusCode: res.statusCode });
                if (res.statusCode === 404) {
                    resolve(null);
                    return;
                }
                if (res.statusCode && res.statusCode >= 200 && res.statusCode < 300) {
                    try {
                        const data = JSON.parse(body);
                        const rawTasks = (data.implementationTasks ?? data.ImplementationTasks);
                        const implementationTasks = rawTasks?.map((t) => ({ task: t.task ?? t.Task ?? '', done: t.done ?? t.Done ?? false }));
                        const item = {
                            id: (data.id ?? data.Id ?? id),
                            title: (data.title ?? data.Title ?? ''),
                            section: (data.section ?? data.Section ?? ''),
                            priority: (data.priority ?? data.Priority ?? ''),
                            done: (data.done ?? data.Done ?? false),
                            estimate: (data.estimate ?? data.Estimate),
                            note: (data.note ?? data.Note),
                            description: (data.description ?? data.Description),
                            technicalDetails: (data.technicalDetails ?? data.TechnicalDetails),
                            implementationTasks,
                            completedDate: (data.completedDate ?? data.CompletedDate),
                            doneSummary: (data.doneSummary ?? data.DoneSummary),
                            remaining: (data.remaining ?? data.Remaining),
                            dependsOn: (data.dependsOn ?? data.DependsOn),
                            reference: (data.reference ?? data.Reference),
                        };
                        resolve(item);
                    }
                    catch (e) {
                        (0, logger_1.log)('fetchTodoById() JSON parse error', String(e));
                        reject(new Error('MCP todo: invalid JSON'));
                    }
                }
                else {
                    reject(new Error(`MCP todo: ${res.statusCode ?? 'unknown'} ${res.statusMessage ?? ''}`));
                }
            });
        });
        req.on('error', (err) => {
            (0, logger_1.log)('fetchTodoById() request error', { code: err.code, message: err.message });
            reject(err);
        });
    });
}
/**
 * PUT /mcp/todo/{id}. Updates an existing todo. Body fields are optional.
 */
function updateTodo(id, body) {
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
    (0, logger_1.log)('updateTodo() request', { id, url: url.toString(), bodyKeys: Object.keys(body) });
    const isHttps = url.protocol === 'https:';
    const lib = isHttps ? https : http;
    return new Promise((resolve, reject) => {
        const req = lib.request({
            host: url.hostname,
            port: url.port || (isHttps ? 443 : 80),
            path: url.pathname + url.search,
            method: 'PUT',
            headers: { 'Content-Type': 'application/json', Accept: 'application/json', 'Content-Length': Buffer.byteLength(payload, 'utf8') },
            rejectUnauthorized: false,
        }, (res) => {
            const chunks = [];
            res.on('data', (chunk) => chunks.push(chunk));
            res.on('end', () => {
                const respBody = Buffer.concat(chunks).toString('utf8');
                (0, logger_1.log)('updateTodo() response', { statusCode: res.statusCode });
                if (res.statusCode && res.statusCode >= 200 && res.statusCode < 300) {
                    try {
                        const data = (respBody ? JSON.parse(respBody) : {});
                        resolve({
                            success: data.Success ?? data.success ?? true,
                            error: data.Error ?? data.error,
                            item: data.Item ?? data.item,
                        });
                    }
                    catch {
                        resolve({ success: true });
                    }
                }
                else {
                    try {
                        const errData = respBody ? JSON.parse(respBody) : {};
                        const errMsg = errData.Error ?? errData.error ?? res.statusMessage ?? String(res.statusCode);
                        resolve({ success: false, error: errMsg });
                    }
                    catch {
                        resolve({ success: false, error: res.statusMessage ?? String(res.statusCode) });
                    }
                }
            });
        });
        req.on('error', (err) => {
            (0, logger_1.log)('updateTodo() request error', { code: err.code, message: err.message });
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
async function ensureMcpServerRunning() {
    if (await isHealthy())
        return false;
    (0, logger_1.log)('MCP server is not running. Attempting to start...');
    const scriptPath = findStartScript();
    if (!scriptPath) {
        (0, logger_1.log)('Could not find scripts/Start-McpServer.ps1. MCP server must be started manually.');
        return false;
    }
    (0, logger_1.log)(`Starting MCP server via ${scriptPath}`);
    const path = await Promise.resolve().then(() => __importStar(require('path')));
    const workDir = path.dirname(path.dirname(scriptPath));
    const child = (0, child_process_1.spawn)('pwsh', ['-NoProfile', '-File', scriptPath], {
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
            (0, logger_1.log)('MCP server started successfully.');
            return true;
        }
    }
    (0, logger_1.log)('MCP server did not become healthy within 30 seconds.');
    return false;
}
function isHealthy() {
    const base = getMcpBaseUrl().replace(/\/$/, '');
    const url = new URL('/health', base);
    const isHttps = url.protocol === 'https:';
    const lib = isHttps ? https : http;
    return new Promise((resolve) => {
        const req = lib.get({
            host: url.hostname,
            port: url.port || (isHttps ? 443 : 80),
            path: '/health',
            timeout: 3000,
            headers: { Accept: 'application/json' },
        }, (res) => {
            res.resume();
            resolve(res.statusCode !== undefined && res.statusCode >= 200 && res.statusCode < 300);
        });
        req.on('error', () => resolve(false));
        req.on('timeout', () => {
            req.destroy();
            resolve(false);
        });
    });
}
function findStartScript() {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require('path');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require('fs');
    // Extension only activates when FunWasHad.sln is in the workspace,
    // so the workspace root is the repo root.
    const folders = vscode.workspace.workspaceFolders;
    if (folders) {
        for (const folder of folders) {
            const candidate = path.join(folder.uri.fsPath, 'scripts', 'Start-McpServer.ps1');
            if (fs.existsSync(candidate))
                return candidate;
        }
    }
    return null;
}
//# sourceMappingURL=mcpClient.js.map