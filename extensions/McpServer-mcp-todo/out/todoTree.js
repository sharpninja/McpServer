"use strict";
/**
 * Tree data provider for MCP TODO items.
 * Groups by priority; shows priority on each item. Supports filter by priority and filter by text.
 * MCP client is lazy-loaded on first refresh.
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
exports.TodoTreeDataProvider = void 0;
const vscode = __importStar(require("vscode"));
const logger_1 = require("./logger");
const PRIORITY_ORDER = ['high', 'medium', 'low'];
function isPriorityNode(node) {
    return typeof node === 'object' && node !== null && 'kind' in node && node.kind === 'priority';
}
function searchableForScope(item, scope) {
    const strings = (s) => typeof s === 'string' && s.length > 0;
    switch (scope) {
        case 'id':
            return item.id ? [item.id] : [];
        case 'title':
            return item.title ? [item.title] : [];
        case 'all':
        default:
            return [
                item.id,
                item.title,
                item.section,
                item.priority,
                item.note,
                item.estimate,
                item.remaining,
                ...(item.description ?? []),
                ...(item.technicalDetails ?? []),
                ...(item.implementationTasks?.map((t) => t.task) ?? []),
            ].filter(strings);
    }
}
function matchesText(item, text, scope) {
    if (!text || !text.trim())
        return true;
    const searchable = searchableForScope(item, scope).join(' ').toLowerCase();
    const words = text.trim().toLowerCase().split(/\s+/);
    return words.every((word) => searchable.includes(word));
}
function matchesPriority(item, filterPriority) {
    if (!filterPriority || !filterPriority.trim())
        return true;
    return item.priority?.toLowerCase() === filterPriority.trim().toLowerCase();
}
function priorityKey(p) {
    const k = (p || 'other').trim().toLowerCase();
    return k || 'other';
}
class TodoTreeDataProvider {
    _onDidChangeTreeData = new vscode.EventEmitter();
    onDidChangeTreeData = this._onDidChangeTreeData.event;
    _items = [];
    _error;
    _filterPriority = '';
    _filterText = '';
    _filterTextScope = 'title';
    get filterPriority() {
        return this._filterPriority;
    }
    get filterText() {
        return this._filterText;
    }
    get filterTextScope() {
        return this._filterTextScope;
    }
    setFilterPriority(value) {
        this._filterPriority = value?.trim() ?? '';
        this._onDidChangeTreeData.fire();
    }
    setFilterText(value) {
        this._filterText = value?.trim() ?? '';
        this._onDidChangeTreeData.fire();
    }
    setFilterTextScope(value) {
        this._filterTextScope = value;
        this._onDidChangeTreeData.fire();
    }
    async refresh() {
        (0, logger_1.log)('TodoTreeDataProvider.refresh() start');
        this._error = undefined;
        try {
            const { fetchTodoList } = await Promise.resolve().then(() => __importStar(require('./mcpClient')));
            const result = await fetchTodoList({ done: false });
            this._items = result.items ?? [];
            (0, logger_1.log)('TodoTreeDataProvider.refresh() success', { itemCount: this._items.length, totalCount: result.totalCount });
        }
        catch (e) {
            const raw = e instanceof Error ? e.message : String(e);
            const code = e && typeof e === 'object' && 'code' in e ? String(e.code) : '';
            this._error = raw || code || 'Connection failed (is MCP server running at http://localhost:7147?)';
            this._items = [];
            (0, logger_1.log)('TodoTreeDataProvider.refresh() error', { error: this._error, code: code || undefined });
        }
        this._onDidChangeTreeData.fire();
        (0, logger_1.log)('TodoTreeDataProvider.refresh() done');
    }
    getTreeItem(element) {
        if (isPriorityNode(element)) {
            const label = element.priority === 'other' ? 'Other' : element.priority.charAt(0).toUpperCase() + element.priority.slice(1);
            const ti = new vscode.TreeItem(`Priority: ${label}`, vscode.TreeItemCollapsibleState.Expanded);
            ti.id = `priority-${element.priority}`;
            ti.contextValue = 'priorityGroup';
            return ti;
        }
        const item = element;
        const isPlaceholder = !item.id;
        const priorityLabel = item.priority ? item.priority.charAt(0).toUpperCase() + item.priority.slice(1) : '';
        const ti = new vscode.TreeItem(item.id || item.title || '(Empty)', vscode.TreeItemCollapsibleState.None);
        ti.id = item.id || `placeholder-${item.title?.slice(0, 20) ?? 'empty'}`;
        ti.description = item.id ? [item.title ?? '', priorityLabel].filter(Boolean).join(' · ') : undefined;
        ti.tooltip = item.id ? `${item.id} — ${priorityLabel}\n${item.title ?? ''}` : item.title;
        if (!isPlaceholder) {
            ti.contextValue = 'todoItem';
            ti.command = {
                command: 'fwhMcpTodo.openInEditor',
                title: 'Open in editor',
                arguments: [item.id],
            };
        }
        return ti;
    }
    getChildren(element) {
        if (this._error) {
            if (!element) {
                return [{ id: '', title: `Unable to load: ${this._error}`, section: '', priority: '', done: false }];
            }
            return [];
        }
        if (this._items.length === 0 && !element) {
            return [{ id: '', title: 'Click Refresh (⟳) to load items', section: '', priority: '', done: false }];
        }
        const filtered = this._items.filter((i) => matchesText(i, this._filterText, this._filterTextScope) && matchesPriority(i, this._filterPriority));
        if (!element) {
            const filtersActive = Boolean(this._filterPriority || this._filterText);
            if (filtered.length === 0 && filtersActive) {
                const parts = [];
                if (this._filterPriority)
                    parts.push(`Priority: ${this._filterPriority}`);
                if (this._filterText)
                    parts.push(`Text: "${this._filterText}"`);
                return [
                    {
                        id: '',
                        title: `No items match (${parts.join(', ')}). Use "Clear filters" or change filters.`,
                        section: '',
                        priority: '',
                        done: false,
                    },
                ];
            }
            const byPriority = new Map();
            for (const item of filtered) {
                const key = priorityKey(item.priority ?? '');
                if (!byPriority.has(key))
                    byPriority.set(key, []);
                byPriority.get(key).push(item);
            }
            const order = [...PRIORITY_ORDER];
            const otherKeys = [...byPriority.keys()].filter((k) => !order.includes(k));
            otherKeys.sort();
            const sortedKeys = [...order.filter((k) => byPriority.has(k)), ...otherKeys];
            return sortedKeys.map((priority) => ({ kind: 'priority', priority }));
        }
        if (isPriorityNode(element)) {
            const key = priorityKey(element.priority);
            const list = filtered.filter((i) => priorityKey(i.priority ?? '') === key);
            return list;
        }
        return [];
    }
}
exports.TodoTreeDataProvider = TodoTreeDataProvider;
//# sourceMappingURL=todoTree.js.map