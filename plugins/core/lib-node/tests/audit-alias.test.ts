import { spawnSync } from 'child_process';
import { resolve } from 'path';
// SPDX-License-Identifier: Apache-2.0
import { AuditOutbox, type AuditEvent, type AuditReceipt } from '../src/cache/audit-outbox.js';
import { promises as fs } from 'fs';
import { tmpdir } from 'os';
import { join, basename, dirname } from 'path';
import { load } from 'js-yaml';

type Alias = { workspacePath: string; agent: string; mcpSessionId: string; hostSessionKey: string; ingressNonce: string; pendingId: string; requestId: string; aliasedAtUtc: string; acks: string[] };
type Box = AuditOutbox & { applyAlias(alias: Alias): Promise<void>; resolveRequestId(id: string): Promise<string> };

describe('TEST-QB-007/008 C2: durable alias application', () => {
  let root: string;
  const pending = 'pending-nonce-one';
  const minted = 'req-minted-one';
  function box(hostSessionKey = 'host-one'): Box {
    const options = { rootDirectory: root, context: { workspacePath: root, agentIdentity: 'ClineQb', mcpSessionId: 'session-one' }, hostSessionKey };
    return new AuditOutbox(options) as Box;
  }
  function alias(change: Partial<Alias> = {}): Alias {
    return { workspacePath: root, agent: 'ClineQb', mcpSessionId: 'session-one', hostSessionKey: 'host-one', ingressNonce: 'nonce-one', pendingId: pending, requestId: minted, aliasedAtUtc: '2026-10-09T23:00:00Z', acks: [], ...change };
  }
  function wire() {
    const submit = jest.fn(async (event: AuditEvent): Promise<AuditReceipt> => ({ ...event, code: 'persisted', revision: 1 }));
    return { submit, read: submit };
  }
  async function events(dir = root): Promise<string[]> {
    return (await Promise.all((await fs.readdir(dir, { withFileTypes: true })).map(entry => entry.isDirectory() ? events(join(dir, entry.name)) : Promise.resolve(entry.name.endsWith('.event.yaml') ? [join(dir, entry.name)] : [])))).flat();
  }
  beforeEach(async () => { root = await fs.mkdtemp(join(tmpdir(), 'qb-alias-')); });
  afterEach(async () => { jest.restoreAllMocks(); await fs.rm(root, { recursive: true, force: true }); });

  test('unminted events remain pending and never reach the server', async () => {
    await box().enqueue(pending, 'action', { description: 'offline' });
    const transport = wire();
    expect(await box().flush(transport)).toMatchObject({ persisted: 0, pending: 1, quarantined: 0 });
    expect(transport.submit).not.toHaveBeenCalled();
  });
  test('both ids retain one sequence, stable files, exact payload bytes and event ids across restart', async () => {
    const first = (await box().enqueue(pending, 'action', { text: '雪', n: 1 })).event!;
    const second = (await box().enqueue(pending, 'dialog', { text: 'two' })).event!;
    const names = (await events()).map(file => basename(file)).sort();
    await box().applyAlias(alias());
    expect((await events()).map(file => basename(file)).sort()).toEqual(names);
    expect(await box().resolveRequestId(pending)).toBe(minted);
    expect(await box().resolveRequestId(minted)).toBe(minted);
    expect((await box().enqueue(minted, 'decision', {})).event?.order).toBe(3);
    expect((await box().enqueue(pending, 'metadata', {})).event).toMatchObject({ requestId: minted, order: 4 });
    const transport = wire();
    expect(await box().flush(transport)).toMatchObject({ persisted: 4, pending: 0, quarantined: 0 });
    expect(transport.submit.mock.calls.map(([event]) => event.order)).toEqual([1, 2, 3, 4]);
    expect(transport.submit.mock.calls[0][0]).toEqual({ ...first, requestId: minted });
    expect(transport.submit.mock.calls[1][0]).toEqual({ ...second, requestId: minted });
  });
  test('duplicate delivery after archive is idempotent despite changed acknowledgements', async () => {
    await box().enqueue(pending, 'action', {});
    await box().applyAlias(alias());
    await box().flush(wire());
    await box().applyAlias(alias({ acks: ['other'] }));
    const transport = wire();
    expect(await box().flush(transport)).toMatchObject({ persisted: 0, pending: 0 });
    expect(transport.submit).not.toHaveBeenCalled();
    expect((await box().enqueue(pending, 'action', {})).event?.order).toBe(2);
  });
  test.each(['workspacePath', 'agent', 'mcpSessionId', 'hostSessionKey', 'ingressNonce', 'pendingId', 'aliasedAtUtc'] as const)('rejects invalid or foreign %s without rewriting evidence', async field => {
    await box().enqueue(pending, 'action', {});
    const [file] = await events(); const before = await fs.readFile(file, 'utf8');
    await expect(box().applyAlias(alias({ [field]: 'foreign-invalid' }))).rejects.toThrow();
    expect(await fs.readFile(file, 'utf8')).toBe(before);
  });
  test('refuses contradictory aliases and joining two pending logical turns', async () => {
    await box().enqueue(pending, 'action', {});
    await box().applyAlias(alias());
    await expect(box().applyAlias(alias({ requestId: 'req-conflict' }))).rejects.toThrow();
    await expect(box().applyAlias(alias({ pendingId: 'pending-other', ingressNonce: 'other' }))).rejects.toThrow();
    expect(await box().resolveRequestId(pending)).toBe(minted);
  });
  test('does not merge independently allocated canonical orders', async () => {
    await box().enqueue(pending, 'action', { pending: true });
    await box().enqueue(minted, 'action', { canonical: true });
    await expect(box().applyAlias(alias())).rejects.toThrow();
  });
  test('same MCP session with distinct host sessions has independent alias and event state', async () => {
    await box('host-one').enqueue(pending, 'action', { host: 1 });
    await box('host-two').enqueue(pending, 'action', { host: 2 });
    await box('host-one').applyAlias(alias());
    const transport = wire();
    expect(await box('host-one').flush(transport)).toMatchObject({ persisted: 1, pending: 0 });
    expect(await box('host-two').flush(transport)).toMatchObject({ persisted: 0, pending: 1 });
    expect(transport.submit).toHaveBeenCalledTimes(1);
  });
  test.each(['journal', 'first-event'])('restart replays committed alias after crash following %s publication', async stage => {
    const first = (await box().enqueue(pending, 'action', { n: 1 })).event!;
    const second = (await box().enqueue(pending, 'action', { n: 2 })).event!;
    const rename = fs.rename.bind(fs); let crashed = false;
    jest.spyOn(fs, 'rename').mockImplementation(async (from, to) => {
      await rename(from, to);
      const hit = stage === 'journal' ? String(to).endsWith('applied-aliases.yaml') : String(to).endsWith('.event.yaml');
      if (!crashed && hit) { crashed = true; throw new Error('simulated crash after durable publication'); }
    });
    await expect(box().applyAlias(alias())).rejects.toThrow();
    jest.restoreAllMocks();
    const transport = wire();
    expect(await box().flush(transport)).toMatchObject({ persisted: 2, pending: 0, quarantined: 0 });
    expect(transport.submit.mock.calls.map(([event]) => event)).toEqual([{ ...first, requestId: minted }, { ...second, requestId: minted }]);
    expect((await box().enqueue(minted, 'action', {})).event?.order).toBe(3);
  });
  test.each(['before-journal', 'after-journal', 'before-event', 'after-event'])('real process exit at %s retains one logical turn and recoverable evidence', async stage => {
    const original = (await box().enqueue(pending, 'action', { crash: stage })).event!;
    const program = `
      const { promises: fs } = require('fs');
      const { AuditOutbox } = require(process.argv[1]);
      const root = process.argv[2], stage = process.argv[3], alias = JSON.parse(process.argv[4]);
      const rename = fs.rename.bind(fs);
      fs.rename = async (from, to) => {
        const hit = stage.endsWith('journal') ? String(to).endsWith('applied-aliases.yaml') : String(to).endsWith('.event.yaml');
        if (hit && stage.startsWith('before')) process.exit(73);
        await rename(from, to);
        if (hit && stage.startsWith('after')) process.exit(73);
      };
      new AuditOutbox({ rootDirectory: root, context: { workspacePath: root, agentIdentity: 'ClineQb', mcpSessionId: 'session-one' }, hostSessionKey: 'host-one' })
        .applyAlias(alias).then(() => process.exit(0), () => process.exit(74));
    `;
    const child = spawnSync(process.execPath, ['-e', program, resolve(__dirname, '../dist/cache/audit-outbox.js'), root, stage, JSON.stringify(alias())], { timeout: 10000, encoding: 'utf8' });
    expect(child.error).toBeUndefined(); expect(child.status).toBe(73); expect(child.stderr).toBe('');
    expect(await box().recoverAbandonedLock()).toBe(true);
    // A8 still owns the unacknowledged alias and redelivers it after restart.
    await box().applyAlias(alias());
    expect(await box().resolveRequestId(pending)).toBe(minted);
    const [eventFile] = await events();
    const state = load(await fs.readFile(eventFile, 'utf8')) as { event: AuditEvent };
    expect(state.event).toEqual({ ...original, requestId: minted });
    const next = (await box().enqueue(minted, 'action', {})).event!;
    expect(next.order).toBe(2); expect(next.eventId).not.toBe(original.eventId);
    const transport = wire(); const result = await box().flush(transport);
    if (stage.startsWith('before')) {
      // Incomplete atomic writes remain quarantined recovery evidence. Do not
      // turn successful alias application into a false audit-clearance claim.
      expect(result).toMatchObject({ persisted: 0, pending: 2, quarantined: 1 });
      expect(transport.submit).not.toHaveBeenCalled();
    } else {
      expect(result).toMatchObject({ persisted: 2, pending: 0, quarantined: 0 });
      expect(transport.submit.mock.calls.map(([event]) => event)).toEqual([{ ...original, requestId: minted }, next]);
    }
  });

  test('concurrent duplicate alias applications and writers keep one sequence', async () => {
    await box().enqueue(pending, 'action', {});
    await Promise.all([box().applyAlias(alias()), box().applyAlias(alias())]);
    const results = await Promise.all(Array.from({ length: 12 }, (_, i) => box().enqueue(i % 2 ? pending : minted, 'action', { i })));
    expect(results.every(result => result.code === 'queued')).toBe(true);
    expect(results.map(result => result.event!.order).sort((a, b) => a - b)).toEqual(Array.from({ length: 12 }, (_, i) => i + 2));
    const transport = wire();
    expect(await box().flush(transport)).toMatchObject({ persisted: 13, pending: 0 });
    expect(transport.submit.mock.calls.map(([event]) => event.order)).toEqual(Array.from({ length: 13 }, (_, i) => i + 1));
  });
  test('alias application refuses pending events already attempted by a legacy writer', async () => {
    await box().enqueue(pending, 'action', {});
    const [file] = await events();
    const text = await fs.readFile(file, 'utf8');
    await fs.writeFile(file, text.replace('attempts: 0', 'attempts: 1\nnextAttemptAt: 1'));
    await expect(box().applyAlias(alias())).rejects.toThrow();
    const state = load(await fs.readFile(file, 'utf8')) as { event: AuditEvent };
    expect(state.event.requestId).toBe(pending);
  });
});