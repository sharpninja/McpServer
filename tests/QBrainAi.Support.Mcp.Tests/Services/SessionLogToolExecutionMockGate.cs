// SPDX-License-Identifier: Apache-2.0
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-AUDITACTION-001 / FR-QB-003/005: run every preserved checkpoint case
/// against an independently authored stateful service model before production changes.
/// </summary>
public static class SessionLogToolExecutionMockGate
{
    /// <summary>Runs the same scenario methods with good and deliberately damaged dependency behavior.</summary>
    /// <returns>Complete attributable interaction observations, never case-name outcome constants.</returns>
    public static async Task<SessionLogToolExecutionMockGateResult> RunAsync()
    {
        var observations = new List<SessionLogToolExecutionMockObservation>();
        var cases = new List<(string Name, Func<SessionLogToolExecutionTests, Task> Run)>();
        foreach (var pair in new[] { ("tool_execution", "completed"), ("TOOL_EXECUTION", "failed"), ("tool_execution", "pending") })
        {
            var current = pair;
            cases.Add(("valid:" + current.Item1 + ":" + current.Item2, test => test.ValidExecution_RoundTripsWithoutDecisionPromotion(current.Item1, current.Item2)));
        }
        foreach (var operation in new[] { "submit", "upsert", "replace", "section" })
            foreach (var field in new[] { "description", "status", "order" })
            {
                var op = operation;
                var invalid = field;
                cases.Add(("invalid:" + op + ":" + invalid, test => test.InvalidExecution_RejectsBeforeTrackedOrPersistedMutation(op, invalid)));
            }
        foreach (var existing in new[] { false, true })
        {
            var value = existing;
            cases.Add(("later-invalid:" + value, test => test.InvalidLaterTurn_RejectsWholeSubmission(value)));
            foreach (var invalidLast in new[] { false, true })
            {
                var last = invalidLast;
                cases.Add(("last-wins:" + value + ":" + last, test => test.DuplicateRequestIds_LastWinsBeforeActionValidation(value, last)));
            }
        }
        cases.Add(("legacy", test => test.LegacyCustomAction_RemainsAccepted()));
        foreach (var status in new[] { "in_progress", "canceled" })
        {
            var current = status;
            cases.Add(("terminal:" + current, test => test.LateSnapshot_InvalidActionsRemainIgnored(current)));
        }
        cases.Add(("zero-order", test => test.ZeroExecutionOrder_RoundTripsWithoutPositionalRewrite()));
        cases.Add(("other-section", test => test.OtherSection_IgnoresUnwrittenActions()));
        cases.Add(("deleted-session", test => test.InvalidResubmission_LeavesDeletedSessionDeleted()));

        foreach (var entry in cases)
            observations.Add(await RunOneAsync(entry.Name, entry.Run, null, expectRejection: false).ConfigureAwait(true));

        var controls = new List<(string Fault, Func<SessionLogToolExecutionTests, Task> Run)>
        {
            ("skip-validation", test => test.InvalidExecution_RejectsBeforeTrackedOrPersistedMutation("submit", "description")),
            ("leak-before-validation", test => test.InvalidExecution_RejectsBeforeTrackedOrPersistedMutation("submit", "description")),
            ("first-wins", test => test.DuplicateRequestIds_LastWinsBeforeActionValidation(false, false)),
            ("accept-late", test => test.LateSnapshot_InvalidActionsRemainIgnored("in_progress")),
            ("rewrite-zero", test => test.ZeroExecutionOrder_RoundTripsWithoutPositionalRewrite()),
            ("resurrect-before-validation", test => test.InvalidResubmission_LeavesDeletedSessionDeleted()),
        };
        foreach (var fault in new[] { "drop-action", "promote-decision", "wrong-description", "wrong-status", "wrong-order", "wrong-file", "reverse-dialog", "drop-dialog" })
            controls.Add((fault, test => test.ValidExecution_RoundTripsWithoutDecisionPromotion("tool_execution", "completed")));
        foreach (var entry in controls)
            observations.Add(await RunOneAsync(entry.Fault, entry.Run, entry.Fault, expectRejection: true).ConfigureAwait(true));

        return new SessionLogToolExecutionMockGateResult(cases.Count, controls.Count, observations);
    }

    private static async Task<SessionLogToolExecutionMockObservation> RunOneAsync(
        string name, Func<SessionLogToolExecutionTests, Task> run, string? fault, bool expectRejection)
    {
        var model = new SessionLogToolExecutionMockModel(fault);
        using var test = new SessionLogToolExecutionTests(model);
        Exception? rejection = null;
        try { await run(test).ConfigureAwait(true); }
        catch (Exception error) { rejection = error; }
        Assert.Equal(expectRejection, rejection is not null);
        Assert.Contains("submit", model.Effects);
        Assert.Contains("validate", model.Effects);
        if (!expectRejection) Assert.Contains("read", model.Effects);
        return new SessionLogToolExecutionMockObservation(name, expectRejection, rejection?.GetType().FullName, rejection?.Message, model.Effects.ToArray());
    }
}


