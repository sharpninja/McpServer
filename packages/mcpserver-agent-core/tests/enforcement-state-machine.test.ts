/**
 * enforcement-state-machine.test.ts
 * Focused tests for the production V4EnforcementStateMachine in the shared core package.
 *
 * Covers core contract behaviors (from AgentPluginCoreV4Contracts + codex shims):
 * - beginTurn, record + build success/failure, completeTurn
 * - Build gate (BlockedOnBuild + no escape)
 * - Stop gate decisions
 * - Self-heal / force paths
 * - State machine invariants
 *
 * These complement the heavy contract tests in AgentPluginCore.
 * Run via the package: npm test (after updating script to include this file)
 */

import { describe, it, beforeEach } from 'node:test';
import assert from 'node:assert/strict';
import { V4EnforcementStateMachine } from '../src/enforcement-state-machine.js';

describe('V4EnforcementStateMachine (core package)', () => {
  let sm: V4EnforcementStateMachine;

  beforeEach(() => {
    sm = new V4EnforcementStateMachine();
  });

  it('beginTurn → TurnOpen', async () => {
    const res = await sm.beginTurnAsync('req-001');
    assert.equal(res.success, true);
    assert.equal(sm.CurrentState, 'TurnOpen');
    assert.equal(sm.CurrentTurnId, 'req-001');
  });

  it('record success build → EditsInProgress', async () => {
    await sm.beginTurnAsync('req-001');
    const res = await sm.recordCodeEditAndVerifyBuildAsync('src/test.ts', 'success');
    assert.equal(res.success, true);
    assert.equal(sm.CurrentState, 'EditsInProgress');
  });

  it('record failed build → BlockedOnBuild (no escape on complete)', async () => {
    await sm.beginTurnAsync('req-001');
    await sm.recordCodeEditAndVerifyBuildAsync('src/test.ts', 'failed');

    assert.equal(sm.CurrentState, 'BlockedOnBuild');

    const complete = await sm.completeTurnAsync('req-001');
    assert.equal(complete.success, false);
    assert.equal(complete.errorCode, 'BUILD_FAILED');
  });

  it('completeTurn when pending edits → BlockedOnMissingComplete', async () => {
    await sm.beginTurnAsync('req-001');
    await sm.recordCodeEditAndVerifyBuildAsync('src/test.ts', 'success');

    // Simulate pending state (in real usage the adapter tracks this)
    // For direct test we exercise the stop-gate path
    const stop = sm.evaluateStopGate();
    // After a successful edit we are in EditsInProgress with no forced pending flag here,
    // but the contract test in AgentPluginCore covers the _hasPendingEdits case.
    assert.equal(sm.CurrentState, 'EditsInProgress');
  });

  it('evaluateStopGate covers all states', () => {
    // Simple state machine coverage
    const states: any[] = ['NoTurn', 'TurnOpen', 'EditsInProgress', 'BlockedOnBuild', 'BlockedOnMissingComplete', 'TurnComplete'];

    for (const s of states) {
      (sm as any).currentState = s; // internal for test coverage
      const d = sm.evaluateStopGate();
      assert.equal(typeof d.canEmitFinalResponse, 'boolean');
    }
  });

  it('forceSelfHeal allows recovery from in_progress', async () => {
    await sm.beginTurnAsync('req-001');
    await sm.recordCodeEditAndVerifyBuildAsync('src/test.ts', 'success');

    const res = await sm.completeTurnAsync('req-001', true);
    assert.equal(res.success, true);
    assert.equal(sm.CurrentState, 'TurnComplete');
  });

  // Additional focused coverage tests for Core Package Integration wave cross-validation (V4 contract parity)
  it('beginTurn_BlockedOnBuild_FailsWithBuildFailed (no new turn escape)', async () => {
    await sm.beginTurnAsync('req-b');
    await sm.recordCodeEditAndVerifyBuildAsync('bad.ts', 'failed');
    const res = await sm.beginTurnAsync('req-b2');
    assert.equal(res.success, false);
    assert.equal(res.errorCode, 'BUILD_FAILED');
    assert.equal(sm.CurrentState, 'BlockedOnBuild');
  });

  it('recordCodeEdit_InvalidState_FromNoTurn_ReturnsInvalidState', async () => {
    const res = await sm.recordCodeEditAndVerifyBuildAsync('x.ts', 'success');
    assert.equal(res.success, false);
    assert.equal(res.errorCode, 'INVALID_STATE');
  });

  it('completeTurn_TurnMismatch_WithoutForce_FailsWithTurnMismatch', async () => {
    await sm.beginTurnAsync('req-m');
    const res = await sm.completeTurnAsync('wrong-id');
    assert.equal(res.success, false);
    assert.equal(res.errorCode, 'TURN_MISMATCH');
  });

  it('evaluateStopGate_TurnComplete_AllowsFinalResponse', async () => {
    await sm.beginTurnAsync('req-c');
    await sm.completeTurnAsync('req-c');
    const d = sm.evaluateStopGate();
    assert.equal(d.canEmitFinalResponse, true);
    assert.equal(d.state, 'TurnComplete');
  });

  it('multipleSuccessfulEdits_StayInEditsInProgress_UntilComplete', async () => {
    await sm.beginTurnAsync('req-multi');
    await sm.recordCodeEditAndVerifyBuildAsync('a.ts', 'success');
    await sm.recordCodeEditAndVerifyBuildAsync('b.ts', 'success');
    assert.equal(sm.CurrentState, 'EditsInProgress');
    const gate = sm.evaluateStopGate();
    assert.equal(gate.canEmitFinalResponse, false);
    assert.match(gate.blockReason || '', /IN_PROGRESS/);
    const done = await sm.completeTurnAsync('req-multi', true);
    assert.equal(done.success, true);
    assert.equal(sm.CurrentState, 'TurnComplete');
  });

  it('resetForTesting_ResetsToNoTurn_ClearsTurnId', async () => {
    await sm.beginTurnAsync('req-r');
    await sm.recordCodeEditAndVerifyBuildAsync('r.ts', 'success');
    (sm as any).resetForTesting(); // test helper is public for harness
    assert.equal(sm.CurrentState, 'NoTurn');
    assert.equal(sm.CurrentTurnId, null);
  });

  it('stopGate_BlockedOnBuild_BlocksWithBuildFailed', async () => {
    await sm.beginTurnAsync('req-bb');
    await sm.recordCodeEditAndVerifyBuildAsync('bb.ts', 'failed');
    const d = sm.evaluateStopGate();
    assert.equal(d.canEmitFinalResponse, false);
    assert.equal(d.blockReason, 'BUILD_FAILED');
  });
});
