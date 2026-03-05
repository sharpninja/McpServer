"use strict";
/**
 * Integration tests for the VSCode extension's TODO lifecycle:
 * create → serialize to markdown → parse back → update → verify.
 *
 * These tests run against the live MCP server (http://localhost:7147).
 * They exercise the same code paths used by the extension:
 *   - todoMarkdown.ts: todoToMarkdown / markdownToUpdateBody
 *   - HTTP calls to GET/POST/PUT/DELETE /mcpserver/todo
 *
 * Run: cd extensions/fwh-mcp-todo && npm run compile && npm test
 * Requires: MCP server running on http://localhost:7147
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
const node_test_1 = require("node:test");
const assert = __importStar(require("node:assert/strict"));
const http = __importStar(require("http"));
// Import the compiled markdown helpers (no vscode dependency)
const todoMarkdown_1 = require("../todoMarkdown");
const BASE_URL = process.env.MCP_BASE_URL ?? 'http://localhost:7147';
// --- HTTP helper (mirrors mcpClient.ts but without vscode deps) ---
function httpRequest(method, path, body) {
    const url = new URL(path, BASE_URL);
    const payload = body ? JSON.stringify(body) : undefined;
    return new Promise((resolve, reject) => {
        const req = http.request({
            host: url.hostname,
            port: url.port || 80,
            path: url.pathname + url.search,
            method,
            headers: {
                Accept: 'application/json',
                ...(payload
                    ? {
                        'Content-Type': 'application/json',
                        'Content-Length': Buffer.byteLength(payload, 'utf8'),
                    }
                    : {}),
            },
        }, (res) => {
            const chunks = [];
            res.on('data', (chunk) => chunks.push(chunk));
            res.on('end', () => {
                resolve({
                    status: res.statusCode ?? 0,
                    body: Buffer.concat(chunks).toString('utf8'),
                });
            });
        });
        req.on('error', reject);
        if (payload)
            req.write(payload, 'utf8');
        req.end();
    });
}
async function createTodo(item) {
    const res = await httpRequest('POST', '/mcpserver/todo', item);
    assert.equal(res.status, 201, `Create ${item.id} failed: ${res.body}`);
}
async function getTodo(id) {
    const res = await httpRequest('GET', `/mcpserver/todo/${encodeURIComponent(id)}`);
    assert.equal(res.status, 200, `GET ${id} failed: ${res.body}`);
    const raw = JSON.parse(res.body);
    // Normalize camelCase (API returns PascalCase)
    return {
        id: (raw.id ?? raw.Id ?? id),
        title: (raw.title ?? raw.Title ?? ''),
        section: (raw.section ?? raw.Section ?? ''),
        priority: (raw.priority ?? raw.Priority ?? ''),
        done: (raw.done ?? raw.Done ?? false),
        estimate: (raw.estimate ?? raw.Estimate),
        note: (raw.note ?? raw.Note),
        description: (raw.description ?? raw.Description),
        technicalDetails: (raw.technicalDetails ?? raw.TechnicalDetails),
        implementationTasks: (raw.implementationTasks ?? raw.ImplementationTasks)?.map((t) => ({
            task: t.task ?? t.Task ?? '',
            done: t.done ?? t.Done ?? false,
        })),
        completedDate: (raw.completedDate ?? raw.CompletedDate),
        doneSummary: (raw.doneSummary ?? raw.DoneSummary),
        remaining: (raw.remaining ?? raw.Remaining),
        dependsOn: (raw.dependsOn ?? raw.DependsOn),
        functionalRequirements: (raw.functionalRequirements ?? raw.FunctionalRequirements),
        technicalRequirements: (raw.technicalRequirements ?? raw.TechnicalRequirements),
    };
}
async function updateTodo(id, body) {
    const payload = {
        Title: body.title,
        Priority: body.priority,
        Section: body.section,
        Done: body.done,
        Estimate: body.estimate,
        Description: body.description,
        TechnicalDetails: body.technicalDetails,
        ImplementationTasks: body.implementationTasks?.map((t) => ({
            Task: t.task,
            Done: t.done,
        })),
        Note: body.note,
        CompletedDate: body.completedDate,
        DoneSummary: body.doneSummary,
        Remaining: body.remaining,
        DependsOn: body.dependsOn,
        FunctionalRequirements: body.functionalRequirements,
        TechnicalRequirements: body.technicalRequirements,
    };
    const res = await httpRequest('PUT', `/mcpserver/todo/${encodeURIComponent(id)}`, payload);
    if (res.status >= 200 && res.status < 300) {
        return { success: true };
    }
    const errData = res.body ? JSON.parse(res.body) : {};
    return {
        success: false,
        error: errData.Error ??
            errData.error ??
            String(res.status),
    };
}
async function deleteTodo(id) {
    await httpRequest('DELETE', `/mcpserver/todo/${encodeURIComponent(id)}`);
}
async function listTodos(params) {
    const qs = params ? '?' + new URLSearchParams(params).toString() : '';
    const res = await httpRequest('GET', `/mcpserver/todo${qs}`);
    assert.equal(res.status, 200, `List failed: ${res.body}`);
    const data = JSON.parse(res.body);
    const items = (data.items ?? data.Items ?? []);
    const totalCount = typeof data.totalCount === 'number' ? data.totalCount : (data.TotalCount ?? items.length);
    return { items, totalCount };
}
// --- Tests ---
(0, node_test_1.describe)('Todo Lifecycle Integration Tests (VSCode extension paths)', () => {
    const createdIds = [];
    // Check MCP server is reachable
    (0, node_test_1.before)(async () => {
        try {
            const res = await httpRequest('GET', '/health');
            assert.ok(res.status >= 200 && res.status < 300, `MCP server not healthy: ${res.status} ${res.body}`);
        }
        catch (e) {
            const msg = e instanceof Error ? e.message : String(e);
            assert.fail(`MCP server not reachable at ${BASE_URL}. Start it first.\n${msg}`);
        }
    });
    // Clean up all created items
    (0, node_test_1.after)(async () => {
        for (const id of createdIds) {
            try {
                await deleteTodo(id);
            }
            catch {
                /* best effort */
            }
        }
    });
    (0, node_test_1.describe)('Markdown serialization (pure logic, no MCP)', () => {
        (0, node_test_1.it)('blankTodoTemplate produces valid YAML front matter + markdown body', () => {
            const md = (0, todoMarkdown_1.blankTodoTemplate)();
            assert.ok(md.startsWith('---'), 'Should start with ---');
            assert.ok(md.includes('id: NEW-TODO'), 'Should contain id');
            assert.ok(md.includes('priority: low'), 'Should contain priority');
            assert.ok(md.includes('# '), 'Should contain H1');
            assert.ok(md.includes('## Technical Details'), 'Should contain H2 Technical Details');
            assert.ok(md.includes('## Implementation Tasks'), 'Should contain H2 Implementation Tasks');
        });
        (0, node_test_1.it)('todoToMarkdown serializes all fields correctly', () => {
            const item = {
                id: 'TEST-MD-001',
                title: 'Markdown round-trip test',
                section: 'mvp-app',
                priority: 'high',
                done: false,
                estimate: '4-8 hours',
                description: ['Line one', 'Line two'],
                technicalDetails: ['Use xUnit', 'Follow patterns'],
                implementationTasks: [
                    { task: 'Step 1', done: true },
                    { task: 'Step 2', done: false },
                ],
                functionalRequirements: ['FR-LOC-001', 'FR-WF-005'],
                technicalRequirements: ['TR-API-001'],
                dependsOn: ['OTHER-001'],
            };
            const md = (0, todoMarkdown_1.todoToMarkdown)(item);
            // Front matter assertions
            assert.ok(md.includes('id: TEST-MD-001'));
            assert.ok(md.includes('section: mvp-app'));
            assert.ok(md.includes('priority: high'));
            assert.ok(md.includes('estimate: 4-8 hours'));
            assert.ok(md.includes('functional-requirements:'));
            assert.ok(md.includes('  - FR-LOC-001'));
            assert.ok(md.includes('  - FR-WF-005'));
            assert.ok(md.includes('technical-requirements:'));
            assert.ok(md.includes('  - TR-API-001'));
            assert.ok(md.includes('depends-on:'));
            assert.ok(md.includes('  - OTHER-001'));
            // Body assertions
            assert.ok(md.includes('# Markdown round-trip test'));
            assert.ok(md.includes('Line one'));
            assert.ok(md.includes('Line two'));
            assert.ok(md.includes('## Technical Details'));
            assert.ok(md.includes('- Use xUnit'));
            assert.ok(md.includes('## Implementation Tasks'));
            assert.ok(md.includes('- [x] Step 1'));
            assert.ok(md.includes('- [ ] Step 2'));
        });
        (0, node_test_1.it)('markdownToUpdateBody round-trips correctly', () => {
            const item = {
                id: 'RT-001',
                title: 'Round trip title',
                section: 'mvp-app',
                priority: 'medium',
                done: false,
                estimate: '2-4 hours',
                description: ['Some description'],
                technicalDetails: ['Detail A', 'Detail B'],
                implementationTasks: [
                    { task: 'Task alpha', done: false },
                    { task: 'Task beta', done: true },
                ],
                functionalRequirements: ['FR-LOC-001'],
                technicalRequirements: ['TR-API-001', 'TR-API-002'],
                dependsOn: ['DEP-001'],
            };
            const md = (0, todoMarkdown_1.todoToMarkdown)(item);
            const parsed = (0, todoMarkdown_1.markdownToUpdateBody)(md);
            assert.equal(parsed.title, 'Round trip title');
            assert.equal(parsed.priority, 'medium');
            assert.equal(parsed.section, 'mvp-app');
            assert.equal(parsed.estimate, '2-4 hours');
            assert.deepEqual(parsed.description, ['Some description']);
            assert.deepEqual(parsed.technicalDetails, ['Detail A', 'Detail B']);
            assert.ok(parsed.implementationTasks);
            assert.equal(parsed.implementationTasks.length, 2);
            assert.equal(parsed.implementationTasks[0].task, 'Task alpha');
            assert.equal(parsed.implementationTasks[0].done, false);
            assert.equal(parsed.implementationTasks[1].task, 'Task beta');
            assert.equal(parsed.implementationTasks[1].done, true);
            assert.deepEqual(parsed.functionalRequirements, ['FR-LOC-001']);
            assert.deepEqual(parsed.technicalRequirements, ['TR-API-001', 'TR-API-002']);
            assert.deepEqual(parsed.dependsOn, ['DEP-001']);
        });
        (0, node_test_1.it)('markdownToUpdateBody handles edited content (simulating user edits)', () => {
            const item = {
                id: 'EDIT-001',
                title: 'Original title',
                section: 'mvp-app',
                priority: 'high',
                done: false,
                description: ['Original description'],
                implementationTasks: [
                    { task: 'Design', done: false },
                    { task: 'Build', done: false },
                ],
            };
            let md = (0, todoMarkdown_1.todoToMarkdown)(item);
            // Simulate user edits including priority and section changes
            md = md.replace('# Original title', '# Edited title');
            md = md.replace('- [ ] Design', '- [x] Design');
            md = md.replace('Original description', 'Edited description');
            md = md.replace('priority: high', 'priority: low');
            md = md.replace('section: mvp-app', 'section: mvp-support');
            const parsed = (0, todoMarkdown_1.markdownToUpdateBody)(md);
            assert.equal(parsed.title, 'Edited title');
            assert.equal(parsed.priority, 'low');
            assert.equal(parsed.section, 'mvp-support');
            assert.deepEqual(parsed.description, ['Edited description']);
            assert.ok(parsed.implementationTasks);
            assert.equal(parsed.implementationTasks[0].done, true, 'Design should be marked done');
            assert.equal(parsed.implementationTasks[1].done, false, 'Build should still be undone');
        });
        (0, node_test_1.it)('markdownToNewTodoFields extracts title and priority', () => {
            const md = (0, todoMarkdown_1.blankTodoTemplate)()
                .replace('# ', '# My New Todo')
                .replace('priority: low', 'priority: high');
            const fields = (0, todoMarkdown_1.markdownToNewTodoFields)(md);
            assert.equal(fields.title, 'My New Todo');
            assert.equal(fields.priority, 'high');
        });
    });
    (0, node_test_1.describe)('Full lifecycle against MCP server', () => {
        (0, node_test_1.it)('create → serialize → parse → update → verify', async () => {
            const id = `VSCODE-LC-${Date.now().toString(36).slice(-6)}`;
            // 1. Create via API
            await createTodo({
                id,
                title: 'VSCode lifecycle test',
                section: 'mvp-app',
                priority: 'high',
                estimate: '4-8 hours',
                description: ['First description line', 'Second line'],
                technicalDetails: ['Use xUnit', 'Follow patterns'],
                implementationTasks: [
                    { task: 'Design', done: false },
                    { task: 'Implement', done: false },
                ],
                functionalRequirements: ['FR-LOC-001'],
                technicalRequirements: ['TR-API-001', 'TR-API-002'],
            });
            createdIds.push(id);
            // 2. GET (simulates opening in editor)
            const item = await getTodo(id);
            assert.equal(item.id, id);
            assert.equal(item.title, 'VSCode lifecycle test');
            // 3. Serialize to markdown (simulates todoToMarkdown in writeFile)
            const md = (0, todoMarkdown_1.todoToMarkdown)(item);
            assert.ok(md.includes(`id: ${id}`));
            assert.ok(md.includes('# VSCode lifecycle test'));
            // 4. Simulate user edit
            const editedMd = md
                .replace('# VSCode lifecycle test', '# Updated VSCode lifecycle')
                .replace('- [ ] Design', '- [x] Design')
                .replace('First description line', 'Modified description');
            // 5. Parse back (simulates markdownToUpdateBody in writeFile)
            const updateBody = (0, todoMarkdown_1.markdownToUpdateBody)(editedMd);
            assert.equal(updateBody.title, 'Updated VSCode lifecycle');
            // 6. Push update (simulates PUT in writeFile)
            const result = await updateTodo(id, updateBody);
            assert.ok(result.success, `Update failed: ${result.error}`);
            // 7. Verify (simulates list refresh after fwhMcpTodo.refresh command)
            const updated = await getTodo(id);
            assert.equal(updated.title, 'Updated VSCode lifecycle');
            assert.ok(updated.description);
            assert.ok(updated.description.some((d) => d.includes('Modified description')), 'Description should contain modified text');
        });
        (0, node_test_1.it)('list refresh reflects changes (simulates tree view refresh)', async () => {
            const id = `VSCODE-LIST-${Date.now().toString(36).slice(-6)}`;
            await createTodo({
                id,
                title: 'List refresh test',
                section: 'mvp-app',
                priority: 'medium',
            });
            createdIds.push(id);
            // Verify appears in list (simulates fetchTodoList after refresh command)
            const list1 = await listTodos({ id });
            assert.equal(list1.items.length, 1);
            // Update
            await updateTodo(id, { title: 'Updated list test', done: true });
            // Verify list reflects update (simulates second refresh)
            const list2 = await listTodos({ id });
            assert.equal(list2.items.length, 1);
            const found = list2.items[0];
            assert.ok(found.title === 'Updated list test' ||
                found.Title === 'Updated list test', 'Title should be updated in list');
        });
        (0, node_test_1.it)('two-save lifecycle: create → save → modify → save → verify', async () => {
            const id = `VSCODE-2S-${Date.now().toString(36).slice(-6)}`;
            await createTodo({
                id,
                title: 'Two-save test',
                section: 'mvp-app',
                priority: 'high',
                description: ['Original'],
                implementationTasks: [
                    { task: 'Step 1', done: false },
                    { task: 'Step 2', done: false },
                    { task: 'Step 3', done: false },
                ],
            });
            createdIds.push(id);
            // First save cycle
            const item1 = await getTodo(id);
            const md1 = (0, todoMarkdown_1.todoToMarkdown)(item1);
            const edited1 = md1
                .replace('- [ ] Step 1', '- [x] Step 1')
                .replace('Original', 'After first save');
            const body1 = (0, todoMarkdown_1.markdownToUpdateBody)(edited1);
            const res1 = await updateTodo(id, body1);
            assert.ok(res1.success);
            // Verify first save
            const after1 = await getTodo(id);
            assert.ok(after1.description);
            assert.ok(after1.description.some((d) => d.includes('After first save')));
            // Second save cycle
            const md2 = (0, todoMarkdown_1.todoToMarkdown)(after1);
            const edited2 = md2
                .replace('- [ ] Step 2', '- [x] Step 2')
                .replace('After first save', 'After second save');
            const body2 = (0, todoMarkdown_1.markdownToUpdateBody)(edited2);
            const res2 = await updateTodo(id, body2);
            assert.ok(res2.success);
            // Verify second save
            const after2 = await getTodo(id);
            assert.ok(after2.description);
            assert.ok(after2.description.some((d) => d.includes('After second save')));
        });
        (0, node_test_1.it)('FR/TR fields survive markdown round-trip through MCP', async () => {
            const id = `VSCODE-FRTR-${Date.now().toString(36).slice(-6)}`;
            await createTodo({
                id,
                title: 'FR/TR round-trip via MCP',
                section: 'mvp-app',
                priority: 'low',
                functionalRequirements: ['FR-WF-005', 'FR-LOC-001'],
                technicalRequirements: ['TR-MOBILE-001'],
            });
            createdIds.push(id);
            // Fetch, serialize, parse back
            const item = await getTodo(id);
            const md = (0, todoMarkdown_1.todoToMarkdown)(item);
            const parsed = (0, todoMarkdown_1.markdownToUpdateBody)(md);
            assert.deepEqual(parsed.functionalRequirements, ['FR-WF-005', 'FR-LOC-001']);
            assert.deepEqual(parsed.technicalRequirements, ['TR-MOBILE-001']);
            // Update through round-trip and verify persistence
            const res = await updateTodo(id, parsed);
            assert.ok(res.success);
            const verified = await getTodo(id);
            assert.ok(verified.functionalRequirements);
            assert.ok(verified.functionalRequirements.includes('FR-WF-005'));
            assert.ok(verified.functionalRequirements.includes('FR-LOC-001'));
            assert.ok(verified.technicalRequirements);
            assert.ok(verified.technicalRequirements.includes('TR-MOBILE-001'));
        });
        (0, node_test_1.it)('priority and section changes survive markdown round-trip through MCP', async () => {
            const id = `VSCODE-PRIO-${Date.now().toString(36).slice(-6)}`;
            await createTodo({
                id,
                title: 'Priority/section round-trip via MCP',
                section: 'mvp-app',
                priority: 'high',
                description: ['Test priority persistence'],
            });
            createdIds.push(id);
            // Fetch, serialize, edit priority/section, parse back, update
            const item = await getTodo(id);
            assert.equal(item.priority, 'high');
            assert.equal(item.section, 'mvp-app');
            let md = (0, todoMarkdown_1.todoToMarkdown)(item);
            assert.ok(md.includes('priority: high'));
            assert.ok(md.includes('section: mvp-app'));
            // Simulate user changing priority and section
            md = md.replace('priority: high', 'priority: low');
            md = md.replace('section: mvp-app', 'section: mvp-support');
            const parsed = (0, todoMarkdown_1.markdownToUpdateBody)(md);
            assert.equal(parsed.priority, 'low');
            assert.equal(parsed.section, 'mvp-support');
            const res = await updateTodo(id, parsed);
            assert.ok(res.success, `Update failed: ${res.error}`);
            // Verify persistence
            const verified = await getTodo(id);
            assert.equal(verified.priority, 'low', 'Priority should be updated to low');
            assert.equal(verified.section, 'mvp-support', 'Section should be updated to mvp-support');
        });
        (0, node_test_1.it)('delete removes item from list', async () => {
            const id = `VSCODE-DEL-${Date.now().toString(36).slice(-6)}`;
            await createTodo({
                id,
                title: 'Delete test',
                section: 'mvp-app',
                priority: 'low',
            });
            // Verify exists
            const res1 = await httpRequest('GET', `/mcpserver/todo/${encodeURIComponent(id)}`);
            assert.equal(res1.status, 200);
            // Delete
            await deleteTodo(id);
            // Verify gone
            const res2 = await httpRequest('GET', `/mcpserver/todo/${encodeURIComponent(id)}`);
            assert.equal(res2.status, 404);
            // Don't add to createdIds since we already deleted
        });
    });
});
//# sourceMappingURL=todoLifecycle.integration.test.js.map