using System.Text;
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
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string RenderTechnical(IEnumerable<TrEntry> entries)
    {
        var sb = new StringBuilder();
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
            sb.Append("- ").Append(entry.Id).Append(": ").AppendLine(entry.Condition.Trim());

        if (sb.Length > 0 && sb[^1] != '\n')
            sb.AppendLine();

        return sb.ToString();
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
