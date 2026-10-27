/** TEST-QB-007: actual process contention, durable attempt counts and dead-owner recovery. */
import { AuditOutbox, type AuditEvent, type AuditReceipt } from '../src/cache/audit-outbox.js';
import { execFileSync, spawn } from 'child_process';
import { promises as fs } from 'fs';
import { join, resolve } from 'path';
import { tmpdir } from 'os';
import { load } from 'js-yaml';

describe('audit outbox across processes', () => {
  const packageRoot = resolve(__dirname, '..');
  let root: string;
  function box() {
    return new AuditOutbox({ rootDirectory: root, context: { workspacePath: root, agentIdentity: 'ClineQb', mcpSessionId: 'ClineQb-20261009T120000Z-process' } });
  }
  const recover = (outbox: AuditOutbox) => (outbox as AuditOutbox & { recoverAbandonedLock(): Promise<boolean> }).recoverAbandonedLock();
  const receipt = (event: AuditEvent): AuditReceipt => ({ ...event, code: 'persisted', revision: 1 });
  function worker(mode: string, count: number): Promise<{ code: number | null; events: AuditEvent[] }> {
    return new Promise((resolveWorker, reject) => {
      const child = spawn(process.execPath, [resolve(packageRoot, '../test-fixtures/node/audit-outbox-worker.cjs'),
        join(packageRoot, 'dist/cache/audit-outbox.js'), root, mode, String(count)], { stdio: ['ignore', 'pipe', 'pipe'] });
      let output = ''; let errors = '';
      child.stdout.on('data', data => { output += data; });
      child.stderr.on('data', data => { errors += data; });
      child.on('error', reject);
      child.on('close', code => {
        if (errors) return reject(new Error(errors));
        try { resolveWorker({ code, events: output.trim() ? output.trim().split('\n').map(line => JSON.parse(line)) : [] }); }
        catch (error) { reject(error); }
      });
    });
  }
  beforeAll(() => {
    // The child must execute this source revision, never stale generated output.
    execFileSync(process.execPath, [join(packageRoot, 'node_modules/typescript/bin/tsc'), '-p', 'tsconfig.json'], { cwd: packageRoot, timeout: 30000 });
  }, 35000);
  beforeEach(async () => { root = await fs.mkdtemp(join(tmpdir(), 'qb-process-')); });
  afterEach(async () => { if (root) await fs.rm(root, { recursive: true, force: true }); });

  test('two OS processes allocate forty unique consecutive event orders', async () => {
    const results = await Promise.all([worker('enqueue', 20), worker('enqueue', 20)]);
    expect(results.map(result => result.code)).toEqual([0, 0]);
    expect(results.flatMap(result => result.events.map(event => event.order)).sort((a, b) => a - b)).toEqual(Array.from({ length: 40 }, (_, i) => i + 1));
    const wire = { submit: jest.fn(async event => receipt(event)), read: jest.fn(async event => receipt(event)) };
    expect(await box().flush(wire)).toMatchObject({ persisted: 40, pending: 0, quarantined: 0 });
    expect(wire.submit.mock.calls.map(([event]) => event.order)).toEqual(Array.from({ length: 40 }, (_, i) => i + 1));
  }, 20000);

  test('a publish crash preserves its reserved order and quarantines incomplete evidence after dead-lock recovery', async () => {
    expect((await worker('publish-crash', 1)).code).toBe(73);
    const outbox = box();
    expect(await recover(outbox)).toBe(true);
    expect((await outbox.enqueue('req-process', 'action', { next: true })).event?.order).toBe(2);
    const submit = jest.fn(async event => receipt(event));
    expect(await outbox.flush({ submit, read: submit })).toMatchObject({ persisted: 0, pending: 1, quarantined: 1 });
    expect(submit).not.toHaveBeenCalled();
  });

  test('a crash during submission consumes one durable attempt and resumes the same event', async () => {
    const crashed = await worker('submit-crash', 1);
    expect(crashed.code).toBe(74);
    const outbox = box();
    expect(await recover(outbox)).toBe(true);
    const submit = jest.fn(async event => receipt(event));
    expect(await outbox.flush({ submit, read: submit })).toMatchObject({ persisted: 1, pending: 0, quarantined: 0 });
    expect(submit.mock.calls[0][0].eventId).toBe(crashed.events[0].eventId);
    const [identityDirectory] = await fs.readdir(root);
    const archive = join(root, identityDirectory, 'archive');
    const [eventFile] = (await fs.readdir(archive)).filter(name => name.endsWith('.event.yaml'));
    expect(load(await fs.readFile(join(archive, eventFile), 'utf8'))).toMatchObject({ attempts: 2 });
  });

  test('recovery refuses to break a lock whose owner is still alive', async () => {
    const outbox = box();
    await outbox.enqueue('req-live', 'action', { live: true });
    let release!: () => void;
    let entered!: () => void;
    const active = new Promise<void>(resolveActive => { entered = resolveActive; });
    const hold = new Promise<void>(resolveHold => { release = resolveHold; });
    const submit = async (event: AuditEvent) => { entered(); await hold; return receipt(event); };
    const flushing = outbox.flush({ submit, read: submit });
    await active;
    try { expect(await recover(box())).toBe(false); }
    finally { release(); await flushing; }
  });
});
