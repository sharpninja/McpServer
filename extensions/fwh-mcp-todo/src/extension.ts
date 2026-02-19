/**
 * FWH MCP Todo: shows TODO items from the MCP server in a tree view.
 * Double-click opens the todo in an editor as markdown; saving pushes updates to MCP.
 * All interactions are traced to Output → FWH MCP Todo.
 */

import * as vscode from 'vscode';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { TodoTreeDataProvider } from './todoTree';
import { FilterPanelProvider } from './filterPanel';
import { TodoContentProvider, TodoFileSystemProvider, todoUri, newTodoUri } from './todoDocument';
import { log, show, copilotLog } from './logger';
import { ensureMcpServerRunning } from './mcpClient';

let activeCopilotAbort: AbortController | null = null;

async function sendCopilotPrompt(id: string, action: string, prompt: string): Promise<void> {
  let invokeCopilot: (p: string, opts?: unknown) => Promise<{ state: string; body?: string; stderr?: string }>;
  let CopilotResultState: { Cancelled: string; Success: string };
  try {
    const client = await import('fwh-copilot-client');
    invokeCopilot = client.invokeCopilot as (p: string, opts?: unknown) => Promise<{ state: string; body?: string; stderr?: string }>;
    CopilotResultState = client.CopilotResultState;
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    log('fwh-copilot-client failed to load', msg);
    copilotLog(`Copilot unavailable: ${msg}`);
    vscode.window.showErrorMessage(`FWH MCP Todo: Copilot client failed to load. ${msg}`);
    return;
  }

  // Cancel any previously running operation
  activeCopilotAbort?.abort();
  const abort = new AbortController();
  activeCopilotAbort = abort;
  vscode.commands.executeCommand('setContext', 'fwhMcpTodo.copilotRunning', true);

  try {
    copilotLog(`>>> Prompt (${action} ${id}):\n${prompt}`);
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
      log: (msg: string) => copilotLog(msg),
      onStdoutLine: (line: string) => {
        try {
          const edit = new vscode.WorkspaceEdit();
          if (firstLine) {
            // Replace "Running…" placeholder with first real line
            const fullRange = new vscode.Range(doc.positionAt(0), doc.positionAt(doc.getText().length));
            edit.replace(doc.uri, fullRange, `# ${action}: ${id}\n\n${line}\n`);
            firstLine = false;
          } else {
            // Append at end of document
            const endPos = doc.positionAt(doc.getText().length);
            edit.insert(doc.uri, endPos, line + '\n');
          }
          vscode.workspace.applyEdit(edit);
        } catch { /* editor race — swallow */ }
      },
    });

    // Write final content to disk so file is saved
    fs.writeFileSync(mdPath, doc.getText());

    copilotLog(`<<< ${result.state} (${action} ${id}):\n${result.body}`);
    if (result.stderr) copilotLog(`<<< Stderr:\n${result.stderr}`);
    if (result.state === CopilotResultState.Cancelled) {
      vscode.window.setStatusBarMessage(`FWH MCP Todo: ${action} ${id} stopped`, 5000);
    } else if (result.state === CopilotResultState.Success) {
      vscode.window.setStatusBarMessage(`FWH MCP Todo: ${action} ${id} complete`, 5000);
      const logUri = vscode.Uri.file(mdPath);
      vscode.window.showInformationMessage(
        `${action} ${id} complete`,
        'Show Log'
      ).then((choice) => {
        if (choice === 'Show Log') {
          vscode.workspace.openTextDocument(logUri).then(
            (d) => vscode.window.showTextDocument(d, { preview: false }),
            () => { /* file may have been deleted */ }
          );
        }
      });
    } else {
      copilotLog(`<<< Warning (${action} ${id}): ${result.stderr || result.body}`);
    }
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    copilotLog(`<<< Error (${action} ${id}): ${msg}`);
    log(`sendCopilotPrompt(${action}) error`, msg);
  } finally {
    if (activeCopilotAbort === abort) {
      activeCopilotAbort = null;
      vscode.commands.executeCommand('setContext', 'fwhMcpTodo.copilotRunning', false);
    }
  }
}

export function activate(context: vscode.ExtensionContext): void {
  // Create output channel and log immediately so we know activation ran
  log('activate() called');

  // Only fully initialise when the FunWasHad solution is in the workspace
  const isFunWasHad = vscode.workspace.workspaceFolders?.some((f) => {
    const fs = require('fs') as typeof import('fs');
    const path = require('path') as typeof import('path');
    return fs.existsSync(path.join(f.uri.fsPath, 'FunWasHad.sln'));
  });
  if (!isFunWasHad) {
    log('FunWasHad.sln not found in workspace — extension inactive.');
    return;
  }

  try {
    const provider = new TodoTreeDataProvider();

    const treeView = vscode.window.createTreeView('fwhMcpTodo.todoList', {
      treeDataProvider: provider,
    });
    context.subscriptions.push(treeView);

    const updateFilterDescription = (): void => {
      const p = provider.filterPriority;
      const t = provider.filterText;
      const parts = [];
      if (p) parts.push(`Priority: ${p}`);
      if (t) parts.push(`Text: "${t}"`);
      treeView.description = parts.length ? parts.join(' · ') : undefined;
    };

    const filterPanel = new FilterPanelProvider(provider, updateFilterDescription);
    context.subscriptions.push(
      vscode.window.registerWebviewViewProvider('fwhMcpTodo.filters', filterPanel)
    );

    log('Tree view created for fwhMcpTodo.todoList');

    // Auto-refresh when view becomes visible so data loads even if title-bar refresh isn’t triggered
    treeView.onDidChangeVisibility((e) => {
      if (e.visible) {
        log('Tree view visible, triggering refresh');
        void provider.refresh();
      }
    });

    // Initial load: getChildren(root) is called before we've ever fetched, so _items is [].
    // Ensure MCP server is running, then trigger refresh to fetch from MCP.
    log('Ensuring MCP server is running');
    ensureMcpServerRunning().then((serverWasStarted) => {
      log(`MCP server check complete (started=${serverWasStarted}), triggering refresh`);
      void provider.refresh();
    }, (err) => {
      log('ensureMcpServerRunning failed, refreshing anyway', String(err));
      void provider.refresh();
    });

    const contentProvider = new TodoContentProvider();
    const fsProvider = new TodoFileSystemProvider();
    context.subscriptions.push(
      vscode.workspace.registerTextDocumentContentProvider('fwh-todo', contentProvider)
    );
    context.subscriptions.push(
      vscode.workspace.registerFileSystemProvider('fwh-todo', fsProvider, { isCaseSensitive: true })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.refresh', () => {
        log('Command fwhMcpTodo.refresh invoked');
        vscode.window.setStatusBarMessage('FWH MCP Todo: Refreshing…', 2000);
        void provider.refresh();
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.newTodo', () => {
        log('Command fwhMcpTodo.newTodo invoked');
        const uri = newTodoUri();
        vscode.workspace.openTextDocument(uri).then(
          (doc) => vscode.window.showTextDocument(doc, { preview: false }),
          (err) => {
            log('newTodo failed', String(err));
          }
        );
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.openInEditor', (arg: string | { id: string }) => {
        const id = typeof arg === 'string' ? arg : arg?.id;
        log('Command fwhMcpTodo.openInEditor invoked', { id });
        if (!id || typeof id !== 'string') return;
        const uri = todoUri(id);
        vscode.workspace.openTextDocument(uri).then(
          (doc) => vscode.window.showTextDocument(doc, { preview: false }),
          (err) => {
            log('openInEditor failed', String(err));
          }
        );
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.copyId', (arg: string | { id: string }) => {
        const id = typeof arg === 'string' ? arg : arg?.id;
        log('Command fwhMcpTodo.copyId invoked', { id });
        if (id && typeof id === 'string') {
          vscode.env.clipboard.writeText(id).then(() => {
            vscode.window.setStatusBarMessage(`Copied ${id} to clipboard`, 2000);
          });
        }
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.status', async (arg: string | { id: string }) => {
        const id = typeof arg === 'string' ? arg : arg?.id;
        log('Command fwhMcpTodo.status invoked', { id });
        if (!id) return;
        const prompt = `Get the current status of TODO ${id} from the local MCP server at http://localhost:7147. Use: curl http://localhost:7147/mcp/todo/${id} to retrieve the item. Report the title, priority, section, done status, description, technical details, implementation tasks with completion status, and any blockers or next steps.`;
        await sendCopilotPrompt(id, 'Status', prompt);
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.implement', async (arg: string | { id: string }) => {
        const id = typeof arg === 'string' ? arg : arg?.id;
        log('Command fwhMcpTodo.implement invoked', { id });
        if (!id) return;
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
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.plan', async (arg: string | { id: string }) => {
        const id = typeof arg === 'string' ? arg : arg?.id;
        log('Command fwhMcpTodo.plan invoked', { id });
        if (!id) return;
        const prompt = `Create an implementation plan in excruciating detail as a new TODO that TODO ${id} depends on. First retrieve the full details of ${id} from the local MCP server using: curl http://localhost:7147/mcp/todo/${id}. Then create a new TODO via POST http://localhost:7147/mcp/todo with the detailed plan. Finally update ${id} via PUT http://localhost:7147/mcp/todo/${id} to add the new plan TODO as a dependency.`;
        await sendCopilotPrompt(id, 'Plan', prompt);
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.showOutput', () => {
        log('Command fwhMcpTodo.showOutput invoked');
        show();
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.stop', () => {
        log('Command fwhMcpTodo.stop invoked');
        activeCopilotAbort?.abort();
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.filters', async () => {
        const choice = await vscode.window.showQuickPick(
          [
            { label: '$(filter) Filter by priority', value: 'priority' },
            { label: '$(search) Filter by text', value: 'text' },
            { label: '$(clear-all) Clear filters', value: 'clear' },
          ],
          { title: 'FWH MCP Todo: Filters', placeHolder: 'Choose filter action' }
        );
        if (choice?.value === 'priority') await vscode.commands.executeCommand('fwhMcpTodo.filterByPriority');
        else if (choice?.value === 'text') await vscode.commands.executeCommand('fwhMcpTodo.filterByText');
        else if (choice?.value === 'clear') await vscode.commands.executeCommand('fwhMcpTodo.clearFilters');
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.filterByPriority', async () => {
        const current = provider.filterPriority || 'All';
        const choice = await vscode.window.showQuickPick(
          [
            { label: 'All', value: '' },
            { label: 'High', value: 'high' },
            { label: 'Medium', value: 'medium' },
            { label: 'Low', value: 'low' },
          ],
          { title: 'Filter by priority', placeHolder: current ? `Current: ${current}` : 'All' }
        );
        if (choice !== undefined) {
          provider.setFilterPriority(choice.value);
          updateFilterDescription();
          filterPanel.updateFilterState();
          vscode.window.setStatusBarMessage(
            choice.value ? `FWH MCP Todo: priority = ${choice.value}` : 'FWH MCP Todo: priority = all',
            2000
          );
        }
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.filterByText', async () => {
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
          vscode.window.setStatusBarMessage(
            value ? `FWH MCP Todo: filter "${value}"` : 'FWH MCP Todo: text filter cleared',
            2000
          );
        }
      })
    );

    context.subscriptions.push(
      vscode.commands.registerCommand('fwhMcpTodo.clearFilters', () => {
        provider.setFilterPriority('');
        provider.setFilterText('');
        provider.setFilterTextScope('title');
        updateFilterDescription();
        filterPanel.updateFilterState();
        vscode.window.setStatusBarMessage('FWH MCP Todo: filters cleared', 2000);
      })
    );

    log('activate() completed (initial refresh in progress)');
  } catch (err) {
    log('activate() failed', String(err));
    console.error('FWH MCP Todo: activate failed', err);
  }
}

export function deactivate(): void {}
