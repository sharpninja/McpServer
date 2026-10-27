// SPDX-License-Identifier: Apache-2.0
import { AuditOutbox, type AuditAliasRecord, type AuditEvent, type AuditReceipt } from '../src/cache/audit-outbox.js';
import { promises as fs } from 'fs';
import { tmpdir } from 'os';
import { join } from 'path';
import { AuditAliasConsumer } from '../src/cache/audit-alias-consumer.js';

describe('TEST-QB-007 C2: alias consumer applies before durable acknowledgement', () => {
  let root: string;
  let aliases: AuditAliasRecord[];
  let wake: (() => void | Promise<void>) | undefined;
  const id = 'qb-audit-outbox-v1';
  function box() { return new AuditOutbox({ rootDirectory: root, context: { workspacePath: root, agentIdentity: 'ClineQb', mcpSessionId: 'session' }, hostSessionKey: 'host' }); }
  function alias(): AuditAliasRecord { return { workspacePath: root, agent: 'ClineQb', mcpSessionId: 'session', hostSessionKey: 'host', ingressNonce: 'one', pendingId: 'pending-one', requestId: 'req-one', aliasedAtUtc: '2026-10-09T23:00:00Z', acks: [] }; }
  function source() {
    const registered = new Set<string>();
    return {
      registerAliasConsumer: jest.fn(async (consumer: string) => { registered.add(consumer); }),
      listUnacknowledgedAliases: jest.fn(async (consumer: string) => {
        if (!registered.has(consumer)) throw new Error('unregistered consumer');
        return aliases.filter(item => !item.acks.includes(consumer));
      }),
      getAlias: jest.fn(async (request: string) => aliases.find(item => item.pendingId === request || item.requestId === request) ?? null),
      acknowledgeAlias: jest.fn(async (consumer: string, pending: string) => {
        const item = aliases.find(item => item.pendingId === pending)!;
        expect(await box().resolveRequestId(pending)).toBe(item.requestId);
        if (!item.acks.includes(consumer)) item.acks.push(consumer);
      }),
      onAliasCommitted: jest.fn((handler: () => void | Promise<void>) => { wake = handler; return () => { wake = undefined; }; }),
    };
  }
  const wire = () => { const submit = jest.fn(async (event: AuditEvent): Promise<AuditReceipt> => ({ ...event, code: 'persisted', revision: 1 })); return { submit, read: submit }; };
  beforeEach(async () => { root = await fs.mkdtemp(join(tmpdir(), 'qb-consumer-')); aliases = []; wake = undefined; });
  afterEach(async () => { await fs.rm(root, { recursive: true, force: true }); });

  test('registers before accepting pending events and uses notification only to drain durable list', async () => {
    const port = source();
    const consumer = await AuditAliasConsumer.open(box(), port, id);
    expect(port.registerAliasConsumer).toHaveBeenCalledWith(id);
    await consumer.enqueue('pending-one', 'action', { content: 'one' });
    aliases.push(alias());
    await wake!();
    expect(aliases[0].acks).toEqual([id]);
    const transport = wire();
    expect(await consumer.flush(transport)).toMatchObject({ persisted: 1, pending: 0 });
    expect(transport.submit.mock.calls[0][0].requestId).toBe('req-one');
    await consumer.close();
  });
  test('startup enumeration recovers a missed notification and duplicate delivery is harmless', async () => {
    await box().enqueue('pending-one', 'action', {}); aliases.push(alias());
    const port = source();
    const consumer = await AuditAliasConsumer.open(box(), port, id);
    await wake!(); await wake!();
    expect(port.acknowledgeAlias).toHaveBeenCalledTimes(1);
    expect(await consumer.flush(wire())).toMatchObject({ persisted: 1, pending: 0 });
    await consumer.close();
  });
  test('lookup reconciles canonical writes before an independent order can be allocated', async () => {
    await box().enqueue('pending-one', 'action', {}); aliases.push(alias());
    const port = source();
    port.listUnacknowledgedAliases.mockResolvedValue([]);
    const consumer = await AuditAliasConsumer.open(box(), port, id);
    expect((await consumer.enqueue('req-one', 'action', {})).event).toMatchObject({ requestId: 'req-one', order: 2 });
    expect(aliases[0].acks).toEqual([id]);
    await consumer.close();
  });
  test('ack failure blocks readiness; restart replays without another event or order reset', async () => {
    await box().enqueue('pending-one', 'action', {});
    const port = source(); const consumer = await AuditAliasConsumer.open(box(), port, id);
    aliases.push(alias());
    port.acknowledgeAlias.mockRejectedValueOnce(new Error('ack offline'));
    await expect(consumer.synchronize()).rejects.toThrow('ack offline');
    expect(consumer.status.ready).toBe(false);
    expect(aliases[0].acks).toEqual([]);
    await consumer.close();
    const recovered = await AuditAliasConsumer.open(box(), port, id);
    expect(recovered.status.ready).toBe(true);
    const transport = wire();
    expect(await recovered.flush(transport)).toMatchObject({ persisted: 1, pending: 0 });
    expect((await recovered.enqueue('req-one', 'action', {})).event?.order).toBe(2);
    await recovered.close();
  });
  test('foreign alias fails without acknowledgement or transport submission', async () => {
    const port = source(); const consumer = await AuditAliasConsumer.open(box(), port, id);
    aliases.push({ ...alias(), hostSessionKey: 'foreign' });
    const transport = wire();
    await expect(consumer.flush(transport)).rejects.toThrow();
    expect(port.acknowledgeAlias).not.toHaveBeenCalled(); expect(transport.submit).not.toHaveBeenCalled();
    expect(consumer.status.ready).toBe(false);
    await consumer.close();
  });
  test('registration failure prevents subscription and consumer creation', async () => {
    const port = source(); port.registerAliasConsumer.mockRejectedValue(new Error('registration offline'));
    await expect(AuditAliasConsumer.open(box(), port, id)).rejects.toThrow('registration offline');
    expect(port.onAliasCommitted).not.toHaveBeenCalled();
  });
  test('wrong lookup identity cannot redirect an event', async () => {
    const port = source(); port.getAlias.mockResolvedValue(alias());
    const consumer = await AuditAliasConsumer.open(box(), port, id);
    await expect(consumer.enqueue('req-unrelated', 'action', {})).rejects.toThrow();
    expect(port.acknowledgeAlias).not.toHaveBeenCalled();
    await consumer.close();
  });
  test('pending reconciliation cannot be observed as ready', async () => {
    const port = source(); const consumer = await AuditAliasConsumer.open(box(), port, id);
    let release!: () => void;
    const hold = new Promise<void>(resolve => { release = resolve; });
    port.listUnacknowledgedAliases.mockImplementationOnce(async () => { await hold; return []; });
    const work = consumer.synchronize();
    expect(consumer.status.ready).toBe(false);
    release(); await work;
    expect(consumer.status.ready).toBe(true);
    await consumer.close();
  });
  test('uncopyable payload refuses execution and records failure readiness', async () => {
    const consumer = await AuditAliasConsumer.open(box(), source(), id);
    await expect(Promise.resolve().then(() => consumer.enqueue('pending-one', 'action', { bad: () => {} }))).rejects.toThrow();
    expect(consumer.status.ready).toBe(false);
    await consumer.close();
  });
  test('close unsubscribes and refuses further writes', async () => {
    const consumer = await AuditAliasConsumer.open(box(), source(), id);
    await consumer.close();
    expect(wake).toBeUndefined(); expect(consumer.status.ready).toBe(false);
    await expect(consumer.enqueue('pending-one', 'action', {})).rejects.toThrow();
  });
  test('parallel wakeups and writes serialize without allocating independent canonical orders', async () => {
    const port = source(); const consumer = await AuditAliasConsumer.open(box(), port, id);
    await consumer.enqueue('pending-one', 'action', {}); aliases.push(alias());
    const results = await Promise.all([consumer.enqueue('req-one', 'action', {}), consumer.enqueue('pending-one', 'action', {}), consumer.synchronize()]);
    expect(results.slice(0, 2).map((result: any) => result.event.order)).toEqual([2, 3]);
    const transport = wire(); expect(await consumer.flush(transport)).toMatchObject({ persisted: 3, pending: 0 });
    expect(transport.submit.mock.calls.map(([event]) => event.order)).toEqual([1, 2, 3]);
    await consumer.close();
  });
});