namespace McpServer.QBAgent.Skills;

/// <summary>
/// FR-MCP-QBSKILLS-001 / TR-MCP-QBSKILLS-001: Parses an agentskills.io <c>SKILL.md</c> file: a YAML frontmatter
/// block delimited by <c>---</c> lines (requiring <c>name</c> and <c>description</c>; optional <c>license</c> and
/// <c>allowed-tools</c>) followed by the markdown instruction body.
/// </summary>
public interface ISkillManifestParser
{
    /// <summary>Attempts to parse a SKILL.md document.</summary>
    /// <param name="content">The full file content.</param>
    /// <param name="path">The file path (recorded on the manifest).</param>
    /// <param name="manifest">The parsed manifest when successful.</param>
    /// <param name="error">The reason parsing failed, when unsuccessful.</param>
    /// <returns><see langword="true"/> when the document parsed into a valid manifest.</returns>
    bool TryParse(string content, string path, out SkillManifest? manifest, out string? error);
}

/// <summary>FR-MCP-QBSKILLS-001: Default <see cref="ISkillManifestParser"/> using a minimal frontmatter reader.</summary>
public sealed class SkillManifestParser : ISkillManifestParser
{
    /// <inheritdoc />
    public bool TryParse(string content, string path, out SkillManifest? manifest, out string? error)
    {
        manifest = null;
        error = null;

        if (string.IsNullOrWhiteSpace(content))
        {
            error = "skill file is empty";
            return false;
        }

        var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            error = "missing YAML frontmatter (the file must begin with a '---' line)";
            return false;
        }

        var closing = normalized.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (closing < 0)
        {
            error = "unterminated YAML frontmatter (no closing '---' line)";
            return false;
        }

        var frontmatter = normalized[4..closing];
        var body = normalized[(closing + 4)..].TrimStart('\n');

        string? name = null;
        string? description = null;
        string? license = null;
        var allowedTools = new List<string>();

        foreach (var rawLine in frontmatter.Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0 || line[0] == '#' || char.IsWhiteSpace(line[0]))
                continue;

            var separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = Unquote(line[(separator + 1)..].Trim());

            switch (key.ToLowerInvariant())
            {
                case "name":
                    name = value;
                    break;
                case "description":
                    description = value;
                    break;
                case "license":
                    license = value;
                    break;
                case "allowed-tools":
                case "allowedtools":
                    allowedTools.AddRange(SplitList(value));
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "frontmatter is missing the required 'name' field";
            return false;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            error = "frontmatter is missing the required 'description' field";
            return false;
        }

        manifest = new SkillManifest(name.Trim(), description.Trim(), license, allowedTools, body, path);
        return true;
    }

    private static IEnumerable<string> SplitList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var trimmed = value.Trim();
        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            trimmed = trimmed[1..^1];

        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Unquote);
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
