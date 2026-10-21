// SPDX-License-Identifier: Apache-2.0
/** TEST-QB-007/008: durable event ordering, context isolation and fail-closed acknowledgement. */
import { AuditOutbox, type AuditContext, type AuditEvent, type AuditReceipt, type AuditTransport } from '../src/cache/audit-outbox.js';
import { promises as fs } from 'fs';
import { tmpdir } from 'os';
import { basename, dirname, join } from 'path';
import { dump, load } from 'js-yaml';

describe('durable audit outbox', () => {
  let root: string;
  let context: AuditContext;
  let sleep: jest.Mock<Promise<void>, [number]>;
  const receipt = (event: AuditEvent, code: AuditReceipt['code'] = 'persisted'): AuditReceipt => ({ ...event, code, revision: 7 });
  function outbox(identity = context) { return new AuditOutbox({ rootDirectory: root, context: identity, sleep }); }
  function transport(): AuditTransport {
    return { submit: jest.fn(async event => receipt(event)), read: jest.fn(async event => receipt(event)) };
  }
  async function eventFiles(directory = root): Promise<string[]> {
    const entries = await fs.readdir(directory, { withFileTypes: true });
    const files = await Promise.all(entries.map(entry => entry.isDirectory() ? eventFiles(join(directory, entry.name)) : [join(directory, entry.name)]));
    return files.flat().filter(file => file.endsWith('.event.yaml'));
  }
  beforeEach(async () => {
    root = await fs.mkdtemp(join(tmpdir(), 'qb-outbox-'));
    context = { workspacePath: root, agentIdentity: 'ClineQb', mcpSessionId: 'ClineQb-20261009T120000Z-test' };
    sleep = jest.fn(async (_milliseconds: number): Promise<void> => undefined);
  });
  afterEach(async () => { jest.restoreAllMocks(); await fs.rm(root, { recursive: true, force: true }); });

  test('persists an immutable event before returning queued and resumes turn ordering after reconstruction', async () => {
    const payload = { description: 'first' };
    const first = await outbox().enqueue('req-first', 'action', payload);
    expect(first.code).toBe('queued');
    expect(first.event?.order).toBe(1);
    expect(await eventFiles()).toHaveLength(1);
    payload.description = 'changed caller object';
    const second = await outbox().enqueue('req-first', 'decision', { conclusion: 'second' });
    const otherTurn = await outbox().enqueue('req-other', 'dialog', { content: 'new turn' });
    expect(second.event?.order).toBe(2);
    expect(otherTurn.event?.order).toBe(1);
    expect(first.event?.payloadJson).toBe(JSON.stringify({ description: 'first' }));
    expect(second.event?.eventId).not.toBe(first.event?.eventId);
  });

  test('concurrent independent outbox objects allocate unique durable order', async () => {
    const a = outbox(); const b = outbox();
    const results = await Promise.all(Array.from({ length: 20 }, (_, i) => (i % 2 ? a : b).enqueue('req-parallel', 'action', { i })));
    expect(results.every(result => result.code === 'queued')).toBe(true);
    expect(results.map(result => result.event!.order).sort((a, b) => a - b)).toEqual(Array.from({ length: 20 }, (_, i) => i + 1));
    expect(await eventFiles()).toHaveLength(20);
  });

  test.each([
    ['pending', 'rollback'], ['pending', 'missing-key'],
    ['archive', 'rollback'], ['archive', 'missing-key'],
    ['quarantine', 'rollback'], ['quarantine', 'missing-key'],
    ['rejected', 'rollback'], ['rejected', 'missing-key'],
  ])('refuses sequence %s evidence with %s instead of reusing an order', async (location, damage) => {
    const box = outbox();
    await box.enqueue('req-sequence', 'action', { name: 'first' });
    if (location !== 'quarantine') await box.enqueue('req-sequence', 'action', { name: 'second' });
    if (location === 'archive') await box.flush(transport());
    if (location === 'quarantine') {
      const wire = transport();
      jest.mocked(wire.submit).mockImplementation(async event => receipt(event, 'rejected'));
      await box.flush(wire);
    }
    if (location === 'rejected') {
      const wire = transport();
      jest.mocked(wire.submit).mockImplementation(async event => ({ ...receipt(event, 'rejected'), reason: 'skipped_terminal', storedStatus: 'completed' }));
      await box.flush(wire);
    }
    const files = (await eventFiles()).sort();
    const originalEvidence = await Promise.all(files.map(file => fs.readFile(file, 'utf8')));
    const sequenceFile = join(dirname(dirname(files[0])), 'sequence.yaml');
    const state = load(await fs.readFile(sequenceFile, 'utf8')) as { version: number; orders: Record<string, number> };
    const [key] = Object.keys(state.orders);
    if (damage === 'rollback') state.orders[key] = location === 'quarantine' ? 0 : 1;
    else delete state.orders[key];
    await fs.writeFile(sequenceFile, dump(state));

    expect(await outbox().enqueue('req-sequence', 'action', { name: 'third' })).toMatchObject({ code: 'lost' });
    expect((await eventFiles()).sort()).toEqual(files);
    expect(await Promise.all(files.map(file => fs.readFile(file, 'utf8')))).toEqual(originalEvidence);
    expect(load(await fs.readFile(sequenceFile, 'utf8'))).toEqual(state);
  });

  test('replay is restricted to its captured context even if the caller changes the config object', async () => {
    const a = outbox();
    const b = outbox({ ...context, mcpSessionId: 'ClineQb-20261009T120000Z-other' });
    await a.enqueue('req-a', 'action', { name: 'a' });
    await b.enqueue('req-b', 'action', { name: 'b' });
    context.mcpSessionId = 'mutated';
    const wire = transport();
    const result = await a.flush(wire);
    expect(result).toMatchObject({ persisted: 1, pending: 0, quarantined: 0 });
    expect(wire.submit).toHaveBeenCalledTimes(1);
    expect(jest.mocked(wire.submit).mock.calls[0][0]).toMatchObject({ requestId: 'req-a', mcpSessionId: 'ClineQb-20261009T120000Z-test' });
  });

  test.each([
    'turn_immutable', 'skipped_terminal', 'host_session_unknown', 'host_session_ambiguous',
    'request_ambiguous', 'request_unknown', 'turn_logging_unsupported', 'mint_requires_host_ingress',
    'nonce_source_missing', 'session_turn_completion_gate', 'request_class_invalid', 'review_invalid',
    'ingress_nonce_invalid', 'mint_collision_exhausted', 'unknown_new_code',
  ])('never retries final or unknown detail code %s, even with HTTP 500 and retryable true', async detailCode => {
    const box = outbox();
    await box.enqueue('req-final', 'action', { name: 'retained' });
    const wire = transport();
    jest.mocked(wire.submit).mockRejectedValue(Object.assign(new Error('remote diagnostic'), { code: detailCode, retryable: true, status: 500 }));
    expect(await box.flush(wire)).toMatchObject({ persisted: 0, pending: 0, quarantined: 1 });
    expect(wire.submit).toHaveBeenCalledTimes(1);
    expect(sleep).not.toHaveBeenCalled();
    const saved = load(await fs.readFile((await eventFiles())[0], 'utf8'));
    expect(saved).toMatchObject({ failure: { detailCode, retryable: false, reportedRetryable: true } });
  });

  test('unknown thrown failures are final and retain durable evidence', async () => {
    const box = outbox();
    await box.enqueue('req-unknown', 'action', {});
    const wire = transport();
    jest.mocked(wire.submit).mockRejectedValue(new Error('unclassified failure'));
    expect(await box.flush(wire)).toMatchObject({ quarantined: 1, persisted: 0 });
    expect(wire.submit).toHaveBeenCalledTimes(1);
    expect(sleep).not.toHaveBeenCalled();
  });

  test.each([false, true])('persistence retry respects explicit retryable=%s', async retryable => {
    const box = outbox();
    await box.enqueue('req-persistence', 'action', {});
    const wire = transport();
    jest.mocked(wire.submit).mockRejectedValueOnce(Object.assign(new Error('storage'), { code: 'persistence_error', retryable }));
    const result = await box.flush(wire);
    expect(result).toMatchObject({ persisted: retryable ? 1 : 0, quarantined: retryable ? 0 : 1 });
    expect(wire.submit).toHaveBeenCalledTimes(retryable ? 2 : 1);
  });

  test('restart never resubmits a final error after interruption before quarantine move', async () => {
    const box = outbox();
    await box.enqueue('req-final-crash', 'action', {});
    const wire = transport();
    jest.mocked(wire.submit).mockRejectedValue(Object.assign(new Error('final'), { code: 'mint_collision_exhausted', retryable: false }));
    const rename = fs.rename.bind(fs);
    const intercepted = jest.spyOn(fs, 'rename').mockImplementation(async (source, target) => {
      if (basename(dirname(String(target))) === 'quarantine') throw new Error('interrupted final move');
      return rename(source, target);
    });
    await expect(box.flush(wire)).rejects.toMatchObject({ code: 'lost' });
    intercepted.mockRestore();
    expect(wire.submit).toHaveBeenCalledTimes(1);
    jest.mocked(wire.submit).mockClear();
    expect(await outbox().flush(wire)).toMatchObject({ quarantined: 1, persisted: 0 });
    expect(wire.submit).not.toHaveBeenCalled();
  });
  test('an ambiguous timeout after server commit replays the same event and requires persisted readback', async () => {
    const box = outbox();
    const queued = await box.enqueue('req-retry', 'action', { name: 'edit' });
    const wire = transport();
    jest.mocked(wire.submit).mockRejectedValueOnce(Object.assign(new Error('timeout after commit'), { code: 'ETIMEDOUT' })).mockImplementation(async event => receipt(event, 'unchanged'));
    const result = await box.flush(wire);
    expect(result).toMatchObject({ persisted: 1, pending: 0, quarantined: 0 });
    expect(jest.mocked(wire.submit).mock.calls.map(([event]) => event.eventId)).toEqual([queued.event!.eventId, queued.event!.eventId]);
    expect(wire.read).toHaveBeenCalledWith(queued.event);
    expect(sleep).toHaveBeenCalledWith(1000);
    expect(await eventFiles()).toHaveLength(1); // Acknowledged evidence remains archived.
    expect(await box.flush(wire)).toMatchObject({ persisted: 0, pending: 0 });
  });

  test.each(['workspacePath', 'agentIdentity', 'mcpSessionId', 'requestId', 'eventId', 'order', 'payloadHash', 'revision'] as const)('quarantines a success acknowledgement with incorrect %s', async key => {
    const box = outbox();
    await box.enqueue('req-bad', 'action', { name: 'edit' });
    const wire = transport();
    jest.mocked(wire.submit).mockImplementation(async event => ({ ...receipt(event), [key]: key === 'order' ? 500 : key === 'revision' ? 0 : 'wrong' }));
    expect(await box.flush(wire)).toMatchObject({ persisted: 0, pending: 0, quarantined: 1 });
    expect(wire.submit).toHaveBeenCalledTimes(1);
    expect(await eventFiles()).toHaveLength(1);
  });

  test('unchanged without matching persisted readback is never accepted', async () => {
    const box = outbox();
    await box.enqueue('req-readback', 'metadata', { status: 'completed' });
    const wire = transport();
    jest.mocked(wire.submit).mockImplementation(async event => receipt(event, 'unchanged'));
    jest.mocked(wire.read).mockImplementation(async event => ({ ...receipt(event), payloadHash: 'wrong' }));
    expect(await box.flush(wire)).toMatchObject({ persisted: 0, quarantined: 1 });
  });

  test.each([false, true])('retains terminal rejection across restart without resubmission (interrupted move: %s)', async interruptMove => {
    const box = outbox();
    const queued = await box.enqueue('req-terminal', 'action', { name: 'required audit' });
    const wire = transport();
    const terminal = { ...receipt(queued.event!, 'rejected'), reason: 'skipped_terminal', storedStatus: 'completed' };
    jest.mocked(wire.submit).mockResolvedValue(terminal);
    if (interruptMove) {
      const rename = fs.rename.bind(fs);
      jest.spyOn(fs, 'rename').mockImplementation(async (source, target) => {
        if (basename(dirname(String(target))) === 'rejected') throw new Error('interrupted terminal move');
        return rename(source, target);
      });
      await expect(box.flush(wire)).rejects.toMatchObject({ code: 'lost' });
      jest.restoreAllMocks();
    } else {
      expect(await box.flush(wire)).toMatchObject({ persisted: 0, pending: 0, quarantined: 0, rejected: 1, receipts: [terminal] });
    }
    expect(await outbox().flush(wire)).toMatchObject({ persisted: 0, pending: 0, quarantined: 0, rejected: 1 });
    expect(wire.submit).toHaveBeenCalledTimes(1);
    expect(sleep).not.toHaveBeenCalled();
    const [file] = await eventFiles();
    expect(basename(dirname(file))).toBe('rejected');
    expect(load(await fs.readFile(file, 'utf8'))).toMatchObject({ event: queued.event, receipt: terminal });
    expect((await outbox().enqueue('req-terminal', 'action', { name: 'next' })).event?.order).toBe(2);
  });

  test('ten failed attempts are durable, quarantined, and stop later events', async () => {
    const box = outbox();
    await box.enqueue('req-cap', 'action', { name: 'first' });
    await box.enqueue('req-cap', 'action', { name: 'second' });
    const wire = transport();
    jest.mocked(wire.submit).mockRejectedValue(Object.assign(new Error('transient offline'), { code: 'ENETUNREACH' }));
    expect(await box.flush(wire)).toMatchObject({ persisted: 0, pending: 1, quarantined: 1 });
    expect(wire.submit).toHaveBeenCalledTimes(10);
    expect(sleep.mock.calls.map(([delay]) => delay)).toEqual([1000, 2000, 4000, 8000, 16000, 30000, 30000, 30000, 30000]);
    expect(await outbox().flush(wire)).toMatchObject({ persisted: 0, pending: 1, quarantined: 1 });
    expect(wire.submit).toHaveBeenCalledTimes(10);
  });

  test('corrupt event evidence is quarantined without submission or silent deletion', async () => {
    const box = outbox();
    await box.enqueue('req-corrupt', 'action', { name: 'first' });
    const [file] = await eventFiles();
    await fs.writeFile(file, 'invalid: [');
    const wire = transport();
    expect(await box.flush(wire)).toMatchObject({ persisted: 0, pending: 0, quarantined: 1 });
    expect(wire.submit).not.toHaveBeenCalled();
    expect(await fs.readFile((await eventFiles())[0], 'utf8')).toBe('invalid: [');
  });

  test('an atomic publish failure returns lost and never reuses its reserved order', async () => {
    const box = outbox();
    const originalRename = fs.rename.bind(fs);
    jest.spyOn(fs, 'rename').mockImplementation(async (source, target) => {
      if (String(target).endsWith('.event.yaml')) throw new Error('synthetic crash before event publish');
      return originalRename(source, target);
    });
    expect(await box.enqueue('req-crash', 'action', { name: 'first' })).toMatchObject({ code: 'lost' });
    jest.restoreAllMocks();
    const retry = await outbox().enqueue('req-crash', 'action', { name: 'second' });
    expect(retry.event?.order).toBe(2);
  });

  test('reconstruction honors the persisted retry deadline after interruption during backoff', async () => {
    jest.spyOn(Date, 'now').mockReturnValue(5000);
    const box = outbox();
    const queued = await box.enqueue('req-deadline', 'action', { name: 'first' });
    const wire = transport();
    jest.mocked(wire.submit).mockRejectedValueOnce(Object.assign(new Error('offline'), { code: 'ENETUNREACH' }));
    sleep.mockRejectedValueOnce(new Error('simulated interruption during backoff'));
    await expect(box.flush(wire)).rejects.toMatchObject({ code: 'lost' });
    sleep.mockClear();
    expect(await outbox().flush(wire)).toMatchObject({ persisted: 1, pending: 0, quarantined: 0 });
    expect(sleep).toHaveBeenCalledWith(1000);
    expect(jest.mocked(wire.submit).mock.calls[1][0].eventId).toBe(queued.event!.eventId);
  });

  test('renamed records cannot reorder a turn during replay', async () => {
    const box = outbox();
    await box.enqueue('req-reorder', 'action', { name: 'first' });
    await box.enqueue('req-reorder', 'action', { name: 'second' });
    const files = (await eventFiles()).sort();
    await fs.rename(files[1], join(dirname(files[1]), '000-first.event.yaml'));
    const wire = transport();
    expect(await box.flush(wire)).toMatchObject({ persisted: 0, pending: 1, quarantined: 1 });
    expect(wire.submit).not.toHaveBeenCalled();
  });
});
