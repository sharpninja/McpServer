using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent EF metadata and physical information_schema/SHOW CREATE TABLE observations.</summary>
/// <param name="Logical">Actual EF model, including converter observations.</param>
/// <param name="Physical">Actual persisted columns, constraints and indexes, not the EF model repeated.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0DoltModelObservation(JsonObject? Logical, JsonObject? Physical);

/// <summary>Canonical typed value returned from a real round trip, including type-specific precision information.</summary>
/// <param name="Id">Unique specimen identity.</param>
/// <param name="Type">CLR type or explicit type/Kind identity.</param>
/// <param name="Value">Invariant exact text, hexadecimal bytes/bits, or null.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0DoltScalar(string Id, string Type, string? Value);

/// <summary>Only the independently specified columns relevant to a conditional unique-key experiment.</summary>
/// <param name="Id">Numeric product id or source-compatible slot/report string id.</param>
/// <param name="Workspace">Seeded workspace identity.</param>
/// <param name="Key">Product key, role or nullable idempotency key.</param>
/// <param name="Enabled">Whether the role participates in uniqueness.</param>
/// <param name="Deleted">Original soft-delete state.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0ConditionalRow(string Id, string Workspace, string? Key, bool Enabled, bool Deleted);

/// <summary>Completed database write outcome, distinct from subsequent durable row reads.</summary>
/// <param name="Accepted">Whether the second SQL insert completed.</param>
/// <param name="NativeError">1062 for the expected duplicate-key rejection, otherwise null on success.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0ConditionalOutcome(bool Accepted, int? NativeError);

/// <summary>Full standalone model, lossless scalar and conditional-constraint observation boundary.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal interface IPhase0DoltFullModelProbe
{
    /// <summary>Creates/migrates the complete disposable model or closes all connections and reopens it, then independently reads EF and physical metadata.</summary>
    /// <param name="stage">Created or Reopened, in that order.</param>
    /// <returns>Complete actual observations, with canonicalized equivalent SQL syntax only.</returns>
    Phase0DoltModelObservation ReadModels(string stage);
    /// <summary>Persists typed specimens, closes the connection, and returns values from the actual reopened data reader.</summary>
    /// <param name="specimens">Fixed invariant specimen specification; the adapter must parse and bind actual CLR values.</param>
    /// <returns>Reopened values, canonicalized from the returned CLR values rather than echoed inputs.</returns>
    IReadOnlyList<Phase0DoltScalar>? Roundtrip(IReadOnlyList<Phase0DoltScalar> specimens);
    /// <summary>In a fresh scenario fixture, seeds required parents/first row, then attempts the second insert through actual SQL.</summary>
    /// <param name="scenario">Product, Brain or Triage scenario identity.</param>
    /// <param name="seed">Initial row with its original predicate state.</param>
    /// <param name="attempt">Second row, never an update to the seed.</param>
    /// <returns>Observed SQL outcome and diagnostic.</returns>
    Phase0ConditionalOutcome InsertConditional(string scenario, Phase0ConditionalRow seed, Phase0ConditionalRow attempt);
    /// <summary>Reads all scenario rows with soft-delete filters disabled, optionally after disposing all contexts/connections.</summary>
    /// <param name="scenario">Previously attempted scenario identity.</param>
    /// <param name="reopen">Whether to reopen before reading.</param>
    /// <returns>Actual complete conditional-column projection.</returns>
    IReadOnlyList<Phase0ConditionalRow>? ReadConditional(string scenario, bool reopen);
}

/// <summary>Checks the full independently specified model and actual data semantics through a replaceable probe.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.
/// The desired fixture is a compatibility experiment, not evidence that Dolt supports the selected mappings.
/// Created/Reopened observations must come from actual EF and physical metadata separately in the native adapter.
/// </remarks>
internal static class Phase0DoltFullModelContract
{
    /// <summary>Checks both complete inventories at creation and reopen, freezing both expected models before any probe call.</summary>
    /// <param name="probe">Scripted observer or later native integration adapter.</param>
    /// <param name="desired">Independently pinned complete desired schema.</param>
    internal static void AssertModels(IPhase0DoltFullModelProbe probe, JsonObject desired)
    {
        var logical = desired["logicalModel"]!.DeepClone().AsObject();
        var physical = desired["physicalSchema"]!.DeepClone().AsObject();
        foreach (var stage in new[] { "Created", "Reopened" })
        {
            var observation = probe.ReadModels(stage);
            Assert.NotNull(observation);
            Assert.NotNull(observation.Logical);
            Assert.NotNull(observation.Physical);
            Phase0RelationalModelContract.AssertModel(new ObservedModel(observation.Logical), logical);
            Phase0RelationalModelContract.AssertModel(new ObservedModel(observation.Physical), physical);
        }
    }

    /// <summary>Tests all mapping families and exact reopened values, including seven-digit timestamps with nonzero offsets.</summary>
    /// <param name="probe">Actual round-trip observation source.</param>
    internal static void AssertScalars(IPhase0DoltFullModelProbe probe)
    {
        var expected = Scalars();
        var actual = probe.Roundtrip(expected.ToArray());
        Assert.NotNull(actual);
        Assert.Equal(expected.OrderBy(row => row.Id, StringComparer.Ordinal), actual.OrderBy(row => row.Id, StringComparer.Ordinal));
    }

    /// <summary>Gets literal independent scalar specimens; date Kind, timezone offset, byte payloads and IEEE bits are explicit. Decimal scale retains its separate query/migration fixture.</summary>
    internal static Phase0DoltScalar[] Scalars() =>
    [
        new("null", "System.String", null), new("empty", "System.String", ""),
        new("unicode", "System.String", "東京😀\0 trailing "), new("bytes", "System.Byte[]", "00017FFF00"),
        new("false", "System.Boolean", "False"), new("true", "System.Boolean", "True"),
        new("int-min", "System.Int32", "-2147483648"), new("int-max", "System.Int32", "2147483647"),
        new("long-min", "System.Int64", "-9223372036854775808"), new("long-max", "System.Int64", "9223372036854775807"),
        new("double", "System.Double.Bits", "3FB999999999999A"),
        new("utc", "System.DateTime.Utc", "2026-11-17T01:02:03.1234567Z"),
        new("unspecified", "System.DateTime.Unspecified", "2026-11-17T01:02:03.1234567"),
        new("offset-positive", "System.DateTimeOffset", "2026-11-17T01:02:03.1234567+05:45"),
        new("offset-negative", "System.DateTimeOffset", "2026-11-17T01:02:03.1234567-03:30")
    ];

    /// <summary>Checks duplicate acceptance/rejection and all durable rows before and after reopen.</summary>
    /// <param name="probe">Actual SQL writer/independent reader.</param>
    /// <param name="scenario">Fixed scenario identity from Scenarios.</param>
    internal static void AssertConditional(IPhase0DoltFullModelProbe probe, string scenario)
    {
        var seed = new Phase0ConditionalRow("1", "Atlas", "same", true, false);
        var attempt = seed with { Id = "2" };
        var accepted = false;
        switch (scenario)
        {
            case "Product.ActiveDuplicate": break;
            case "Product.DeletedDuplicate": attempt = attempt with { Deleted = true }; accepted = true; break;
            case "Product.DeletedThenActive": seed = seed with { Deleted = true }; accepted = true; break;
            case "Product.CaseDistinct": attempt = attempt with { Key = "Same" }; accepted = true; break;
            case "Brain.EnabledDuplicate": break;
            case "Brain.DisabledDuplicate": attempt = attempt with { Enabled = false }; accepted = true; break;
            case "Brain.DeletedDuplicate": attempt = attempt with { Deleted = true }; accepted = true; break;
            case "Brain.OtherWorkspace": attempt = attempt with { Workspace = "Other" }; accepted = true; break;
            case "Triage.NonNullDuplicate": break;
            case "Triage.NullDuplicate": seed = seed with { Key = null }; attempt = attempt with { Key = null }; accepted = true; break;
            case "Triage.OtherWorkspace": attempt = attempt with { Workspace = "Other" }; accepted = true; break;
            default: throw new ArgumentOutOfRangeException(nameof(scenario));
        }
        var expected = accepted ? new[] { seed, attempt } : [seed];
        var outcome = probe.InsertConditional(scenario, seed, attempt);
        Assert.Equal(new Phase0ConditionalOutcome(accepted, accepted ? null : 1062), outcome);
        foreach (var reopen in new[] { false, true })
        {
            var rows = probe.ReadConditional(scenario, reopen);
            Assert.NotNull(rows);
            Assert.Equal(expected.OrderBy(row => row.Id, StringComparer.Ordinal), rows.OrderBy(row => row.Id, StringComparer.Ordinal));
        }
    }

    /// <summary>Gets every independently specified conditional-constraint case.</summary>
    internal static readonly string[] Scenarios = ["Product.ActiveDuplicate", "Product.DeletedDuplicate", "Product.DeletedThenActive", "Product.CaseDistinct", "Brain.EnabledDuplicate", "Brain.DisabledDuplicate", "Brain.DeletedDuplicate", "Brain.OtherWorkspace", "Triage.NonNullDuplicate", "Triage.NullDuplicate", "Triage.OtherWorkspace"];

    /// <summary>Supplies one independently read observation to the existing complete field/membership assertions.</summary>
    private sealed class ObservedModel(JsonObject observation) : IPhase0RelationalModelProbe
    {
        /// <inheritdoc />
        public JsonObject ReadModel(string provider, string contextType) => observation;
    }
}

/// <summary>Loads immutable desired-schema fixture bytes with an explicit cross-host line-ending pin.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal static class Phase0DoltFullModelFixture
{
    /// <summary>Returns a fresh model object after checking its complete expected-data pin.</summary>
    internal static JsonObject Load()
    {
        using var source = typeof(Phase0DoltFullModelFixture).Assembly.GetManifestResourceStream("Federation.Phase0.desired-dolt-standalone-schema.json");
        Assert.NotNull(source);
        using var reader = new StreamReader(source, Encoding.UTF8);
        var text = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Equal("CBB94211E69057F6B8717283DCFC142BB3F2050356FDB5B762065D29EBEBC2C7", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))));
        return JsonNode.Parse(text)!.AsObject();
    }
}
