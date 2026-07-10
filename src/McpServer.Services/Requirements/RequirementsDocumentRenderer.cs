using System.Text;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Requirements;

internal static class RequirementsDocumentRenderer
{
    internal const string FunctionalFileName = "Functional-Requirements.md";
    internal const string TechnicalFileName = "Technical-Requirements.md";
    internal const string TestingFileName = "Testing-Requirements.md";
    internal const string MappingFileName = "TR-per-FR-Mapping.md";
    internal const string MatrixFileName = "Requirements-Matrix.md";

    public static string RenderFunctional(IEnumerable<FrEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Functional Requirements (MCP Server)");
        sb.AppendLine();

        foreach (var entry in entries)
        {
            sb.Append("## ").Append(entry.Id).Append(' ').AppendLine(entry.Title);
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(entry.Body))
                sb.AppendLine(entry.Body.Trim());
            AppendScopeMetadata(sb, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey);
            AppendAcceptanceCriteria(sb, entry.AcceptanceCriteria);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string RenderTechnical(IEnumerable<TrEntry> entries) =>
        RenderTechnical(entries, mappings: null);

    public static string RenderTechnical(IEnumerable<TrEntry> entries, IEnumerable<FrTrMapping>? mappings)
    {
        var sb = new StringBuilder();
        var coverageByTrId = BuildCoverageByTechnicalId(mappings);
        sb.AppendLine("# Technical Requirements (MCP Server)");
        sb.AppendLine();

        foreach (var entry in entries)
        {
            sb.Append("## ").AppendLine(entry.Id);
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(entry.Title))
            {
                sb.Append("**").Append(entry.Title.Trim()).Append("**");
                if (!string.IsNullOrWhiteSpace(entry.Body))
                    sb.Append(" — ").Append(entry.Body.Trim());
                sb.AppendLine();
            }
            else if (!string.IsNullOrWhiteSpace(entry.Body))
            {
                sb.AppendLine(entry.Body.Trim());
            }

            AppendCoverageMetadata(sb, entry, coverageByTrId);
            AppendStatusMetadata(sb, entry);
            AppendScopeMetadata(sb, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey);
            AppendAcceptanceCriteria(sb, entry.AcceptanceCriteria);

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string RenderTesting(IEnumerable<TestEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Testing Requirements (MCP Server)");
        sb.AppendLine();

        foreach (var entry in entries)
        {
            sb.Append("- ").Append(entry.Id).Append(": ").AppendLine(entry.Condition.Trim());
            AppendScopeMetadata(sb, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey, listItemIndent: "  ");
            AppendAcceptanceCriteria(sb, entry.AcceptanceCriteria, listItemIndent: "  ");
        }

        if (sb.Length > 0 && sb[^1] != '\n')
            sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>Renders requirement status metadata in a deterministic Markdown-friendly form.</summary>
    internal static void AppendStatusMetadata(StringBuilder sb, string status, string listItemIndent = "")
    {
        var normalized = string.IsNullOrWhiteSpace(status) ? "pending" : status.Trim();
        sb.Append(listItemIndent).Append("**Status:** ").AppendLine(normalized);
    }

    internal static void AppendStatusMetadata(StringBuilder sb, TrEntry entry)
    {
        if (entry.Body.Contains("**Status:**", StringComparison.OrdinalIgnoreCase))
            return;

        AppendStatusMetadata(sb, entry.Status);
    }

    /// <summary>Renders mapping-derived coverage metadata for technical requirements that do not already declare coverage.</summary>
    internal static void AppendCoverageMetadata(
        StringBuilder sb,
        TrEntry entry,
        IReadOnlyDictionary<string, string> coverageByTrId)
    {
        if (entry.Body.Contains("**Covered by:**", StringComparison.OrdinalIgnoreCase))
            return;

        if (!coverageByTrId.TryGetValue(entry.Id, out var coverage) || string.IsNullOrWhiteSpace(coverage))
            return;

        sb.Append("**Covered by:** ").AppendLine(coverage);
    }

    private static IReadOnlyDictionary<string, string> BuildCoverageByTechnicalId(IEnumerable<FrTrMapping>? mappings)
    {
        var frByTrId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var testByTrId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (mappings is null)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings)
        {
            foreach (var trId in mapping.TrIds)
            {
                if (string.IsNullOrWhiteSpace(trId))
                    continue;

                var normalizedTrId = trId.Trim();
                AddUnique(GetOrAdd(frByTrId, normalizedTrId), mapping.FrId);
                foreach (var testId in mapping.TestIds)
                    AddUnique(GetOrAdd(testByTrId, normalizedTrId), testId);
            }
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var trId in frByTrId.Keys.Concat(testByTrId.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var parts = new List<string>();
            if (frByTrId.TryGetValue(trId, out var frIds) && frIds.Count > 0)
                parts.Add("FR: " + string.Join(", ", frIds));
            if (testByTrId.TryGetValue(trId, out var testIds) && testIds.Count > 0)
                parts.Add("TEST: " + string.Join(", ", testIds));
            if (parts.Count > 0)
                result[trId] = string.Join("; ", parts);
        }

        return result;
    }

    private static List<string> GetOrAdd(Dictionary<string, List<string>> valuesByKey, string key)
    {
        if (!valuesByKey.TryGetValue(key, out var values))
        {
            values = [];
            valuesByKey[key] = values;
        }

        return values;
    }

    private static void AddUnique(List<string> values, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var normalized = value.Trim();
        if (values.Any(existing => existing.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            return;

        values.Add(normalized);
    }

    /// <summary>
    /// FR-MCP-REQAC-001 / TR-MCP-REQAC-002: renders a deterministic "Acceptance Criteria"
    /// block under a requirement entry. Each criterion becomes a checklist-style bullet with
    /// optional evidence appended in parentheses. Emits nothing for null/empty lists.
    /// </summary>
    /// <param name="sb">Target builder.</param>
    /// <param name="criteria">Criteria to render; null or empty produces no output.</param>
    /// <param name="listItemIndent">Optional indent applied to nested lists (e.g. "  " when the parent entry is a list item).</param>
    internal static void AppendAcceptanceCriteria(StringBuilder sb, IReadOnlyList<AcceptanceCriterion>? criteria, string listItemIndent = "")
    {
        if (criteria is null || criteria.Count == 0)
            return;

        sb.Append(listItemIndent).AppendLine("**Acceptance Criteria:**");
        foreach (var criterion in criteria)
        {
            var marker = criterion.IsSatisfied ? "[x]" : "[ ]";
            sb.Append(listItemIndent).Append("- ").Append(marker).Append(' ').Append(criterion.Text);
            if (!string.IsNullOrWhiteSpace(criterion.Evidence))
                sb.Append(" (evidence: ").Append(criterion.Evidence.Trim()).Append(')');
            sb.AppendLine();
        }
    }

    /// <summary>Renders requirement scope metadata in a deterministic Markdown-friendly form.</summary>
    internal static void AppendScopeMetadata(StringBuilder sb, string scopeStartLayerKey, string? scopeEndLayerKey, string listItemIndent = "")
    {
        var start = string.IsNullOrWhiteSpace(scopeStartLayerKey) ? RequirementScopeLayerDefaults.DefaultLayerKey : scopeStartLayerKey.Trim();
        var end = string.IsNullOrWhiteSpace(scopeEndLayerKey) ? "+" : $"..{scopeEndLayerKey.Trim()}";
        sb.Append(listItemIndent).Append("Scope: ").Append(start).AppendLine(end);
    }

    public static string RenderMapping(IEnumerable<FrTrMapping> mappings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# TR per FR Mapping (MCP Server)");
        sb.AppendLine();
        sb.AppendLine("| FR | Primary TRs | Tests |");
        sb.AppendLine("| --- | --- | --- |");

        foreach (var mapping in mappings)
        {
            var trCell = mapping.TrIds is { Count: > 0 }
                ? string.Join(", ", mapping.TrIds)
                : "*(Planned)*";
            var testCell = mapping.TestIds is { Count: > 0 }
                ? string.Join(", ", mapping.TestIds)
                : "*(Planned)*";
            sb.Append("| ").Append(mapping.FrId).Append(" | ").Append(trCell).Append(" | ").Append(testCell).AppendLine(" |");
        }

        return sb.ToString();
    }

    public static string RenderMatrix(
        IEnumerable<FrEntry> functional,
        IEnumerable<TrEntry> technical,
        IEnumerable<TestEntry> testing,
        string? existingMatrixMarkdown = null)
    {
        var rows = new List<MatrixRequirementRow>();
        rows.AddRange(functional.Select(static entry => new MatrixRequirementRow(entry.Id, FunctionalFileName)));
        rows.AddRange(technical.Select(static entry => new MatrixRequirementRow(entry.Id, TechnicalFileName)));
        rows.AddRange(testing.Select(static entry => new MatrixRequirementRow(entry.Id, TestingFileName)));

        var sb = new StringBuilder();
        sb.AppendLine("# Requirements Matrix (MCP Server)");
        sb.AppendLine();
        sb.AppendLine("Traceability policy: see `Requirements-Traceability-Policy.md`.");
        sb.AppendLine();
        sb.AppendLine("| Requirement | Status | Source Files |");
        sb.AppendLine("| --- | --- | --- |");

        var coveredIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var existingRow in ReadExistingMatrixRows(existingMatrixMarkdown))
        {
            sb.AppendLine(existingRow.Line);
            foreach (var coveredId in existingRow.CoveredIds)
                coveredIds.Add(coveredId);
        }

        foreach (var row in rows)
        {
            if (!coveredIds.Add(row.Id))
                continue;

            AppendMatrixRow(sb, row.Id, row.SourceFile);
        }

        return sb.ToString();
    }

    private static IEnumerable<ExistingMatrixRow> ReadExistingMatrixRows(string? existingMatrixMarkdown)
    {
        if (string.IsNullOrWhiteSpace(existingMatrixMarkdown))
            yield break;

        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rawLine in existingMatrixMarkdown.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!rawLine.StartsWith('|'))
                continue;

            var cells = rawLine.Split('|', StringSplitOptions.TrimEntries);
            if (cells.Length < 3)
                continue;

            var requirementToken = cells[1];
            if (string.IsNullOrWhiteSpace(requirementToken)
                || requirementToken.Equals("Requirement", StringComparison.OrdinalIgnoreCase)
                || requirementToken.StartsWith("---", StringComparison.Ordinal))
            {
                continue;
            }

            var coveredIds = ExpandMatrixRequirementToken(requirementToken).ToArray();
            if (coveredIds.Length == 0 || !emitted.Add(rawLine))
                continue;

            yield return new ExistingMatrixRow(rawLine, coveredIds);
        }
    }

    private static IEnumerable<string> ExpandMatrixRequirementToken(string token)
    {
        var trimmed = token.Trim();
        var lastDash = Math.Max(trimmed.LastIndexOf('-'), trimmed.LastIndexOf('\u2013'));
        if (lastDash <= 0 || lastDash >= trimmed.Length - 1)
            return IsRequirementId(trimmed) ? [trimmed] : [];

        var prefix = trimmed[..(lastDash + 1)];
        var startText = prefix.Length >= 4 ? prefix[^4..^1] : string.Empty;
        var endText = trimmed[(lastDash + 1)..];
        if (!int.TryParse(startText, out var start)
            || !int.TryParse(endText, out var end)
            || end < start)
        {
            return IsRequirementId(trimmed) ? [trimmed] : [];
        }

        var idPrefix = prefix[..^4];
        return Enumerable.Range(start, end - start + 1)
            .Select(i => $"{idPrefix}{i:D3}");
    }

    private static bool IsRequirementId(string value) =>
        value.StartsWith("FR-", StringComparison.Ordinal)
        || value.StartsWith("TR-", StringComparison.Ordinal)
        || value.StartsWith("TEST-", StringComparison.Ordinal);

    private static void AppendMatrixRow(StringBuilder sb, string id, string sourceFile)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        sb.Append("| ")
            .Append(id.Trim())
            .Append(" | Tracked | ")
            .Append(sourceFile)
            .AppendLine(" |");
    }

    private sealed record MatrixRequirementRow(string Id, string SourceFile);

    private sealed record ExistingMatrixRow(string Line, IReadOnlyList<string> CoveredIds);
}
