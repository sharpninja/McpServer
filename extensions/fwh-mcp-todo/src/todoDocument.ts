/**
 * Virtual documents for TODO items: fwh-todo://todo/{id}
 * - ContentProvider supplies initial markdown when opening.
 * - FileSystemProvider writeFile handles Save and pushes to MCP.
 */

import * as vscode from 'vscode';
import { log, copilotLog } from './logger';
import { todoToMarkdown, markdownToUpdateBody, blankTodoTemplate, markdownToNewTodoFields } from './todoMarkdown';

const SCHEME = 'fwh-todo';
const AUTHORITY = 'todo';
const NEW_AUTHORITY = 'new';

function todoIdFromUri(uri: vscode.Uri): string | null {
  if (uri.scheme !== SCHEME || !uri.path.startsWith('/')) return null;
  if (uri.authority !== AUTHORITY && uri.authority !== NEW_AUTHORITY) return null;
  return decodeURIComponent(uri.path.slice(1));
}

function isNewTodoUri(uri: vscode.Uri): boolean {
  return uri.scheme === SCHEME && uri.authority === NEW_AUTHORITY;
}

export function todoUri(id: string): vscode.Uri {
  return vscode.Uri.parse(`${SCHEME}://${AUTHORITY}/${encodeURIComponent(id)}`);
}

export function newTodoUri(): vscode.Uri {
  return vscode.Uri.parse(`${SCHEME}://${NEW_AUTHORITY}/new-todo`);
}

/**
 * Provides initial markdown when opening a todo URI.
 */
export class TodoContentProvider implements vscode.TextDocumentContentProvider {
  async provideTextDocumentContent(uri: vscode.Uri): Promise<string> {
    if (isNewTodoUri(uri)) {
      return blankTodoTemplate();
    }
    const id = todoIdFromUri(uri);
    if (!id) return '# Invalid URI';
    try {
      const { fetchTodoById } = await import('./mcpClient');
      const item = await fetchTodoById(id);
      if (!item) return `# ${id}\n\n(Not found on server.)`;
      return todoToMarkdown(item);
    } catch (e) {
      log('TodoContentProvider.provideTextDocumentContent error', String(e));
      return `# ${id}\n\n(Error loading: ${e instanceof Error ? e.message : String(e)})`;
    }
  }
}

/**
 * Handles Save: writes go to MCP via PUT /mcp/todo/{id}.
 */
export class TodoFileSystemProvider implements vscode.FileSystemProvider {
  private _emitter = new vscode.EventEmitter<vscode.FileChangeEvent[]>();
  readonly onDidChangeFile = this._emitter.event;

  watch(_uri: vscode.Uri): vscode.Disposable {
    return new vscode.Disposable(() => {});
  }

  stat(uri: vscode.Uri): vscode.FileStat {
    const id = todoIdFromUri(uri);
    if (!id) throw vscode.FileSystemError.FileNotFound(uri);
    return {
      type: vscode.FileType.File,
      ctime: Date.now(),
      mtime: Date.now(),
      size: 0,
    };
  }

  readDirectory(_uri: vscode.Uri): [string, vscode.FileType][] {
    return [];
  }

  createDirectory(_uri: vscode.Uri): void {
    throw vscode.FileSystemError.NoPermissions();
  }

  readFile(uri: vscode.Uri): Uint8Array | Thenable<Uint8Array> {
    if (isNewTodoUri(uri)) {
      return Buffer.from(blankTodoTemplate(), 'utf8');
    }
    const id = todoIdFromUri(uri);
    if (!id) throw vscode.FileSystemError.FileNotFound(uri);
    return (async () => {
      try {
        const { fetchTodoById } = await import('./mcpClient');
        const item = await fetchTodoById(id);
        const markdown = item ? todoToMarkdown(item) : `# ${id}\n\n(Not found on server.)`;
        return Buffer.from(markdown, 'utf8');
      } catch (e) {
        log('TodoFileSystemProvider.readFile error', String(e));
        const markdown = `# ${id}\n\n(Error loading: ${e instanceof Error ? e.message : String(e)})`;
        return Buffer.from(markdown, 'utf8');
      }
    })();
  }

  async writeFile(
    uri: vscode.Uri,
    content: Uint8Array,
    _options: { create: boolean; overwrite: boolean }
  ): Promise<void> {
    const text = Buffer.from(content).toString('utf8');

    // New todo: build CLI command and invoke via copilot client
    if (isNewTodoUri(uri)) {
      const fields = markdownToNewTodoFields(text);
      if (!fields.title || fields.title.trim() === '') {
        log('TodoFileSystemProvider.writeFile: title is required for new todo');
        return;
      }
      const priority = fields.priority || 'low';
      const command = `add ${priority} todo: ${fields.title.trim()}`;
      log('TodoFileSystemProvider.writeFile: invoking copilot for new todo', { command });

      try {
        const { invokeCopilot } = await import('fwh-copilot-client');
        copilotLog(`>>> Prompt (New Todo):\n${command}`);
        const result = await invokeCopilot(command, { timeoutMs: 60_000 });
        copilotLog(`<<< ${result.state} (New Todo):\n${result.body}`);
        if (result.stderr) copilotLog(`<<< Stderr:\n${result.stderr}`);
        if (result.state === 'success') {
          vscode.window.setStatusBarMessage(`FWH MCP Todo: Created — ${result.body.substring(0, 80)}`, 5000);
          log('TodoFileSystemProvider.writeFile: new todo created, refreshing tree');
          await vscode.commands.executeCommand('fwhMcpTodo.refresh');
        } else {
          copilotLog(`<<< Error (New Todo): ${result.state} — ${result.stderr || result.body}`);
        }
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        log('TodoFileSystemProvider.writeFile copilot error', msg);
        // Fallback to terminal
        const terminal = vscode.window.createTerminal({ name: 'FWH MCP Todo' });
        terminal.show(true);
        terminal.sendText(command, true);
        vscode.window.setStatusBarMessage(`FWH MCP Todo: Sent "${command}" to terminal (copilot unavailable)`, 5000);
      }
      return;
    }

    const id = todoIdFromUri(uri);
    if (!id) {
      log('TodoFileSystemProvider.writeFile: invalid uri', uri.toString());
      throw vscode.FileSystemError.FileNotFound(uri);
    }
    const body = markdownToUpdateBody(text);
    log('TodoFileSystemProvider.writeFile: pushing to MCP', { id, bodyKeys: Object.keys(body) });
    try {
      const { updateTodo } = await import('./mcpClient');
      const result = await updateTodo(id, body);
      if (result.success) {
        vscode.window.setStatusBarMessage(`FWH MCP Todo: ${id} updated`, 3000);
        log(`TodoFileSystemProvider.writeFile: ${id} updated, refreshing tree`);
        await vscode.commands.executeCommand('fwhMcpTodo.refresh');
      } else {
        log(`TodoFileSystemProvider.writeFile: update failed for ${id}`, result.error ?? 'unknown');
      }
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      log('TodoFileSystemProvider.writeFile error', msg);
      throw e;
    }
  }

  delete(_uri: vscode.Uri): void {
    throw vscode.FileSystemError.NoPermissions();
  }

  rename(_old: vscode.Uri, _new: vscode.Uri): void {
    throw vscode.FileSystemError.NoPermissions();
  }
}
