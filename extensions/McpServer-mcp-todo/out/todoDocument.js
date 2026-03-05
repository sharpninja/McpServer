"use strict";
/**
 * Virtual documents for TODO items: fwh-todo://todo/{id}
 * - ContentProvider supplies initial markdown when opening.
 * - FileSystemProvider writeFile handles Save and pushes to MCP.
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
exports.TodoFileSystemProvider = exports.TodoContentProvider = void 0;
exports.todoUri = todoUri;
exports.newTodoUri = newTodoUri;
const vscode = __importStar(require("vscode"));
const logger_1 = require("./logger");
const todoMarkdown_1 = require("./todoMarkdown");
const SCHEME = 'fwh-todo';
const AUTHORITY = 'todo';
const NEW_AUTHORITY = 'new';
function todoIdFromUri(uri) {
    if (uri.scheme !== SCHEME || !uri.path.startsWith('/'))
        return null;
    if (uri.authority !== AUTHORITY && uri.authority !== NEW_AUTHORITY)
        return null;
    return decodeURIComponent(uri.path.slice(1));
}
function isNewTodoUri(uri) {
    return uri.scheme === SCHEME && uri.authority === NEW_AUTHORITY;
}
function todoUri(id) {
    return vscode.Uri.parse(`${SCHEME}://${AUTHORITY}/${encodeURIComponent(id)}`);
}
function newTodoUri() {
    return vscode.Uri.parse(`${SCHEME}://${NEW_AUTHORITY}/new-todo`);
}
/**
 * Provides initial markdown when opening a todo URI.
 */
class TodoContentProvider {
    async provideTextDocumentContent(uri) {
        if (isNewTodoUri(uri)) {
            return (0, todoMarkdown_1.blankTodoTemplate)();
        }
        const id = todoIdFromUri(uri);
        if (!id)
            return '# Invalid URI';
        try {
            const { fetchTodoById } = await Promise.resolve().then(() => __importStar(require('./mcpClient')));
            const item = await fetchTodoById(id);
            if (!item)
                return `# ${id}\n\n(Not found on server.)`;
            return (0, todoMarkdown_1.todoToMarkdown)(item);
        }
        catch (e) {
            (0, logger_1.log)('TodoContentProvider.provideTextDocumentContent error', String(e));
            return `# ${id}\n\n(Error loading: ${e instanceof Error ? e.message : String(e)})`;
        }
    }
}
exports.TodoContentProvider = TodoContentProvider;
/**
 * Handles Save: writes go to MCP via PUT /mcpserver/todo/{id}.
 */
class TodoFileSystemProvider {
    _emitter = new vscode.EventEmitter();
    onDidChangeFile = this._emitter.event;
    watch(_uri) {
        return new vscode.Disposable(() => { });
    }
    stat(uri) {
        const id = todoIdFromUri(uri);
        if (!id)
            throw vscode.FileSystemError.FileNotFound(uri);
        return {
            type: vscode.FileType.File,
            ctime: Date.now(),
            mtime: Date.now(),
            size: 0,
        };
    }
    readDirectory(_uri) {
        return [];
    }
    createDirectory(_uri) {
        throw vscode.FileSystemError.NoPermissions();
    }
    readFile(uri) {
        if (isNewTodoUri(uri)) {
            return Buffer.from((0, todoMarkdown_1.blankTodoTemplate)(), 'utf8');
        }
        const id = todoIdFromUri(uri);
        if (!id)
            throw vscode.FileSystemError.FileNotFound(uri);
        return (async () => {
            try {
                const { fetchTodoById } = await Promise.resolve().then(() => __importStar(require('./mcpClient')));
                const item = await fetchTodoById(id);
                const markdown = item ? (0, todoMarkdown_1.todoToMarkdown)(item) : `# ${id}\n\n(Not found on server.)`;
                return Buffer.from(markdown, 'utf8');
            }
            catch (e) {
                (0, logger_1.log)('TodoFileSystemProvider.readFile error', String(e));
                const markdown = `# ${id}\n\n(Error loading: ${e instanceof Error ? e.message : String(e)})`;
                return Buffer.from(markdown, 'utf8');
            }
        })();
    }
    async writeFile(uri, content, _options) {
        const text = Buffer.from(content).toString('utf8');
        // New todo: build CLI command and invoke via copilot client
        if (isNewTodoUri(uri)) {
            const fields = (0, todoMarkdown_1.markdownToNewTodoFields)(text);
            if (!fields.title || fields.title.trim() === '') {
                (0, logger_1.log)('TodoFileSystemProvider.writeFile: title is required for new todo');
                return;
            }
            const priority = fields.priority || 'low';
            const command = `add ${priority} todo: ${fields.title.trim()}`;
            (0, logger_1.log)('TodoFileSystemProvider.writeFile: invoking copilot for new todo', { command });
            try {
                const { invokeCopilot } = await Promise.resolve().then(() => __importStar(require('fwh-copilot-client')));
                (0, logger_1.copilotLog)(`>>> Prompt (New Todo):\n${command}`);
                const result = await invokeCopilot(command, { timeoutMs: 60_000 });
                (0, logger_1.copilotLog)(`<<< ${result.state} (New Todo):\n${result.body}`);
                if (result.stderr)
                    (0, logger_1.copilotLog)(`<<< Stderr:\n${result.stderr}`);
                if (result.state === 'success') {
                    vscode.window.setStatusBarMessage(`McpServer MCP Todo: Created — ${result.body.substring(0, 80)}`, 5000);
                    (0, logger_1.log)('TodoFileSystemProvider.writeFile: new todo created, refreshing tree');
                    await vscode.commands.executeCommand('fwhMcpTodo.refresh');
                }
                else {
                    (0, logger_1.copilotLog)(`<<< Error (New Todo): ${result.state} — ${result.stderr || result.body}`);
                }
            }
            catch (e) {
                const msg = e instanceof Error ? e.message : String(e);
                (0, logger_1.log)('TodoFileSystemProvider.writeFile copilot error', msg);
                // Fallback to terminal
                const terminal = vscode.window.createTerminal({ name: 'McpServer MCP Todo' });
                terminal.show(true);
                terminal.sendText(command, true);
                vscode.window.setStatusBarMessage(`McpServer MCP Todo: Sent "${command}" to terminal (copilot unavailable)`, 5000);
            }
            return;
        }
        const id = todoIdFromUri(uri);
        if (!id) {
            (0, logger_1.log)('TodoFileSystemProvider.writeFile: invalid uri', uri.toString());
            throw vscode.FileSystemError.FileNotFound(uri);
        }
        const body = (0, todoMarkdown_1.markdownToUpdateBody)(text);
        (0, logger_1.log)('TodoFileSystemProvider.writeFile: pushing to MCP', { id, bodyKeys: Object.keys(body) });
        try {
            const { updateTodo } = await Promise.resolve().then(() => __importStar(require('./mcpClient')));
            const result = await updateTodo(id, body);
            if (result.success) {
                vscode.window.setStatusBarMessage(`McpServer MCP Todo: ${id} updated`, 3000);
                (0, logger_1.log)(`TodoFileSystemProvider.writeFile: ${id} updated, refreshing tree`);
                await vscode.commands.executeCommand('fwhMcpTodo.refresh');
            }
            else {
                (0, logger_1.log)(`TodoFileSystemProvider.writeFile: update failed for ${id}`, result.error ?? 'unknown');
            }
        }
        catch (e) {
            const msg = e instanceof Error ? e.message : String(e);
            (0, logger_1.log)('TodoFileSystemProvider.writeFile error', msg);
            throw e;
        }
    }
    delete(_uri) {
        throw vscode.FileSystemError.NoPermissions();
    }
    rename(_old, _new) {
        throw vscode.FileSystemError.NoPermissions();
    }
}
exports.TodoFileSystemProvider = TodoFileSystemProvider;
//# sourceMappingURL=todoDocument.js.map