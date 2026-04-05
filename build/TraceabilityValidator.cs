using System.Text.RegularExpressions;

/// <summary>
/// Validates requirements traceability between FR/TR/TEST documents and the mapping/matrix files.
/// Ported from scripts/Validate-RequirementsTraceability.ps1.
/// </summary>
static partial class TraceabilityValidator
{
    [GeneratedRegex(@"^##\s+(FR-[A-Z0-9-]+-\d{3})\b")]
    private static partial Regex FrHeadingRegex();

    [GeneratedRegex(@"^##\s+(TR-[A-Z0-9-]+-\d{3})\b")]
    private static partial Regex TrHeadingRegex();

    [GeneratedRegex(@"\b(TEST-[A-Z]+-\d{3})\b")]
    private static partial Regex TestIdRegex();

    [GeneratedRegex(@"^\|\s*(FR-[A-Z0-9-]+-\d{3})")]
    private static partial Regex MappingFrRegex();

    [GeneratedRegex(@"^\|\s*((?:FR|TR|TEST)-[A-Z0-9-]+-\d{3}(?:[–-]\d{3})?)")]
    private static partial Regex MatrixIdRegex();

    [GeneratedRegex(@"^([A-Z]+(?:-[A-Z0-9]+)+-)(\d{3})[–-](\d{3})$")]
    private static partial Regex RangeTokenRegex();

    /// <summary>Extracts requirement IDs from heading lines matching a given prefix regex.</summary>
    public static List<string> GetIdsFromHeadings(string[] lines, Regex pattern)
    {
        var ids = new List<string>();
        foreach (var line in lines)
        {
            var match = pattern.Match(line);
            if (match.Success)
                ids.Add(match.Groups[1].Value);
        }
        return ids;
    }

    /// <summary>Extracts all TEST-* IDs from content lines.</summary>
    public static HashSet<string> GetTestIds(string[] lines)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            foreach (Match match in TestIdRegex().Matches(line))
                ids.Add(match.Groups[1].Value);
        }
        return ids;
    }

    /// <summary>Extracts FR IDs from the TR-per-FR mapping file.</summary>
    public static List<string> GetMappingFrIds(string[] lines)
    {
        var ids = new List<string>();
        foreach (var line in lines)
        {
            var match = MappingFrRegex().Match(line);
            if (match.Success)
                ids.Add(match.Groups[1].Value);
        }
        return ids;
    }

    /// <summary>Extracts requirement IDs from the matrix file, expanding range tokens.</summary>
    public static HashSet<string> GetMatrixIds(string[] lines)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var match = MatrixIdRegex().Match(line);
            if (!match.Success) continue;

            foreach (var expanded in ExpandRangeToken(match.Groups[1].Value))
                ids.Add(expanded);
        }
        return ids;
    }

    /// <summary>Expands a range token like FR-MCP-001-003 into individual IDs.</summary>
    public static IEnumerable<string> ExpandRangeToken(string token)
    {
        var match = RangeTokenRegex().Match(token);
        if (!match.Success)
            return [token];

        var prefix = match.Groups[1].Value;
        var start = int.Parse(match.Groups[2].Value);
        var end = int.Parse(match.Groups[3].Value);

        if (end < start)
            return [token];

        return Enumerable.Range(start, end - start + 1)
            .Select(i => $"{prefix}{i:D3}");
    }

    /// <summary>Result of traceability validation.</summary>
    public sealed class ValidationResult
    {
        public List<string> MissingFrInMapping { get; init; } = [];
        public List<string> MissingFrInMatrix { get; init; } = [];
        public List<string> MissingTrInMatrix { get; init; } = [];
        public List<string> MissingTestInMatrix { get; init; } = [];

        public bool HasFrErrors => MissingFrInMapping.Count > 0 || MissingFrInMatrix.Count > 0;
        public bool HasTrErrors => MissingTrInMatrix.Count > 0;
        public bool HasTestErrors => MissingTestInMatrix.Count > 0;
    }

    /// <summary>
    /// Validates traceability across all requirements documents.
    /// </summary>
    public static ValidationResult Validate(
        string[] functionalLines,
        string[] technicalLines,
        string[] testingLines,
        string[] mappingLines,
        string[] matrixLines)
    {
        var frIds = GetIdsFromHeadings(functionalLines, FrHeadingRegex());
        var trIds = GetIdsFromHeadings(technicalLines, TrHeadingRegex());
        var testIds = GetTestIds(testingLines);
        var mappingFr = GetMappingFrIds(mappingLines);
        var matrixIds = GetMatrixIds(matrixLines);

        return new ValidationResult
        {
            MissingFrInMapping = frIds.Where(id => !mappingFr.Contains(id)).ToList(),
            MissingFrInMatrix = frIds.Where(id => !matrixIds.Contains(id)).ToList(),
            MissingTrInMatrix = trIds.Where(id => !matrixIds.Contains(id)).ToList(),
            MissingTestInMatrix = testIds.Where(id => !matrixIds.Contains(id)).ToList(),
        };
    }
}
