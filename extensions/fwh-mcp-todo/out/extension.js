"use strict";
/**
 * FWH MCP Todo: shows TODO items from the MCP server in a tree view.
 * Double-click opens the todo in an editor as markdown; saving pushes updates to MCP.
 * All interactions are traced to Output → FWH MCP Todo.
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
exports.activate = activate;
exports.deactivate = deactivate;
const vscode = __importStar(require("vscode"));
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const os = __importStar(require("os"));
const todoTree_1 = require("./todoTree");
const filterPanel_1 = require("./filterPanel");
const todoDocument_1 = require("./todoDocument");
const logger_1 = require("./logger");
const mcpClient_1 = require("./mcpClient");
let activeCopilotAbort = null;
async function sendCopilotPrompt(id, action, prompt) {
    let invokeCopilot;
    let CopilotResultState;
    try {
        const client = await Promise.resolve().then(() => __importStar(require('fwh-copilot-client')));
        invokeCopilot = client.invokeCopilot;
        CopilotResultState = client.CopilotResultState;
    }
    catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        (0, logger_1.log)('fwh-copilot-client failed to load', msg);
        (0, logger_1.copilotLog)(`Copilot unavailable: ${msg}`);
        vscode.window.showErrorMessage(`FWH MCP Todo: Copilot client failed to load. ${msg}`);
        return;
    }
    // Cancel any previously running operation
    activeCopilotAbort?.abort();
    const abort = new AbortController();
    activeCopilotAbort = abort;
    vscode.commands.executeCommand('setContext', 'fwhMcpTodo.copilotRunning', true);
    try {
        (0, logger_1.copilotLog)(`>>> Prompt (${action} ${id}):\n${prompt}`);
        vscode.window.setStatusBarMessage(`FWH MCP Todo: ${action} ${id}…`, 5000);
        // Create a temp markdown file for live output
        const tempDir = path.join(os.tmpdir(), 'FWH-McpTodo');
        fs.mkdirSync(tempDir, { recursive: true });
        const mdPath = path.join(tempDir, `${action}-${id}-${new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19)}.md`);
        const header = `# ${action}: ${id}\n\n_Running…_\n`;
        fs.writeFileSync(mdPath, header);
        // Open the file in VS Code editor
        const doc = await vscode.workspace.openTextDocument(mdPath);
        await vscode.window.showTextDocument(doc, { preview: false });
        let firstLine = true;
        const workspaceDir = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
        const result = await invokeCopilot(prompt, {
            timeoutMs: 120_000,
            cwd: workspaceDir,
            signal: abort.signal,
            log: (msg) => (0, logger_1.copilotLog)(msg),
            onStdoutLine: (line) => {
                try {
                    const edit = new vscode.WorkspaceEdit();
                    if (firstLine) {
                        // Replace "Running…" placeholder with first real line
                        const fullRange = new vscode.Range(doc.positionAt(0), doc.positionAt(doc.getText().length));
                        edit.replace(doc.uri, fullRange, `# ${action}: ${id}\n\n${line}\n`);
                        firstLine = false;
                    }
                    else {
                        // Append at end of document
                        const endPos = doc.positionAt(doc.getText().length);
                        edit.insert(doc.uri, endPos, line + '\n');
                    }
                    vscode.workspace.applyEdit(edit);
                }
                catch { /* editor race — swallow */ }
            },
        });
        // Write final content to disk so file is saved
        fs.writeFileSync(mdPath, doc.getText());
        (0, logger_1.copilotLog)(`<<< ${result.state} (${action} ${id}):\n${result.body}`);
        if (result.stderr)
            (0, logger_1.copilotLog)(`<<< Stderr:\n${result.stderr}`);
        if (result.state === CopilotResultState.Cancelled) {
            vscode.window.setStatusBarMessage(`FWH MCP Todo: ${action} ${id} stopped`, 5000);
        }
        else if (result.state === CopilotResultState.Success) {
            vscode.window.setStatusBarMessage(`FWH MCP Todo: ${action} ${id} complete`, 5000);
            const logUri = vscode.Uri.file(mdPath);
            vscode.window.showInformationMessage(`${action} ${id} complete`, 'Show Log').then((choice) => {
                if (choice === 'Show Log') {
                    vscode.workspace.openTextDocument(logUri).then((d) => vscode.window.showTextDocument(d, { preview: false }), () => { });
                }
            });
        }
        else {
            (0, logger_1.copilotLog)(`<<< Warning (${action} ${id}): ${result.stderr || result.body}`);
        }
    }
    catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        (0, logger_1.copilotLog)(`<<< Error (${action} ${id}): ${msg}`);
        (0, logger_1.log)(`sendCopilotPrompt(${action}) error`, msg);
    }
    finally {
        if (activeCopilotAbort === abort) {
            activeCopilotAbort = null;
            vscode.commands.executeCommand('setContext', 'fwhMcpTodo.copilotRunning', false);
        }
    }
}
function activate(context) {
    // Create output channel and log immediately so we know activation ran
    (0, logger_1.log)('activate() called');
    // Only fully initialise when the FunWasHad solution is in the workspace
    const isFunWasHad = vscode.workspace.workspaceFolders?.some((f) => {
        const fs = require('fs');
        const path = require('path');
        return fs.existsSync(path.join(f.uri.fsPath, 'FunWasHad.sln'));
    });
    if (!isFunWasHad) {
        (0, logger_1.log)('FunWasHad.sln not found in workspace — extension inactive.');
        return;
    }
    try {
        const provider = new todoTree_1.TodoTreeDataProvider();
        const treeView = vscode.window.createTreeView('fwhMcpTodo.todoList', {
            treeDataProvider: provider,
        });
        context.subscriptions.push(treeView);
        const updateFilterDescription = () => {
            const p = provider.filterPriority;
            const t = provider.filterText;
            const parts = [];
            if (p)
                parts.push(`Priority: ${p}`);
            if (t)
                parts.push(`Text: "${t}"`);
            treeView.description = parts.length ? parts.join(' · ') : undefined;
        };
        const filterPanel = new filterPanel_1.FilterPanelProvider(context, provider, updateFilterDescription);
        context.subscriptions.push(vscode.window.registerWebviewViewProvider('fwhMcpTodo.filters', filterPanel));
        (0, logger_1.log)('Tree view created for fwhMcpTodo.todoList');
        // Auto-refresh when view becomes visible so data loads even if title-bar refresh isn’t triggered
        treeView.onDidChangeVisibility((e) => {
            if (e.visible) {
                (0, logger_1.log)('Tree view visible, triggering refresh');
                void provider.refresh();
            }
        });
        // Initial load: getChildren(root) is called before we've ever fetched, so _items is [].
        // Ensure MCP server is running, then trigger refresh to fetch from MCP.
        (0, logger_1.log)('Ensuring MCP server is running');
        (0, mcpClient_1.ensureMcpServerRunning)().then((serverWasStarted) => {
            (0, logger_1.log)(`MCP server check complete (started=${serverWasStarted}), triggering refresh`);
            void provider.refresh();
        }, (err) => {
            (0, logger_1.log)('ensureMcpServerRunning failed, refreshing anyway', String(err));
            void provider.refresh();
        });
        const contentProvider = new todoDocument_1.TodoContentProvider();
        const fsProvider = new todoDocument_1.TodoFileSystemProvider();
        context.subscriptions.push(vscode.workspace.registerTextDocumentContentProvider('fwh-todo', contentProvider));
        context.subscriptions.push(vscode.workspace.registerFileSystemProvider('fwh-todo', fsProvider, { isCaseSensitive: true }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.refresh', () => {
            (0, logger_1.log)('Command fwhMcpTodo.refresh invoked');
            vscode.window.setStatusBarMessage('FWH MCP Todo: Refreshing…', 2000);
            void provider.refresh();
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.newTodo', () => {
            (0, logger_1.log)('Command fwhMcpTodo.newTodo invoked');
            const uri = (0, todoDocument_1.newTodoUri)();
            vscode.workspace.openTextDocument(uri).then((doc) => vscode.window.showTextDocument(doc, { preview: false }), (err) => {
                (0, logger_1.log)('newTodo failed', String(err));
            });
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.openInEditor', (arg) => {
            const id = typeof arg === 'string' ? arg : arg?.id;
            (0, logger_1.log)('Command fwhMcpTodo.openInEditor invoked', { id });
            if (!id || typeof id !== 'string')
                return;
            const uri = (0, todoDocument_1.todoUri)(id);
            vscode.workspace.openTextDocument(uri).then((doc) => vscode.window.showTextDocument(doc, { preview: false }), (err) => {
                (0, logger_1.log)('openInEditor failed', String(err));
            });
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.copyId', (arg) => {
            const id = typeof arg === 'string' ? arg : arg?.id;
            (0, logger_1.log)('Command fwhMcpTodo.copyId invoked', { id });
            if (id && typeof id === 'string') {
                vscode.env.clipboard.writeText(id).then(() => {
                    vscode.window.setStatusBarMessage(`Copied ${id} to clipboard`, 2000);
                });
            }
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.status', async (arg) => {
            const id = typeof arg === 'string' ? arg : arg?.id;
            (0, logger_1.log)('Command fwhMcpTodo.status invoked', { id });
            if (!id)
                return;
            const prompt = `Get the current status of TODO ${id} from the local MCP server at http://localhost:7147. Use: curl http://localhost:7147/mcp/todo/${id} to retrieve the item. Report the title, priority, section, done status, description, technical details, implementation tasks with completion status, and any blockers or next steps.`;
            await sendCopilotPrompt(id, 'Status', prompt);
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.implement', async (arg) => {
            const id = typeof arg === 'string' ? arg : arg?.id;
            (0, logger_1.log)('Command fwhMcpTodo.implement invoked', { id });
            if (!id)
                return;
            const prompt = [
                `Implement TODO ${id}. Follow this procedure:`,
                ``,
                `1. RETRIEVE: Fetch the full TODO from the local MCP server:`,
                `   curl http://localhost:7147/mcp/todo/${id}`,
                `   Note the implementationTasks array — each entry has { task, done }.`,
                ``,
                `2. IMPLEMENT TASKS: Work through each implementationTask that has done=false.`,
                `   After completing each task, immediately update the TODO via PUT to mark`,
                `   that specific task done. Send the FULL implementationTasks array with the`,
                `   completed task's done field set to true:`,
                `   curl -X PUT http://localhost:7147/mcp/todo/${id} \\`,
                `     -H "Content-Type: application/json" \\`,
                `     -d '{"implementationTasks": [ ...full array with updated done flags... ]}'`,
                `   This makes progress visible in the tree view in real time.`,
                ``,
                `3. UPDATE DEPENDENTS: After all tasks are complete, query all TODOs:`,
                `   curl http://localhost:7147/mcp/todo`,
                `   Find any TODO whose dependsOn array contains "${id}". For each dependent:`,
                `   - Update its technicalDetails or note to reflect that ${id} is now complete.`,
                `   - If all of the dependent's own dependencies are satisfied, update its`,
                `     remaining estimate and note accordingly.`,
                ``,
                `4. MARK DONE: When all implementationTasks are done, mark the TODO itself done:`,
                `   curl -X PUT http://localhost:7147/mcp/todo/${id} \\`,
                `     -H "Content-Type: application/json" \\`,
                `     -d '{"done": true}'`,
                ``,
                `5. Update the session log throughout. Run to completion, do not wait for user.`,
            ].join('\n');
            await sendCopilotPrompt(id, 'Implement', prompt);
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.plan', async (arg) => {
            const id = typeof arg === 'string' ? arg : arg?.id;
            (0, logger_1.log)('Command fwhMcpTodo.plan invoked', { id });
            if (!id)
                return;
            const prompt = `Create an implementation plan in excruciating detail as a new TODO that TODO ${id} depends on. First retrieve the full details of ${id} from the local MCP server using: curl http://localhost:7147/mcp/todo/${id}. Then create a new TODO via POST http://localhost:7147/mcp/todo with the detailed plan. Finally update ${id} via PUT http://localhost:7147/mcp/todo/${id} to add the new plan TODO as a dependency.`;
            await sendCopilotPrompt(id, 'Plan', prompt);
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.showOutput', () => {
            (0, logger_1.log)('Command fwhMcpTodo.showOutput invoked');
            (0, logger_1.show)();
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.stop', () => {
            (0, logger_1.log)('Command fwhMcpTodo.stop invoked');
            activeCopilotAbort?.abort();
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.filters', async () => {
            const choice = await vscode.window.showQuickPick([
                { label: '$(filter) Filter by priority', value: 'priority' },
                { label: '$(search) Filter by text', value: 'text' },
                { label: '$(clear-all) Clear filters', value: 'clear' },
            ], { title: 'FWH MCP Todo: Filters', placeHolder: 'Choose filter action' });
            if (choice?.value === 'priority')
                await vscode.commands.executeCommand('fwhMcpTodo.filterByPriority');
            else if (choice?.value === 'text')
                await vscode.commands.executeCommand('fwhMcpTodo.filterByText');
            else if (choice?.value === 'clear')
                await vscode.commands.executeCommand('fwhMcpTodo.clearFilters');
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.filterByPriority', async () => {
            const current = provider.filterPriority || 'All';
            const choice = await vscode.window.showQuickPick([
                { label: 'All', value: '' },
                { label: 'High', value: 'high' },
                { label: 'Medium', value: 'medium' },
                { label: 'Low', value: 'low' },
            ], { title: 'Filter by priority', placeHolder: current ? `Current: ${current}` : 'All' });
            if (choice !== undefined) {
                provider.setFilterPriority(choice.value);
                updateFilterDescription();
                filterPanel.updateFilterState();
                vscode.window.setStatusBarMessage(choice.value ? `FWH MCP Todo: priority = ${choice.value}` : 'FWH MCP Todo: priority = all', 2000);
            }
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.filterByText', async () => {
            const current = provider.filterText;
            const value = await vscode.window.showInputBox({
                title: 'Filter by text',
                placeHolder: 'Search in id, title, description…',
                value: current,
            });
            if (value !== undefined) {
                provider.setFilterText(value);
                updateFilterDescription();
                filterPanel.updateFilterState();
                vscode.window.setStatusBarMessage(value ? `FWH MCP Todo: filter "${value}"` : 'FWH MCP Todo: text filter cleared', 2000);
            }
        }));
        context.subscriptions.push(vscode.commands.registerCommand('fwhMcpTodo.clearFilters', () => {
            provider.setFilterPriority('');
            provider.setFilterText('');
            provider.setFilterTextScope('title');
            updateFilterDescription();
            filterPanel.updateFilterState();
            vscode.window.setStatusBarMessage('FWH MCP Todo: filters cleared', 2000);
        }));
        (0, logger_1.log)('activate() completed (initial refresh in progress)');
    }
    catch (err) {
        (0, logger_1.log)('activate() failed', String(err));
        console.error('FWH MCP Todo: activate failed', err);
    }
}
function deactivate() { }
//# sourceMappingURL=extension.js.map