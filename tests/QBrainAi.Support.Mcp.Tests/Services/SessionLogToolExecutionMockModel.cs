// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using NSubstitute;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-QB-003/005 / TEST-MCP-AUDITACTION-001: independent test-only session store.
/// Commands validate their effective action inputs before committing a cloned graph;
/// test faults alter dependency behavior, never return a named-case summary.
/// </summary>
internal sealed class SessionLogToolExecutionMockModel
{
    private UnifiedSessionLogDto? _saved;
    private bool _deleted;
    private readonly string? _fault;
    internal List<string> Effects { get; } = [];

    internal SessionLogToolExecutionMockModel(string? fault = null) => _fault = fault;

    internal ISessionLogService CreateService()
    {
        var service = Substitute.For<ISessionLogService>();
        service.SubmitAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Submit((call.Arg<UnifiedSessionLogDto>() ?? throw new ArgumentNullException("incoming")))));
        service.GetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(Read()));
        service.UpsertTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(WriteTurn((call.Arg<UnifiedRequestEntryDto>() ?? throw new ArgumentNullException("turn")))));
        service.ReplaceTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(WriteTurn((call.Arg<UnifiedRequestEntryDto>() ?? throw new ArgumentNullException("turn")))));
        service.ReplaceTurnSectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(WriteSection(call.ArgAt<string>(3), (call.Arg<UnifiedRequestEntryDto>() ?? throw new ArgumentNullException("turn")))));
        service.DeleteSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => { Effects.Add("delete"); _deleted = true; return Task.FromResult(true); });
        return service;
    }

    private static T Copy<T>(T input) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(input))!;

    private UnifiedSessionLogDto? Read()
    {
        Effects.Add("read");
        if (_saved is null || _deleted) return null;
        var result = Copy(_saved);
        var turn = result.Turns?.FirstOrDefault();
        if (turn is not null)
        {
            if (_fault == "drop-action") turn.Actions = [];
            if (_fault == "wrong-description") turn.Actions!.First().Description = "wrong stored description";
            if (_fault == "wrong-status") turn.Actions!.First().Status = "wrong-status";
            if (_fault == "wrong-order") turn.Actions!.First().Order = 99;
            if (_fault == "wrong-file") turn.Actions!.First().FilePath = "wrong-file.cs";
            if (_fault == "reverse-dialog") turn.ProcessingDialog = turn.ProcessingDialog!.Reverse().ToList();
            if (_fault == "drop-dialog") turn.ProcessingDialog = [];
            if (_fault == "promote-decision") turn.DesignDecisions = ["inferred from an action"];
            if (_fault == "rewrite-zero") foreach (var action in turn.Actions ?? []) if (action.Order == 0) action.Order = 1;
        }
        return result;
    }

    private long Submit(UnifiedSessionLogDto incoming)
    {
        Effects.Add("submit");
        // A dictionary is the model's independently selected last-wins command buffer.
        var selected = new Dictionary<string, UnifiedRequestEntryDto>(StringComparer.Ordinal);
        foreach (var turn in incoming.Turns ?? [])
            if (_fault != "first-wins" || !selected.ContainsKey(turn.RequestId!)) selected[turn.RequestId!] = turn;
        var effective = selected.Values.Where(turn => !IsLate(turn)).ToArray();
        if (_fault == "leak-before-validation" && _saved is not null) _saved.Title = incoming.Title;
        if (_fault == "resurrect-before-validation") _deleted = false;
        foreach (var turn in effective) Validate(turn.Actions);
        var candidate = _saved is null ? Copy(incoming) : Copy(_saved);
        if (_saved is null) candidate.Turns = [];
        candidate.Title = incoming.Title;
        var merged = (candidate.Turns ?? []).ToDictionary(turn => turn.RequestId!, Copy, StringComparer.Ordinal);
        foreach (var turn in effective) merged[turn.RequestId!] = Copy(turn);
        candidate.Turns = merged.Values.ToList();
        _saved = candidate;
        _deleted = false;
        Effects.Add("commit");
        return 1;
    }

    private bool IsLate(UnifiedRequestEntryDto incoming)
    {
        var prior = _saved?.Turns?.FirstOrDefault(turn => turn.RequestId == incoming.RequestId);
        return _fault != "accept-late" && prior?.Status == "completed" && incoming.Status is "in_progress" or "canceled";
    }

    private long WriteTurn(UnifiedRequestEntryDto turn)
    {
        Effects.Add("turn-write");
        Validate(turn.Actions);
        var next = Copy(_saved ?? throw new InvalidOperationException("Session not open"));
        var turns = (next.Turns ?? []).Where(item => item.RequestId != turn.RequestId).ToList();
        turns.Add(Copy(turn));
        next.Turns = turns;
        _saved = next;
        Effects.Add("commit");
        return 1;
    }

    private bool WriteSection(string section, UnifiedRequestEntryDto payload)
    {
        Effects.Add("section:" + section);
        if (section == "actions") Validate(payload.Actions);
        var next = Copy(_saved ?? throw new InvalidOperationException("Session not open"));
        var turn = next.Turns!.Single(item => item.RequestId == payload.RequestId);
        if (section == "actions") turn.Actions = Copy(payload.Actions);
        if (section == "tags") turn.Tags = Copy(payload.Tags);
        _saved = next;
        Effects.Add("commit");
        return true;
    }

    private void Validate(IEnumerable<UnifiedActionDto>? actions)
    {
        Effects.Add("validate");
        if (_fault == "skip-validation") return;
        foreach (var action in actions ?? [])
        {
            if (!string.Equals(action.Type, "tool_execution", StringComparison.OrdinalIgnoreCase)) continue;
            var field = action.Order < 0 ? "order" : string.IsNullOrWhiteSpace(action.Description) ? "description" : string.IsNullOrWhiteSpace(action.Status) ? "status" : null;
            if (field is not null) { Effects.Add("reject:" + field); throw new ArgumentException("tool_execution invalid " + field); }
        }
    }
}
