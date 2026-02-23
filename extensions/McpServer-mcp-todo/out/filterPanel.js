"use strict";
/**
 * Filter panel: webview with Priority dropdown, Text scope (ID/TITLE/ALL), Text input, and Clear button.
 * Syncs with TodoTreeDataProvider and stays in sync when filters are changed via commands.
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.FilterPanelProvider = void 0;
function escapeHtml(s) {
    return s
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}
function getHtml(webview, state) {
    const priority = escapeHtml(state.priority);
    const text = escapeHtml(state.text);
    const textScope = escapeHtml(state.textScope);
    const selId = textScope === 'id' ? 'selected' : '';
    const selTitle = textScope === 'title' ? 'selected' : '';
    const selAll = textScope === 'all' ? 'selected' : '';
    return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <style>
    * { box-sizing: border-box; }
    body { margin: 0; padding: 4px 8px 6px; font-family: var(--vscode-font-family); font-size: 13px; }
    .row { display: flex; align-items: center; gap: 8px; margin-bottom: 6px; }
    .row:last-of-type { margin-bottom: 0; }
    label { color: var(--vscode-foreground); margin-right: 4px; flex-shrink: 0; }
    select, input[type="text"] {
      background: var(--vscode-input-background);
      color: var(--vscode-input-foreground);
      border: 1px solid var(--vscode-input-border);
      padding: 3px 6px;
      border-radius: 2px;
      min-width: 80px;
    }
    input[type="text"] { flex: 1; min-width: 0; }
    button {
      background: var(--vscode-button-secondaryBackground);
      color: var(--vscode-button-secondaryForeground);
      border: 1px solid var(--vscode-button-border);
      padding: 3px 10px;
      border-radius: 2px;
      cursor: pointer;
    }
    button:hover { background: var(--vscode-button-secondaryHoverBackground); }
  </style>
</head>
<body>
  <div class="row">
    <label for="priority">Priority</label>
    <select id="priority" title="Filter by priority">
      <option value="" ${priority === '' ? 'selected' : ''}>All</option>
      <option value="high" ${priority === 'high' ? 'selected' : ''}>High</option>
      <option value="medium" ${priority === 'medium' ? 'selected' : ''}>Medium</option>
      <option value="low" ${priority === 'low' ? 'selected' : ''}>Low</option>
    </select>
    <button id="clear" title="Clear filters">Clear</button>
  </div>
  <div class="row">
    <label for="scope">Scope</label>
    <select id="scope" title="Text filter scope">
      <option value="id" ${selId}>ID</option>
      <option value="title" ${selTitle}>TITLE</option>
      <option value="all" ${selAll}>ALL</option>
    </select>
    <label for="text">Text</label>
    <input type="text" id="text" placeholder="Search…" value="${text}" title="Filter by text" />
  </div>
  <script>
    (function() {
      const vscode = acquireVsCodeApi();
      const priorityEl = document.getElementById('priority');
      const scopeEl = document.getElementById('scope');
      const textEl = document.getElementById('text');

      function sendPriority() { vscode.postMessage({ type: 'priority', value: priorityEl.value }); }
      function sendScope() { vscode.postMessage({ type: 'textScope', value: scopeEl.value }); }
      function sendText() { vscode.postMessage({ type: 'text', value: textEl.value }); }

      priorityEl.addEventListener('change', sendPriority);
      scopeEl.addEventListener('change', sendScope);
      textEl.addEventListener('input', sendText);
      textEl.addEventListener('change', sendText);
      document.getElementById('clear').addEventListener('click', function() {
        vscode.postMessage({ type: 'clear' });
      });

      window.addEventListener('message', function(event) {
        const msg = event.data;
        if (msg.type === 'setState') {
          if (priorityEl.value !== msg.priority) { priorityEl.value = msg.priority || ''; }
          if (scopeEl.value !== msg.textScope) { scopeEl.value = msg.textScope || 'title'; }
          if (textEl.value !== msg.text) { textEl.value = msg.text || ''; }
        }
      });
    })();
  </script>
</body>
</html>`;
}
class FilterPanelProvider {
    _provider;
    _onFilterChange;
    _view;
    constructor(_provider, _onFilterChange) {
        this._provider = _provider;
        this._onFilterChange = _onFilterChange;
    }
    resolveWebviewView(webviewView, _context, _token) {
        this._view = webviewView;
        webviewView.webview.options = { enableScripts: true };
        const state = {
            priority: this._provider.filterPriority,
            text: this._provider.filterText,
            textScope: this._provider.filterTextScope,
        };
        webviewView.webview.html = getHtml(webviewView.webview, state);
        webviewView.webview.onDidReceiveMessage((msg) => {
            switch (msg.type) {
                case 'priority':
                    this._provider.setFilterPriority(msg.value ?? '');
                    this._onFilterChange();
                    break;
                case 'text':
                    this._provider.setFilterText(msg.value ?? '');
                    this._onFilterChange();
                    break;
                case 'textScope':
                    this._provider.setFilterTextScope(msg.value ?? 'title');
                    this._onFilterChange();
                    break;
                case 'clear':
                    this._provider.setFilterPriority('');
                    this._provider.setFilterText('');
                    this._provider.setFilterTextScope('title');
                    this._onFilterChange();
                    break;
            }
        });
    }
    /** Call when filters were changed from commands so the panel controls stay in sync. */
    updateFilterState() {
        if (!this._view)
            return;
        this._view.webview.postMessage({
            type: 'setState',
            priority: this._provider.filterPriority,
            text: this._provider.filterText,
            textScope: this._provider.filterTextScope,
        });
    }
}
exports.FilterPanelProvider = FilterPanelProvider;
//# sourceMappingURL=filterPanel.js.map