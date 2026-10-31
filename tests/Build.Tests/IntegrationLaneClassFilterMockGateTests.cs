using System.Text.RegularExpressions;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2), BDPv4 mock gate: proves the <see cref="IntegrationLaneClassFilterContract"/> assertions
/// accept a scripted filter that composes the contract-derived filters, and reject deliberately wrong filters
/// (negative controls), before any production class filter code is written.
/// </summary>
public sealed class IntegrationLaneClassFilterMockGateTests
{
    /// <summary>TR-MCP-NUKE-019 AC-2: the scripted filter satisfies every composition row and every refusal.</summary>
    [Fact]
    public void ScriptedClassFilter_SatisfiesTheContract()
        => IntegrationLaneClassFilterContract.AssertContract(new ScriptedClassFilter(Fault.None));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-2 negative controls, each rejected inside a passing harness:
    /// - the class alternative without parentheses (so '|' escapes the host filter);
    /// - class matches without the dot bounds;
    /// - the classes joined with '&amp;' instead of '|';
    /// - the host filter dropped;
    /// - the classes ignored;
    /// - any class name accepted;
    /// - an empty class list refused.
    /// </summary>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData(Fault.NoParentheses)]
    [InlineData(Fault.NoDotBounds)]
    [InlineData(Fault.JoinsWithAnd)]
    [InlineData(Fault.DropsHostFilter)]
    [InlineData(Fault.IgnoresClasses)]
    [InlineData(Fault.AcceptsAnyName)]
    [InlineData(Fault.RefusesEmptyList)]
    public void WrongClassFilter_IsRejected(Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => IntegrationLaneClassFilterContract.AssertContract(new ScriptedClassFilter(fault)));

    /// <summary>Faults injected into the scripted filter for negative controls.</summary>
    public enum Fault
    {
        /// <summary>No fault: the contract-derived filter.</summary>
        None,

        /// <summary>Appends the class alternative without parentheses.</summary>
        NoParentheses,

        /// <summary>Matches each class name without the bounding dots.</summary>
        NoDotBounds,

        /// <summary>Joins the class matches with '&amp;'.</summary>
        JoinsWithAnd,

        /// <summary>Returns the class alternative without the host filter.</summary>
        DropsHostFilter,

        /// <summary>Returns the host filter whatever the classes.</summary>
        IgnoresClasses,

        /// <summary>Accepts any class name and any host filter.</summary>
        AcceptsAnyName,

        /// <summary>Refuses an empty class list instead of returning the host filter.</summary>
        RefusesEmptyList,
    }

    /// <summary>Scripted filter: the contract-derived composition, with an optional injected fault.</summary>
    private sealed class ScriptedClassFilter(Fault fault) : IIntegrationLaneClassFilter
    {
        private static readonly Regex PlainIdentifier = new("^[A-Za-z_][A-Za-z0-9_]*\\z", RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Compose(string hostFilter, IReadOnlyList<string> classNames)
        {
            if (fault != Fault.AcceptsAnyName)
            {
                if (string.IsNullOrWhiteSpace(hostFilter))
                    throw new ArgumentException("The host filter is blank.", nameof(hostFilter));
                foreach (var name in classNames)
                {
                    if (!PlainIdentifier.IsMatch(name))
                        throw new ArgumentException($"'{name}' is not a plain class name.", nameof(classNames));
                }
            }

            if (classNames.Count == 0)
                return fault == Fault.RefusesEmptyList ? throw new ArgumentException("No classes.", nameof(classNames)) : hostFilter;
            if (fault == Fault.IgnoresClasses)
                return hostFilter;

            var matches = classNames.Select(name => fault == Fault.NoDotBounds ? $"FullyQualifiedName~{name}" : $"FullyQualifiedName~.{name}.");
            var alternative = string.Join(fault == Fault.JoinsWithAnd ? "&" : "|", matches);
            return fault switch
            {
                Fault.NoParentheses => $"{hostFilter}&{alternative}",
                Fault.DropsHostFilter => $"({alternative})",
                _ => $"{hostFilter}&({alternative})",
            };
        }
    }
}
