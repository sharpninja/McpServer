// SPDX-License-Identifier: Apache-2.0
import { AuditOutbox, AuditStorageError, type AuditAliasRecord, type AuditEvent, type AuditEnqueueResult, type AuditFlushResult, type AuditTransport } from './audit-outbox.js';
import { classifyOperationFailure, type OperationFailure } from '../runtime/operation-failure.js';

/** C2 interface agreed with A8; every operation is already context-bound. */
export interface AuditAliasSource {
  registerAliasConsumer(consumerId: string): Promise<void>;
  listUnacknowledgedAliases(consumerId: string): Promise<AuditAliasRecord[]>;
  getAlias(id: string): Promise<AuditAliasRecord | null>;
  acknowledgeAlias(consumerId: string, pendingId: string): Promise<void>;
  onAliasCommitted(handler: () => void | Promise<void>): () => void;
}

/** Host-owned adapter: register before ingress, reconcile before writes/flush. */
export class AuditAliasConsumer {
  private queue: Promise<void> = Promise.resolve();
  private closed = false;
  private pendingOperations = 0;
  private failure?: OperationFailure;
  private unsubscribe: () => void = () => {};

  private constructor(private readonly outbox: AuditOutbox, private readonly source: AuditAliasSource, private readonly consumerId: string) {}

  /** A stable consumer ID is persisted by A8 before the host accepts ingress. */
  static async open(outbox: AuditOutbox, source: AuditAliasSource, consumerId: string): Promise<AuditAliasConsumer> {
    if (typeof consumerId !== 'string' || !consumerId.trim()) throw new AuditStorageError('Alias consumer id is required');
    const consumer = new AuditAliasConsumer(outbox, source, consumerId);
    await source.registerAliasConsumer(consumerId);
    try {
      // Subscribe before enumeration so an alias committed during startup is
      // covered by enumeration or a queued wakeup. Neither implies delivery.
      consumer.unsubscribe = source.onAliasCommitted(() => consumer.synchronize());
      if (typeof consumer.unsubscribe !== 'function') throw new AuditStorageError('Alias subscription is invalid');
      await consumer.synchronize();
      return consumer;
    } catch (error) {
      consumer.closed = true;
      if (typeof consumer.unsubscribe === 'function') consumer.unsubscribe();
      throw error;
    }
  }

  /** This readiness concerns reconciliation only; outbox blockers still apply. */
  get status(): Readonly<{ ready: boolean; failure?: OperationFailure }> {
    return Object.freeze({ ready: !this.closed && !this.failure && this.pendingOperations === 0, ...(this.failure ? { failure: this.failure } : {}) });
  }

  private run<T>(operation: () => Promise<T>): Promise<T> {
    this.pendingOperations++;
    const result = this.queue.then(async () => {
      if (this.closed) throw new AuditStorageError('Alias consumer is closed');
      try {
        const value = await operation();
        this.failure = undefined;
        return value;
      } catch (error) {
        this.failure = classifyOperationFailure(error);
        throw error;
      }
    });
    this.queue = result.then(() => undefined, () => undefined);
    return result.finally(() => { this.pendingOperations--; });
  }

  private async applyAndAcknowledge(alias: AuditAliasRecord): Promise<void> {
    // A8 may update its own acknowledgment list during an await. Retain the
    // exact pending ID and binding we applied instead of rereading that object.
    const snapshot: AuditAliasRecord = structuredClone(alias);
    await this.outbox.applyAlias(snapshot);
    await this.source.acknowledgeAlias(this.consumerId, snapshot.pendingId);
  }

  private async drain(): Promise<void> {
    const aliases = await this.source.listUnacknowledgedAliases(this.consumerId);
    if (!Array.isArray(aliases)) throw new AuditStorageError('Alias enumeration is invalid');
    for (const alias of structuredClone(aliases)) await this.applyAndAcknowledge(alias);
  }

  /** Replay durable enumeration; a callback is only a request to call this. */
  synchronize(): Promise<void> { return this.run(() => this.drain()); }

  /** Resolve a fresh mint before allocating its first canonical event order. */
  enqueue(requestId: string, kind: AuditEvent['kind'], payload: Record<string, unknown>): Promise<AuditEnqueueResult> {
    // The caller cannot change payload contents while waiting behind a wakeup.
    let snapshot: Record<string, unknown>;
    try { snapshot = structuredClone(payload); }
    catch (error) {
      this.failure = classifyOperationFailure(error);
      return Promise.reject(error);
    }
    return this.run(async () => {
      await this.drain();
      const alias = await this.source.getAlias(requestId);
      if (alias !== null) {
        if (!alias || (alias.pendingId !== requestId && alias.requestId !== requestId)) throw new AuditStorageError('Alias lookup returned a different request');
        await this.applyAndAcknowledge(alias);
      }
      const result = await this.outbox.enqueue(requestId, kind, snapshot);
      if (result.code === 'lost') throw new AuditStorageError('Audit event could not be durably queued');
      return result;
    });
  }

  /** Outbox pending/rejected/quarantined counts remain authoritative blockers. */
  flush(transport: AuditTransport): Promise<AuditFlushResult> {
    return this.run(async () => { await this.drain(); return this.outbox.flush(transport); });
  }

  /** Stop wakeups, refuse queued/new operations, and await any active operation. */
  async close(): Promise<void> {
    if (!this.closed) { this.closed = true; this.unsubscribe(); }
    await this.queue;
  }
}