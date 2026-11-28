'use strict';

function uniqueIndex(rows, key, label) {
  const index = new Map();
  for (const row of rows) {
    const value = row[key];
    if (index.has(value)) throw new Error(`Duplicate ${label}: ${value}`);
    index.set(value, row);
  }
  return index;
}

function reconcileBindings({ ledger, savedBindings, verifiedReadbacks }) {
  // The caller has checked exact-ID ownership and scope. Lack of a matching
  // readback is unverified, not evidence that the TODO is absent.
  uniqueIndex(ledger, 'sliceKey', 'ledger slice');
  const saved = uniqueIndex(savedBindings, 'sliceKey', 'saved slice');
  const readbacks = uniqueIndex(verifiedReadbacks, 'sliceKey', 'read-back slice');
  const currentBindingBlockers = [];
  const supersededPendingEntries = [];
  for (const entry of ledger) {
    const { sliceKey, todoId } = entry;
    const binding = saved.get(sliceKey);
    const readback = readbacks.get(sliceKey);
    if (!todoId || binding?.todoId !== todoId || readback?.todoId !== todoId) {
      currentBindingBlockers.push({ sliceKey, todoId, reason: 'Saved binding and exact verified readback do not agree with ledger' });
    } else if (entry.status === 'pending') {
      supersededPendingEntries.push(entry);
    }
  }
  return { currentBindingBlockers, supersededPendingEntries };
}

function buildLegacyCrosswalk(rows, verifiedSourceCount) {
  if (!Number.isSafeInteger(verifiedSourceCount) || verifiedSourceCount < 1 || rows.length !== verifiedSourceCount) {
    throw new Error('Incomplete verified legacy roster');
  }
  const byLegacy = uniqueIndex(rows, 'legacyTaskNumber', 'legacy task number');
  uniqueIndex(rows, 'todoId', 'slice TODO');
  const crosswalk = [];
  for (let number = 0; number < verifiedSourceCount; number++) {
    const row = byLegacy.get(number);
    if (!row) throw new Error(`Incomplete zero-based legacy roster: missing ${number}`);
    for (const key of ['phaseId', 'todoId', 'sourceAnchor']) {
      if (typeof row[key] !== 'string' || row[key].trim() === '') throw new Error(`Missing ${key} for legacy task ${number}`);
    }
    crosswalk.push({
      packetTaskNumber: number + 1,
      legacyTaskNumber: number,
      phaseId: row.phaseId,
      todoId: row.todoId,
      sourceAnchor: row.sourceAnchor,
    });
  }
  return crosswalk;
}

function resolvePacketTask(crosswalk, { numbering, taskNumber, todoId }) {
  const key = numbering === 'legacy' ? 'legacyTaskNumber' : numbering === 'packet' ? 'packetTaskNumber' : null;
  if (!key) throw new Error('An explicit task numbering basis is required');
  const matches = crosswalk.filter(row => row[key] === taskNumber && (todoId === undefined || row.todoId === todoId));
  if (matches.length !== 1) throw new Error('Unresolved or ambiguous task reference');
  return matches[0].packetTaskNumber;
}

module.exports = { reconcileBindings, buildLegacyCrosswalk, resolvePacketTask };
