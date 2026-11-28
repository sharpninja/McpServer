'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { reconcileBindings, buildLegacyCrosswalk, resolvePacketTask } = require('../scripts/handoff.cjs');

const roster = Array.from({ length: 13 }, (_, n) => ({
  legacyTaskNumber: n,
  phaseId: `PHASE-WEBREQS-${n + 1}`,
  todoId: `SLICE-WEBREQS-${String(n + 1).padStart(3, '0')}`,
  sourceAnchor: `legacy.md#task-${n}`,
}));

const savedBindings = roster.map(({ phaseId, todoId }) => ({ sliceKey: phaseId, todoId }));
const ledger = savedBindings.map(row => ({
  ...row, status: 'pending', proposal: 'original proposal', approval: 'approval-1',
  receipt: 'receipt-1', blocker: 'waiting for binding',
}));

test('resumed migration clears all 13 stale pending blockers and preserves audit history', () => {
  const before = JSON.stringify(ledger);
  const output = reconcileBindings({ ledger, savedBindings, verifiedReadbacks: savedBindings });
  assert.deepEqual(output.currentBindingBlockers, []);
  assert.deepEqual(output.supersededPendingEntries, ledger);
  assert.equal(JSON.stringify(ledger), before);
});

test('missing or conflicting exact readback keeps the affected blocker current', () => {
  const verifiedReadbacks = savedBindings.slice(1).map((row, i) => i === 0 ? { ...row, todoId: 'SLICE-OTHER-001' } : row);
  const result = reconcileBindings({ ledger, savedBindings, verifiedReadbacks });
  assert.deepEqual(result.currentBindingBlockers.map(row => row.sliceKey), savedBindings.slice(0, 2).map(row => row.sliceKey));
  assert.equal(result.supersededPendingEntries.length, 11);
});

test('one-based export preserves first and last source rows and resolves historical Task 0', () => {
  const crosswalk = buildLegacyCrosswalk([...roster].reverse(), 13);
  assert.equal(crosswalk.length, 13);
  assert.deepEqual(crosswalk[0], { ...roster[0], packetTaskNumber: 1 });
  assert.deepEqual(crosswalk[12], { ...roster[12], packetTaskNumber: 13 });
  assert.equal(resolvePacketTask(crosswalk, { numbering: 'legacy', taskNumber: 0 }), 1);
  assert.equal(resolvePacketTask(crosswalk, { numbering: 'packet', taskNumber: 13 }), 13);
  assert.equal(roster[0].legacyTaskNumber, 0);
});

test('incomplete or duplicate roster mappings and ambiguous references fail closed', () => {
  assert.throws(() => buildLegacyCrosswalk(roster.slice(1), 13), /Incomplete/);
  assert.throws(() => buildLegacyCrosswalk([roster[0], ...roster.slice(0, 12)], 13), /Duplicate legacy task number/);
  assert.throws(() => buildLegacyCrosswalk([{ ...roster[0], todoId: roster[1].todoId }, ...roster.slice(1)], 13), /Duplicate slice TODO/);
  const crosswalk = buildLegacyCrosswalk(roster, 13);
  assert.throws(() => resolvePacketTask(crosswalk, { taskNumber: 1 }), /numbering basis/);
  assert.throws(() => resolvePacketTask(crosswalk, { numbering: 'legacy', taskNumber: 0, todoId: roster[1].todoId }), /Unresolved/);
});
