namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Normalized parser observation, independent of any eventual public DTO or error envelope.</summary>
/// <param name="Accepted">Whether the input is a generated ID under the configured fixture registry.</param>
/// <param name="CanonicalId">Exact generated identity on success.</param>
/// <param name="Family">Registered semantic family, excluding creator provenance.</param>
/// <param name="Tag">Exactly two uppercase ASCII letters or digits.</param>
/// <param name="Sequence">Positive Int64 sequence.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-004 B4; FR-MCP-182 ac-TwoNodesCreateDistinctIds and ac-LegacyIdsRemainValid;
/// TR-MCP-FEDERATION-009; TEST-MCP-230. Tests the documented proposal, not an adopted production schema.</remarks>
internal sealed record Phase0ParsedGeneratedId(bool Accepted, string? CanonicalId, string? Family, string? Tag, long? Sequence);

/// <summary>Pure formatting/parsing/presentation boundary for the proposed B4 grammar.</summary>
/// <remarks>2026-11-19: FR-MCP-182 ac-TwoNodesCreateDistinctIds; TR-MCP-FEDERATION-009; TEST-MCP-230.
/// Future native bindings configure the same explicit fixture registry. These methods never allocate, reserve, import or mutate IDs.</remarks>
internal interface IPhase0IdentityGrammarProbe
{
    /// <summary>Formats a registered family, fixture creator tag and sequence; null denotes invalid input.</summary>
    /// <param name="family">Registered family.</param><param name="tag">Fixture tag.</param><param name="sequence">Sequence.</param>
    /// <returns>Canonical generated ID or null. No public error-envelope shape is selected.</returns>
    string? Format(string family, string tag, long sequence);
    /// <summary>Parses a generated identity under the fixture registry, never treating a legacy ID as a generated one.</summary>
    /// <param name="text">Exact input, including malformed or legacy strings.</param><returns>Normalized observation.</returns>
    Phase0ParsedGeneratedId? Parse(string? text);
    /// <summary>Orders identities within the same family/tag numerically without changing or losing strings.</summary>
    /// <param name="identities">Exact caller-provided identities.</param><returns>Complete ordered identities.</returns>
    IReadOnlyList<string>? Sort(IReadOnlyList<string> identities);
}

/// <summary>Independent literal expectations for B4's canonical decimal, tag and family boundaries.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-004; FR-MCP-182 ac-TwoNodesCreateDistinctIds, ac-LegacyIdsRemainValid;
/// TR-MCP-FEDERATION-009; TEST-MCP-230. This is a proposal mock contract, not a production parser or allocator.</remarks>
internal static class Phase0IdentityGrammarContract
{
    /// <summary>Checks formatting and each independently returned parse field against the literal fixture.</summary>
    /// <param name="probe">Mock or later native boundary.</param><param name="family">Registry entry.</param>
    /// <param name="tag">Creator tag.</param><param name="sequence">Numeric value.</param><param name="expected">Literal canonical ID.</param>
    internal static void AssertValid(IPhase0IdentityGrammarProbe probe, string family, string tag, long sequence, string expected)
    {
        Assert.Equal(expected, probe.Format(family, tag, sequence));
        var parsed = probe.Parse(expected);
        Assert.NotNull(parsed);
        Assert.True(parsed.Accepted);
        Assert.Equal(expected, parsed.CanonicalId);
        Assert.Equal(family, parsed.Family);
        Assert.Equal(tag, parsed.Tag);
        Assert.Equal(sequence, parsed.Sequence);
    }

    /// <summary>Requires explicit rejection of a malformed, unregistered or legacy generated-ID input.</summary>
    /// <param name="probe">Parser boundary.</param><param name="input">Exact invalid input.</param>
    internal static void AssertRejected(IPhase0IdentityGrammarProbe probe, string? input)
    {
        var parsed = probe.Parse(input);
        Assert.NotNull(parsed);
        Assert.False(parsed.Accepted);
    }

    /// <summary>Checks invalid inputs cannot be formatted into a generated identity.</summary>
    /// <param name="probe">Formatter boundary.</param><param name="family">Family.</param>
    /// <param name="tag">Tag.</param><param name="sequence">Sequence.</param>
    internal static void AssertInvalidFormat(IPhase0IdentityGrammarProbe probe, string family, string tag, long sequence)
        => Assert.Null(probe.Format(family, tag, sequence));

    /// <summary>Checks 999 precedes1000, duplicate inputs survive, and canonical text is never rewritten by sorting.</summary>
    /// <param name="probe">Presentation-order boundary.</param><param name="family">Registry family.</param><param name="tag">Tag.</param>
    internal static void AssertNumericOrder(IPhase0IdentityGrammarProbe probe, string family, string tag)
    {
        var prefix = family + "-" + tag + "-";
        string[] input = [prefix + "1000", prefix + "042", prefix + "9223372036854775807", prefix + "001", prefix + "999", prefix + "042"];
        string[] expected = [prefix + "001", prefix + "042", prefix + "042", prefix + "999", prefix + "1000", prefix + "9223372036854775807"];
        Assert.Equal(expected, Assert.IsAssignableFrom<IReadOnlyList<string>>(probe.Sort(input)));
        Assert.Equal(new[] { prefix + "1000", prefix + "042", prefix + "9223372036854775807", prefix + "001", prefix + "999", prefix + "042" }, input);
    }
}

/// <summary>Scripted correct and faulty replies validate the B4 grammar assertions without implementing a parser.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-004; FR-MCP-182 ac-TwoNodesCreateDistinctIds, ac-LegacyIdsRemainValid;
/// TR-MCP-FEDERATION-009; TEST-MCP-230. Registry entries are from the B4 design proposal. LA/LG and other tags are fixtures,
/// not enrollment. Existing human/legacy IDs are rejected only by this generated-ID parser; the alias resolver remains separate.</remarks>
public sealed class Phase0IdentityGrammarMockGateTests
{
    // Explicit examples include a validated semantic TODO prefix and memory category; they do not license arbitrary families.
    private static readonly string[] Families = ["EXEC-TODO", "BUG-TRIAGE", "IMPL-UNITGREEN", "FR-MCP", "TR-MCP", "TEST-MCP", "REQ-AC",
        "MEMORY-FACT", "MEMORY-VERSION", "MEMORY-EDGE", "UC", "ACTOR", "UC-FLOW", "UC-STEP", "UC-EXTENSION", "UC-SPECIAL", "UC-FR-LINK",
        "TODO-LIST", "TODO-TASK", "TODO-NOTE", "TODO-COMPLETED-GROUP", "TODO-COMPLETED-ITEM", "PROJECT-BANNED", "SESSION-SNAPSHOT"];
    private static readonly string[] Tags = ["LA", "LG", "00", "A0", "0Z", "ZZ"];
    private static readonly (long Value, string Text)[] Sequences = [(1, "001"), (42, "042"), (999, "999"), (1000, "1000"), (long.MaxValue, "9223372036854775807")];

    /// <summary>Gets every proposed family, creator-tag boundary and literal Int64 formatting boundary.</summary>
    public static IEnumerable<object[]> ValidInputs
    {
        get
        {
            foreach (var family in Families)
                foreach (var tag in Tags)
                    foreach (var sequence in Sequences)
                        yield return [family, tag, sequence.Value, family + "-" + tag + "-" + sequence.Text];
        }
    }

    /// <summary>Gets individual formatter and parser-result corruptions over every valid fixture.</summary>
    public static IEnumerable<object[]> WrongResults
    {
        get
        {
            foreach (var row in ValidInputs)
                foreach (var fault in new[] { "FormatMissing", "FormatChanged", "ParseMissing", "ParseRejected", "CanonicalId", "Family", "Tag", "Sequence" })
                    yield return [row[0], row[1], row[2], row[3], fault];
        }
    }

    /// <summary>Gets malformed canonical numbers, tags, delimiters and non-generated legacy identities.</summary>
    public static IEnumerable<object?[]> InvalidInputs
    {
        get
        {
            yield return [null];
            foreach (var input in new[] { "", " ", "IMPL-UNITGREEN-001", "TODO-7", "FR-MCP-175", "UC-7", "ISSUE-87", "NOT-REGISTERED-LA-001" })
                yield return [input];
            foreach (var family in Families)
            {
                foreach (var suffix in new[] { "0", "000", "0001", "0042", "0999", "01000", "00", "01", "42", "1", "+001", "-001", "1e3", "1.0", " 001", "001 ", "001\n", "001\r\n", "١", "００１", "9223372036854775808", "18446744073709551615" })
                    yield return [family + "-LA-" + suffix];
                foreach (var tag in new[] { "", "L", "LAA", "la", "La", "lA", "L_", "L-", " L", "L ", "LÅ", "雪L", "L١" })
                    yield return [family + "-" + tag + "-001"];
                foreach (var text in new[] { family + "-LA-001-extra", " " + family + "-LA-001", family.ToLowerInvariant() + "-LA-001", family + "_LA_001" })
                    yield return [text];
            }
        }
    }

    /// <summary>Gets invalid formatter values without choosing any new protocol error code.</summary>
    public static IEnumerable<object[]> InvalidFormatInputs
    {
        get
        {
            foreach (var family in Families)
            {
                foreach (var number in new[] { long.MinValue, -1L, 0L }) yield return [family, "LA", number];
                foreach (var tag in new[] { "", "L", "LAA", "la", "La", "L_", "LÅ", " L", "L " }) yield return [family, tag, 1L];
            }
            foreach (var family in new[] { "", "NOT-REGISTERED", "fr-mcp", "-FR", "FR-", "FR--MCP", "FR_MCP", "FR MCP", "雪" })
                yield return [family, "LA", 1L];
        }
    }

    /// <summary>Gets numeric-order cases for every fixture registry family and creator tag.</summary>
    public static IEnumerable<object[]> SortInputs => Families.SelectMany(family => Tags.Select(tag => new object[] { family, tag }));

    /// <summary>Gets individual missing, reordered, dropped, added and rewritten sorting outcomes.</summary>
    public static IEnumerable<object[]> WrongSortResults => SortInputs.SelectMany(row =>
        new[] { "Missing", "Lexical", "Reverse", "Drop", "Extra", "Rewrite" }.Select(fault => new[] { row[0], row[1], fault }));

    /// <summary>Proves canonical formatting and parsed metadata accept all independently supplied correct replies.</summary>
    /// <param name="family">Registry entry.</param><param name="tag">Tag.</param><param name="sequence">Value.</param><param name="expected">Literal ID.</param>
    [Theory, MemberData(nameof(ValidInputs))]
    public void CorrectGrammarPasses(string family, string tag, long sequence, string expected)
    {
        var probe = new GrammarScript(family, tag, sequence, expected);
        Phase0IdentityGrammarContract.AssertValid(probe, family, tag, sequence, expected);
        Assert.Equal(new[] { "format", "parse" }, probe.Calls);
    }

    /// <summary>Proves each corrupt formatter or parsed field is rejected across the complete valid fixture set.</summary>
    /// <param name="family">Family.</param><param name="tag">Tag.</param><param name="sequence">Value.</param>
    /// <param name="expected">Literal ID.</param><param name="fault">Single independent defect.</param>
    [Theory, MemberData(nameof(WrongResults))]
    public void WrongGrammarIsRejected(string family, string tag, long sequence, string expected, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0IdentityGrammarContract.AssertValid(
            new GrammarScript(family, tag, sequence, expected, fault), family, tag, sequence, expected));

    /// <summary>Proves explicit generated-ID rejection is accepted, including legacy strings that belong to alias resolution.</summary>
    /// <param name="input">Exact malformed or legacy input.</param>
    [Theory, MemberData(nameof(InvalidInputs))]
    public void CorrectInvalidInputRejectionPasses(string? input)
    {
        var probe = new RejectionScript(input, false);
        Phase0IdentityGrammarContract.AssertRejected(probe, input);
        Assert.Equal(1, probe.Reads);
    }

    /// <summary>Proves accepting an invalid generated ID cannot pass the rejection assertion.</summary>
    /// <param name="input">Exact malformed or legacy input.</param>
    [Theory, MemberData(nameof(InvalidInputs))]
    public void WrongInvalidInputAcceptanceIsRejected(string? input)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0IdentityGrammarContract.AssertRejected(new RejectionScript(input, true), input));

    /// <summary>A missing parse reply is not evidence of explicit rejection.</summary>
    [Fact]
    public void MissingRejectionObservationIsRejected()
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0IdentityGrammarContract.AssertRejected(new RejectionScript(null, false, true), null));

    /// <summary>Proves formatter refusal accepts correct null replies for all malformed parameter fixtures.</summary>
    /// <param name="family">Family.</param><param name="tag">Tag.</param><param name="sequence">Number.</param>
    [Theory, MemberData(nameof(InvalidFormatInputs))]
    public void CorrectInvalidFormatRefusalPasses(string family, string tag, long sequence)
        => Phase0IdentityGrammarContract.AssertInvalidFormat(new GrammarScript(family, tag, sequence, null), family, tag, sequence);

    /// <summary>Proves emitting an identity for malformed formatter inputs fails the contract.</summary>
    /// <param name="family">Family.</param><param name="tag">Tag.</param><param name="sequence">Number.</param>
    [Theory, MemberData(nameof(InvalidFormatInputs))]
    public void WrongInvalidFormatAcceptanceIsRejected(string family, string tag, long sequence)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0IdentityGrammarContract.AssertInvalidFormat(
            new GrammarScript(family, tag, sequence, "incorrectly-created"), family, tag, sequence));

    /// <summary>Checks numeric presentation order, duplicate preservation and unchanged caller input.</summary>
    /// <param name="family">Family.</param><param name="tag">Tag.</param>
    [Theory, MemberData(nameof(SortInputs))]
    public void CorrectNumericOrderPasses(string family, string tag)
        => Phase0IdentityGrammarContract.AssertNumericOrder(new OrderScript(family, tag), family, tag);

    /// <summary>Proves lexical order, silent deduplication, lost rows and altered IDs are rejected.</summary>
    /// <param name="family">Family.</param><param name="tag">Tag.</param><param name="fault">Independent faulty reply.</param>
    [Theory, MemberData(nameof(WrongSortResults))]
    public void WrongNumericOrderIsRejected(string family, string tag, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0IdentityGrammarContract.AssertNumericOrder(new OrderScript(family, tag, fault), family, tag));

    /// <summary>Exact scripted observation, with no implementation of formatting, registry validation or parsing.</summary>
    private sealed class GrammarScript(string family, string tag, long sequence, string? expected, string fault = "") : IPhase0IdentityGrammarProbe
    {
        /// <summary>Gets observed boundary calls.</summary>
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public string? Format(string actualFamily, string actualTag, long actualSequence)
        {
            Calls.Add("format");
            Assert.Equal(family, actualFamily); Assert.Equal(tag, actualTag); Assert.Equal(sequence, actualSequence);
            return fault switch { "FormatMissing" => null, "FormatChanged" => expected + "0", _ => expected };
        }
        /// <inheritdoc />
        public Phase0ParsedGeneratedId? Parse(string? text)
        {
            Calls.Add("parse"); Assert.Equal(expected, text);
            var correct = new Phase0ParsedGeneratedId(true, expected, family, tag, sequence);
            return fault switch
            {
                "ParseMissing" => null, "ParseRejected" => correct with { Accepted = false },
                "CanonicalId" => correct with { CanonicalId = expected + "0" }, "Family" => correct with { Family = "OTHER" },
                "Tag" => correct with { Tag = tag == "LA" ? "LG" : "LA" }, "Sequence" => correct with { Sequence = sequence == long.MaxValue ? 1 : sequence + 1 },
                _ => correct
            };
        }
        /// <inheritdoc />
        public IReadOnlyList<string>? Sort(IReadOnlyList<string> identities) => throw new InvalidOperationException("Unexpected sort call.");
    }

    /// <summary>Independent malformed-input reply; never examines input syntax to choose its answer.</summary>
    private sealed class RejectionScript(string? expected, bool accepted, bool missing = false) : IPhase0IdentityGrammarProbe
    {
        /// <summary>Gets parser calls.</summary>
        internal int Reads { get; private set; }
        /// <inheritdoc />
        public Phase0ParsedGeneratedId? Parse(string? text)
        {
            Reads++; Assert.Equal(expected, text);
            return missing ? null : new(accepted, null, null, null, null);
        }
        /// <inheritdoc />
        public string? Format(string family, string tag, long sequence) => throw new InvalidOperationException("Unexpected format call.");
        /// <inheritdoc />
        public IReadOnlyList<string>? Sort(IReadOnlyList<string> identities) => throw new InvalidOperationException("Unexpected sort call.");
    }

    /// <summary>Literal order script; it does not parse IDs or sort numeric suffixes.</summary>
    private sealed class OrderScript(string family, string tag, string fault = "") : IPhase0IdentityGrammarProbe
    {
        /// <inheritdoc />
        public IReadOnlyList<string>? Sort(IReadOnlyList<string> identities)
        {
            var p = family + "-" + tag + "-";
            Assert.Equal(new[] { p + "1000", p + "042", p + "9223372036854775807", p + "001", p + "999", p + "042" }, identities);
            string[] correct = [p + "001", p + "042", p + "042", p + "999", p + "1000", p + "9223372036854775807"];
            return fault switch
            {
                "Missing" => null, "Lexical" => [p + "001", p + "042", p + "042", p + "1000", p + "9223372036854775807", p + "999"],
                "Reverse" => correct.Reverse().ToArray(), "Drop" => correct.Where((_, index) => index != 2).ToArray(),
                "Extra" => [.. correct, p + "001"], "Rewrite" => correct.Select(id => id.Replace("-042", "-42", StringComparison.Ordinal)).ToArray(), _ => correct
            };
        }
        /// <inheritdoc />
        public string? Format(string actualFamily, string actualTag, long sequence) => throw new InvalidOperationException("Unexpected format call.");
        /// <inheritdoc />
        public Phase0ParsedGeneratedId? Parse(string? text) => throw new InvalidOperationException("Unexpected parse call.");
    }
}
