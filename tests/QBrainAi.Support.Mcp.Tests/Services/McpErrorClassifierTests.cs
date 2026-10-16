using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGEERR-001: <see cref="McpErrorClassifier"/> emits the four-field envelope
/// for validation, not-found, persistence (with inner), and backend_unavailable.
/// </summary>
public sealed class McpErrorClassifierTests
{
    /// <summary>Sqlite CANTOPEN is backend_unavailable and retryable.</summary>
    [Fact]
    public void Classify_SqliteCantOpen_IsBackendUnavailableRetryable()
    {
        var result = McpErrorClassifier.Classify(new SqliteException("unable to open database file", 14));

        Assert.Equal(McpErrorClassifier.BackendUnavailable, result.Code);
        Assert.Equal(McpErrorClassifier.BackendUnavailableMessage, result.Message);
        Assert.True(result.Retryable);
        Assert.Equal(503, result.StatusCode);
        Assert.Equal("backend_unavailable", result.Details!["reason"]);
    }

    /// <summary>DbUpdateException includes innermost provider text in details.inner.</summary>
    [Fact]
    public void Classify_DbUpdateException_IncludesInnermostProviderText()
    {
        var inner = new SqliteException("UNIQUE constraint failed: SessionLogTurns.RequestId", 19);
        var exception = new DbUpdateException(
            "An error occurred while saving the entity changes. See the inner exception for details.",
            inner);

        var result = McpErrorClassifier.Classify(exception);

        Assert.Equal(McpErrorClassifier.Conflict, result.Code);
        Assert.False(result.Retryable);
        Assert.NotNull(result.Details);
        Assert.Equal(inner.Message, result.Details!["inner"]);
        Assert.DoesNotContain("See the inner exception", result.Message, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-TRIAGESTORE-005: SQLITE_BUSY is persistence_error and retryable.</summary>
    [Fact]
    public void Classify_SqliteBusy_IsRetryablePersistenceError()
    {
        var result = McpErrorClassifier.Classify(new SqliteException("database is locked", 5));

        Assert.Equal(McpErrorClassifier.PersistenceError, result.Code);
        Assert.True(result.Retryable);
        Assert.NotNull(result.Details);
        Assert.Contains("locked", result.Details!["inner"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-196 / TR-MCP-PERSIST-004: SQLITE_BUSY is persist contention, not storage-down.
    /// </summary>
    [Fact]
    public void Classify_SqliteBusy_IsNotBackendUnavailable()
    {
        var result = McpErrorClassifier.Classify(new SqliteException("database is locked", 5));

        Assert.NotEqual(McpErrorClassifier.BackendUnavailable, result.Code);
        Assert.NotEqual(McpErrorClassifier.BackendUnavailableMessage, result.Message);
    }

    /// <summary>ArgumentException is validation_error and not retryable.</summary>
    [Fact]
    public void Classify_ArgumentException_IsValidationError()
    {
        var result = McpErrorClassifier.Classify(new ArgumentException("sourceType is required."));

        Assert.Equal(McpErrorClassifier.ValidationError, result.Code);
        Assert.False(result.Retryable);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("validation", result.Details!["reason"]);
    }

    /// <summary>KeyNotFoundException is not_found.</summary>
    [Fact]
    public void Classify_KeyNotFound_IsNotFound()
    {
        var result = McpErrorClassifier.Classify(new KeyNotFoundException("Turn not found."));

        Assert.Equal(McpErrorClassifier.NotFound, result.Code);
        Assert.Equal(404, result.StatusCode);
        Assert.False(result.Retryable);
        Assert.Equal("not_found", result.Details!["reason"]);
    }

    // TEST-MCP-TRIAGEERR-001 AC-004: the message EF Core's SQL Server execution strategy gives the InvalidOperationException it reports a
    // transient error in (run a7c-binding-20261016T0914), and the save failure EF Core reports inside it.
    private const string TransientFailureMessage =
        "An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure' to the 'UseSqlServer' call.";

    private const string SaveFailedMessage = "An error occurred while saving the entity changes. See the inner exception for details.";

    /// <summary>
    /// TEST-MCP-TRIAGEERR-001 AC-004 mock gate (TR-MCP-TRIAGEERR-001 AC-004, TEST-MCP-TRIAGESTORE-005; PLAN-SESSIONTRIAGEFIX-001 A7 follow-up,
    /// triage-report-905c3ac1b6af4d57b9c24c2383642379): the execution-strategy wrapper assertion accepts scripted classifications that give EF
    /// Core's transient-failure InvalidOperationException the classification of the database error inside it, and rejects the wrapped deadlock
    /// classified internal_server_error or without details.inner, the wrapped deadlock whose message is not in English classified
    /// internal_server_error, a wrapped unique violation classified retryable, and a plain InvalidOperationException classified retryable.
    /// Data: hand-built exception chains and scripted classifications; no database and no classifier code.
    /// </summary>
    [Fact]
    public void ExecutionStrategyWrapperAssertion_AcceptsWrappedErrorClassificationAndRejectsDeviations()
    {
        var cases = ExecutionStrategyWrapperCases();
        AssertExecutionStrategyWrapperCases(cases, Scripted(cases, deviating: null, static fields => fields));

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertExecutionStrategyWrapperCases(cases, Scripted(
            cases, "wrapped deadlock", static fields => fields with { Code = McpErrorClassifier.InternalError, Retryable = false })));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertExecutionStrategyWrapperCases(cases, Scripted(
            cases, "wrapped deadlock", static fields => fields with { Inner = null })));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertExecutionStrategyWrapperCases(cases, Scripted(
            cases, "wrapped deadlock, message not in English", static fields => fields with { Code = McpErrorClassifier.InternalError, Retryable = false })));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertExecutionStrategyWrapperCases(cases, Scripted(
            cases, "wrapped unique violation", static fields => fields with { Retryable = true })));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertExecutionStrategyWrapperCases(cases, Scripted(
            cases, "plain InvalidOperationException", static fields => fields with { Retryable = true })));
    }

    /// <summary>
    /// TEST-MCP-TRIAGEERR-001 AC-004 binding (TR-MCP-TRIAGEERR-001 AC-004, TEST-MCP-TRIAGESTORE-005; PLAN-SESSIONTRIAGEFIX-001 A7 follow-up,
    /// triage-report-905c3ac1b6af4d57b9c24c2383642379): the mock gate's cases and assertion, bound to McpErrorClassifier.Classify. Data: the same
    /// hand-built exception chains; no database.
    /// </summary>
    [Fact]
    public void Classify_ExecutionStrategyWrapper_ClassifiesTheWrappedDatabaseError()
        => AssertExecutionStrategyWrapperCases(ExecutionStrategyWrapperCases(), McpErrorClassifier.Classify);

    // TEST-MCP-TRIAGEERR-001 AC-004: the chains EF Core's SQL Server execution strategy raises, each with the classification fields
    // TR-MCP-TRIAGEERR-001 AC-004 and TEST-MCP-TRIAGESTORE-005 require. The English deadlock text is SQL Server's message for error 1205 from
    // run a7c-binding-20261016T0914; the other is a French stand-in for a localized message, without the word deadlock.
    private static WrapperCase[] ExecutionStrategyWrapperCases()
    {
        var deadlock = SqlExceptionFactory.Create(
            1205,
            "Transaction (Process ID 71) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction.");
        var deadlockNotEnglish = SqlExceptionFactory.Create(
            1205,
            "La transaction (ID de processus 71) a ete bloquee sur les ressources verrou avec un autre processus et a ete choisie comme victime. Reexecutez la transaction.");
        var unique = new SqliteException("UNIQUE constraint failed: SessionLogProcessingDialog.SessionLogTurnId, SessionLogProcessingDialog.Ordinal", 19);
        return
        [
            new("wrapped deadlock", Wrapped(deadlock), McpErrorClassifier.PersistenceError, Retryable: true, deadlock.Message),
            new("wrapped deadlock, message not in English", Wrapped(deadlockNotEnglish), McpErrorClassifier.PersistenceError, Retryable: true, deadlockNotEnglish.Message),
            new("wrapped unique violation", Wrapped(unique), McpErrorClassifier.Conflict, Retryable: false, unique.Message),
            new(
                "plain InvalidOperationException",
                new InvalidOperationException("Persist pending session graph edits before submitting a session snapshot."),
                McpErrorClassifier.InternalError,
                Retryable: false,
                Inner: null),
        ];
    }

    // TEST-MCP-TRIAGEERR-001 AC-004: a provider error as EF Core's SQL Server execution strategy reports a failed save.
    private static InvalidOperationException Wrapped(Exception providerError)
        => new(TransientFailureMessage, new DbUpdateException(SaveFailedMessage, providerError));

    // TEST-MCP-TRIAGEERR-001 AC-004: a scripted classifier that answers each case's chain with that case's required fields, the fields of the
    // case named by deviating changed by deviate first.
    private static Func<Exception, McpErrorClassification> Scripted(
        IReadOnlyList<WrapperCase> cases, string? deviating, Func<WrapperCase, WrapperCase> deviate)
        => exception =>
        {
            var match = cases.Single(wrapperCase => ReferenceEquals(wrapperCase.Input, exception));
            var answer = string.Equals(match.Name, deviating, StringComparison.Ordinal) ? deviate(match) : match;
            var details = answer.Inner is null ? null : new Dictionary<string, object?>(StringComparer.Ordinal) { ["inner"] = answer.Inner };
            return new McpErrorClassification(answer.Code, "scripted", answer.Retryable, details, StatusCode: 0);
        };

    // TEST-MCP-TRIAGEERR-001 AC-004: every case's chain gets the code and retryable flag TR-MCP-TRIAGEERR-001 AC-004 requires, and the
    // provider text in details.inner where the case carries one.
    private static void AssertExecutionStrategyWrapperCases(IReadOnlyList<WrapperCase> cases, Func<Exception, McpErrorClassification> classify)
    {
        foreach (var wrapperCase in cases)
        {
            var result = classify(wrapperCase.Input);
            Assert.True(
                string.Equals(result.Code, wrapperCase.Code, StringComparison.Ordinal),
                $"{wrapperCase.Name}: code {result.Code}, required {wrapperCase.Code}.");
            Assert.True(result.Retryable == wrapperCase.Retryable, $"{wrapperCase.Name}: retryable {result.Retryable}, required {wrapperCase.Retryable}.");
            if (wrapperCase.Inner is null)
                continue;

            var inner = result.Details is not null && result.Details.TryGetValue("inner", out var value) ? value as string : null;
            Assert.True(
                string.Equals(inner, wrapperCase.Inner, StringComparison.Ordinal),
                $"{wrapperCase.Name}: details.inner '{inner}', required '{wrapperCase.Inner}'.");
        }
    }

    /// <summary>TEST-MCP-TRIAGEERR-001 AC-004: one exception chain and the classification fields TR-MCP-TRIAGEERR-001 AC-004 requires for it.</summary>
    /// <param name="Name">The case name the assertion messages carry.</param>
    /// <param name="Input">The exception chain the classifier receives.</param>
    /// <param name="Code">The required envelope code.</param>
    /// <param name="Retryable">The required retryable flag.</param>
    /// <param name="Inner">The provider text details.inner must carry, or null when the case requires none.</param>
    internal sealed record WrapperCase(string Name, Exception Input, string Code, bool Retryable, string? Inner);
}
