/**
 * Tree data provider for MCP TODO items.
 * Groups by priority; shows priority on each item. Supports filter by priority and filter by text.
 * MCP client is lazy-loaded on first refresh.
 */

import * as vscode from 'vscode';
import type { TodoFlatItem } from './mcpClient';
import { log } from './logger';

const PRIORITY_ORDER = ['high', 'medium', 'low'];

type PriorityNode = { kind: 'priority'; priority: string };
type Node = PriorityNode | TodoFlatItem;

function isPriorityNode(node: Node): node is PriorityNode {
  return typeof node === 'object' && node !== null && 'kind' in node && (node as PriorityNode).kind === 'priority';
}

export type TextFilterScope = 'id' | 'title' | 'all';

function searchableForScope(item: TodoFlatItem, scope: TextFilterScope): string[] {
  const strings = (s: unknown): s is string => typeof s === 'string' && s.length > 0;
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

function matchesText(item: TodoFlatItem, text: string, scope: TextFilterScope): boolean {
  if (!text || !text.trim()) return true;
  const searchable = searchableForScope(item, scope).join(' ').toLowerCase();
  const words = text.trim().toLowerCase().split(/\s+/);
  return words.every((word) => searchable.includes(word));
}

function matchesPriority(item: TodoFlatItem, filterPriority: string): boolean {
  if (!filterPriority || !filterPriority.trim()) return true;
  return item.priority?.toLowerCase() === filterPriority.trim().toLowerCase();
}

function priorityKey(p: string): string {
  const k = (p || 'other').trim().toLowerCase();
  return k || 'other';
}

export class TodoTreeDataProvider implements vscode.TreeDataProvider<Node> {
  private _onDidChangeTreeData = new vscode.EventEmitter<void>();
  readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

  private _items: TodoFlatItem[] = [];
  private _error: string | undefined;
  private _filterPriority: string = '';
  private _filterText: string = '';
  private _filterTextScope: TextFilterScope = 'title';

  get filterPriority(): string {
    return this._filterPriority;
  }
  get filterText(): string {
    return this._filterText;
  }
  get filterTextScope(): TextFilterScope {
    return this._filterTextScope;
  }

  setFilterPriority(value: string): void {
    this._filterPriority = value?.trim() ?? '';
    this._onDidChangeTreeData.fire();
  }

  setFilterText(value: string): void {
    this._filterText = value?.trim() ?? '';
    this._onDidChangeTreeData.fire();
  }

  setFilterTextScope(value: TextFilterScope): void {
    this._filterTextScope = value;
    this._onDidChangeTreeData.fire();
  }

  async refresh(): Promise<void> {
    log('TodoTreeDataProvider.refresh() start');
    this._error = undefined;
    try {
      const { fetchTodoList } = await import('./mcpClient');
      const result = await fetchTodoList({ done: false });
      this._items = result.items ?? [];
      log('TodoTreeDataProvider.refresh() success', { itemCount: this._items.length, totalCount: result.totalCount });
    } catch (e) {
      const raw = e instanceof Error ? e.message : String(e);
      const code = e && typeof e === 'object' && 'code' in e ? String((e as NodeJS.ErrnoException).code) : '';
      this._error = raw || code || 'Connection failed (is MCP server running at http://localhost:7147?)';
      this._items = [];
      log('TodoTreeDataProvider.refresh() error', { error: this._error, code: code || undefined });
    }
    this._onDidChangeTreeData.fire();
    log('TodoTreeDataProvider.refresh() done');
  }

  getTreeItem(element: Node): vscode.TreeItem {
    if (isPriorityNode(element)) {
      const label = element.priority === 'other' ? 'Other' : element.priority.charAt(0).toUpperCase() + element.priority.slice(1);
      const ti = new vscode.TreeItem(`Priority: ${label}`, vscode.TreeItemCollapsibleState.Expanded);
      ti.id = `priority-${element.priority}`;
      ti.contextValue = 'priorityGroup';
      return ti;
    }

    const item = element as TodoFlatItem;
    const isPlaceholder = !item.id;
    const priorityLabel = item.priority ? item.priority.charAt(0).toUpperCase() + item.priority.slice(1) : '';
    const ti = new vscode.TreeItem(
      item.id || item.title || '(Empty)',
      vscode.TreeItemCollapsibleState.None
    );
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

  getChildren(element?: Node): vscode.ProviderResult<Node[]> {
    if (this._error) {
      if (!element) {
        return [{ id: '', title: `Unable to load: ${this._error}`, section: '', priority: '', done: false }];
      }
      return [];
    }
    if (this._items.length === 0 && !element) {
      return [{ id: '', title: 'Click Refresh (⟳) to load items', section: '', priority: '', done: false }];
    }

    const filtered = this._items.filter(
      (i) => matchesText(i, this._filterText, this._filterTextScope) && matchesPriority(i, this._filterPriority)
    );

    if (!element) {
      const filtersActive = Boolean(this._filterPriority || this._filterText);
      if (filtered.length === 0 && filtersActive) {
        const parts: string[] = [];
        if (this._filterPriority) parts.push(`Priority: ${this._filterPriority}`);
        if (this._filterText) parts.push(`Text: "${this._filterText}"`);
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
      const byPriority = new Map<string, TodoFlatItem[]>();
      for (const item of filtered) {
        const key = priorityKey(item.priority ?? '');
        if (!byPriority.has(key)) byPriority.set(key, []);
        byPriority.get(key)!.push(item);
      }
      const order = [...PRIORITY_ORDER];
      const otherKeys = [...byPriority.keys()].filter((k) => !order.includes(k));
      otherKeys.sort();
      const sortedKeys = [...order.filter((k) => byPriority.has(k)), ...otherKeys];
      return sortedKeys.map((priority) => ({ kind: 'priority' as const, priority }));
    }

    if (isPriorityNode(element)) {
      const key = priorityKey(element.priority);
      const list = filtered.filter((i) => priorityKey(i.priority ?? '') === key);
      return list;
    }

    return [];
  }
}
