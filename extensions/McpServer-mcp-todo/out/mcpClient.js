"use strict";
/**
 * MCP server HTTP client for the VS Code extension.
 * Reads the API key from AGENTS-README-FIRST.yaml in the workspace root.
 * All authenticated endpoints send X-Api-Key header automatically.
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
exports.getApiKey = getApiKey;
exports.clearApiKeyCache = clearApiKeyCache;
exports.getWorkspacePath = getWorkspacePath;
exports.fetchTodoList = fetchTodoList;
exports.fetchTodoById = fetchTodoById;
exports.updateTodo = updateTodo;
exports.streamSSE = streamSSE;
exports.ensureMcpServerRunning = ensureMcpServerRunning;
const vscode = __importStar(require("vscode"));
const http = __importStar(require("http"));
const https = __importStar(require("https"));
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const child_process_1 = require("child_process");
const logger_1 = require("./logger");
const defaultBaseUrl = 'http://localhost:7147';
const markerFileName = 'AGENTS-README-FIRST.yaml';
// ─── Configuration helpers ──────────────────────────────────────────────────
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
let _cachedApiKey = null;
/**
 * Reads the API key from AGENTS-README-FIRST.yaml in the workspace root.
 * Caches the value; call `clearApiKeyCache()` to force re-read (e.g., on 401).
 */
function getApiKey() {
    if (_cachedApiKey)
        return _cachedApiKey;
    const folders = vscode.workspace.workspaceFolders;
    if (!folders)
        return null;
    for (const folder of folders) {
        const markerPath = path.join(folder.uri.fsPath, markerFileName);
        if (!fs.existsSync(markerPath))
            continue;
        try {
            const content = fs.readFileSync(markerPath, 'utf8');
            const match = /^apiKey:\s*(.+)$/m.exec(content);
            if (match) {
                _cachedApiKey = match[1].trim();
                (0, logger_1.log)('getApiKey() loaded from marker', { folder: folder.uri.fsPath });
                return _cachedApiKey;
            }
        }
        catch (e) {
            (0, logger_1.log)('getApiKey() read error', String(e));
        }
    }
    return null;
}
/** Clears cached API key so the next call to `getApiKey()` re-reads the marker file. */
function clearApiKeyCache() {
    _cachedApiKey = null;
}
/**
 * Returns the workspace root path (folder containing AGENTS-README-FIRST.yaml, or first folder).
 * Used as X-Workspace-Path so the server resolves the correct workspace and prompt templates.
 */
function getWorkspacePath() {
    const folders = vscode.workspace.workspaceFolders;
    if (!folders?.length)
        return null;
    for (const folder of folders) {
        const markerPath = path.join(folder.uri.fsPath, markerFileName);
        if (fs.existsSync(markerPath))
            return folder.uri.fsPath;
    }
    return folders[0].uri.fsPath;
}
/** Builds common request options with API key and workspace headers. */
function buildRequestOpts(url, opts) {
    const isHttps = url.protocol === 'https:';
    const headers = { Accept: opts.accept ?? 'application/json' };
    const apiKey = getApiKey();
    if (apiKey)
        headers['X-Api-Key'] = apiKey;
    // When API key is present, server resolves workspace via Tier 2 (key → workspace). Only send
    // X-Workspace-Path when we have no key so Tier 1 path lookup is used (avoids path casing mismatch).
    const workspacePath = getWorkspacePath();
    if (workspacePath && !apiKey)
        headers['X-Workspace-Path'] = workspacePath;
    if (opts.body !== undefined) {
        headers['Content-Type'] = 'application/json';
        headers['Content-Length'] = Buffer.byteLength(opts.body, 'utf8');
    }
    return {
        host: url.hostname,
        port: url.port || (isHttps ? 443 : 80),
        path: url.pathname + url.search,
        method: opts.method,
        headers,
        rejectUnauthorized: false,
    };
}
function getLib(url) {
    return url.protocol === 'https:' ? https : http;
}
/** Performs an HTTP request, retrying once on 401 after re-reading the API key. */
function jsonRequest(method, urlPath, body, parse, label) {
    return doRequest(method, urlPath, body, parse, label, true);
}
function doRequest(method, urlPath, body, parse, label, retryOn401) {
    const base = getMcpBaseUrl().replace(/\/$/, '');
    const url = new URL(urlPath, base);
    const lib = getLib(url);
    const reqOpts = buildRequestOpts(url, { method, urlPath, body });
    (0, logger_1.log)(`${label} request`, { url: url.toString(), method });
    return new Promise((resolve, reject) => {
        const req = lib.request(reqOpts, (res) => {
            const chunks = [];
            res.on('data', (chunk) => chunks.push(chunk));
            res.on('end', () => {
                const respBody = Buffer.concat(chunks).toString('utf8');
                const status = res.statusCode ?? 0;
                (0, logger_1.log)(`${label} response`, { statusCode: status, bodyLength: respBody.length });
                if (status === 401 && retryOn401) {
                    clearApiKeyCache();
                    doRequest(method, urlPath, body, parse, label, false).then(resolve, reject);
                    return;
                }
                try {
                    resolve(parse(respBody, status));
                }
                catch (e) {
                    reject(e);
                }
            });
        });
        req.on('error', (err) => {
            (0, logger_1.log)(`${label} request error`, { code: err.code, message: err.message });
            reject(err);
        });
        if (body !== undefined)
            req.write(body, 'utf8');
        req.end();
    });
}
function assertSuccess(body, statusCode, label) {
    if (statusCode < 200 || statusCode >= 300) {
        throw new Error(`MCP todo: ${statusCode} ${body.slice(0, 200)}`);
    }
}
// ─── Public API ──────────────────────────────────────────────────────────────
/**
 * GET /mcpserver/todo with optional query params. Returns items and totalCount.
 */
function fetchTodoList(options) {
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
    const urlPath = qs ? `/mcpserver/todo?${qs}` : '/mcpserver/todo';
    return jsonRequest('GET', urlPath, undefined, (body, status) => {
        assertSuccess(body, status, 'fetchTodoList');
        const data = JSON.parse(body);
        const items = Array.isArray(data.items) ? data.items : (data.Items ?? []);
        const totalCount = typeof data.totalCount === 'number' ? data.totalCount : (data.TotalCount ?? items.length);
        return { items, totalCount };
    }, 'fetchTodoList');
}
/**
 * GET /mcpserver/todo/{id}. Returns a single todo or null if not found.
 */
function fetchTodoById(id) {
    const urlPath = `/mcpserver/todo/${encodeURIComponent(id)}`;
    return jsonRequest('GET', urlPath, undefined, (body, status) => {
        if (status === 404)
            return null;
        assertSuccess(body, status, 'fetchTodoById');
        const data = JSON.parse(body);
        const rawTasks = (data.implementationTasks ?? data.ImplementationTasks);
        const implementationTasks = rawTasks?.map((t) => ({ task: t.task ?? t.Task ?? '', done: t.done ?? t.Done ?? false }));
        return {
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
    }, 'fetchTodoById');
}
/**
 * PUT /mcpserver/todo/{id}. Updates an existing todo. Body fields are optional.
 */
function updateTodo(id, body) {
    const urlPath = `/mcpserver/todo/${encodeURIComponent(id)}`;
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
    return jsonRequest('PUT', urlPath, payload, (respBody, status) => {
        if (status >= 200 && status < 300) {
            try {
                const data = (respBody ? JSON.parse(respBody) : {});
                return { success: data.Success ?? data.success ?? true, error: data.Error ?? data.error, item: data.Item ?? data.item };
            }
            catch {
                return { success: true };
            }
        }
        try {
            const errData = respBody ? JSON.parse(respBody) : {};
            const errMsg = errData.Error ?? errData.error ?? String(status);
            return { success: false, error: errMsg };
        }
        catch {
            return { success: false, error: String(status) };
        }
    }, 'updateTodo');
}
// ─── SSE streaming ───────────────────────────────────────────────────────────
/**
 * Connects to an SSE endpoint and calls `onLine` for each `data:` line.
 * Resolves when the server sends `event: done` or closes the connection.
 * Rejects on HTTP error or network failure.
 */
function streamSSE(urlPath, onLine, signal) {
    const base = getMcpBaseUrl().replace(/\/$/, '');
    const url = new URL(urlPath, base);
    const lib = getLib(url);
    const reqOpts = buildRequestOpts(url, { method: 'GET', urlPath, accept: 'text/event-stream' });
    (0, logger_1.log)('streamSSE request', { url: url.toString() });
    return new Promise((resolve, reject) => {
        const req = lib.request(reqOpts, (res) => {
            const status = res.statusCode ?? 0;
            if (status === 401) {
                // Retry once after refreshing API key
                clearApiKeyCache();
                const retryOpts = buildRequestOpts(url, { method: 'GET', urlPath, accept: 'text/event-stream' });
                const retryReq = lib.request(retryOpts, (retryRes) => {
                    if ((retryRes.statusCode ?? 0) < 200 || (retryRes.statusCode ?? 0) >= 300) {
                        retryRes.resume();
                        reject(new Error(`MCP SSE: ${retryRes.statusCode}`));
                        return;
                    }
                    handleSseStream(retryRes, onLine, resolve, reject);
                });
                retryReq.on('error', reject);
                retryReq.end();
                res.resume();
                return;
            }
            if (status < 200 || status >= 300) {
                res.resume();
                reject(new Error(`MCP SSE: ${status}`));
                return;
            }
            handleSseStream(res, onLine, resolve, reject);
        });
        req.on('error', (err) => {
            (0, logger_1.log)('streamSSE request error', { message: err.message });
            reject(err);
        });
        if (signal) {
            const onAbort = () => { req.destroy(); reject(new Error('Aborted')); };
            if (signal.aborted) {
                req.destroy();
                reject(new Error('Aborted'));
                return;
            }
            signal.addEventListener('abort', onAbort, { once: true });
        }
        req.end();
    });
}
/** Parse one SSE line: if it's "data:" or "data: ...", return payload; if "event: done" return null (signal done); otherwise undefined. */
function parseSseLine(trimmed) {
    if (trimmed.startsWith('event: done'))
        return null;
    if (trimmed.startsWith('data:')) {
        const payload = trimmed.slice(5).replace(/^\s+/, '');
        return payload;
    }
    return undefined;
}
function handleSseStream(res, onLine, resolve, reject) {
    let buffer = '';
    res.setEncoding('utf8');
    function processLine(trimmed) {
        const result = parseSseLine(trimmed);
        if (result === null)
            return true; // event: done
        if (result !== undefined)
            onLine(result);
        return false;
    }
    res.on('data', (chunk) => {
        buffer += chunk;
        const parts = buffer.split('\n');
        buffer = parts.pop() ?? '';
        for (const part of parts) {
            const trimmed = part.trim();
            if (processLine(trimmed)) {
                res.destroy();
                resolve();
                return;
            }
        }
    });
    res.on('end', () => {
        // Flush remaining buffer so last line isn't dropped (e.g. "data: last" without trailing \n\n)
        if (buffer.length > 0) {
            const trimmed = buffer.trim();
            if (processLine(trimmed)) {
                resolve();
                return;
            }
        }
        resolve();
    });
    res.on('error', reject);
}
// ─── Server lifecycle ────────────────────────────────────────────────────────
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
    const workDir = path.dirname(path.dirname(scriptPath));
    const child = (0, child_process_1.spawn)('pwsh', ['-NoProfile', '-File', scriptPath], {
        cwd: workDir, windowsHide: true, detached: true, stdio: 'ignore',
    });
    child.unref();
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
    const lib = getLib(url);
    return new Promise((resolve) => {
        const req = lib.get({ host: url.hostname, port: url.port || (url.protocol === 'https:' ? 443 : 80), path: '/health', timeout: 3000, headers: { Accept: 'application/json' } }, (res) => { res.resume(); resolve(res.statusCode !== undefined && res.statusCode >= 200 && res.statusCode < 300); });
        req.on('error', () => resolve(false));
        req.on('timeout', () => { req.destroy(); resolve(false); });
    });
}
function findStartScript() {
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