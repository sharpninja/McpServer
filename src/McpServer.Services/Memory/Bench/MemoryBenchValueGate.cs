using System.Text.Json;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-018-47 / FR-MCP-MEMORY-018-50: H7a Grok value-gate policy.
/// Other plugins and H7b stay blocked until an operator/hostile AGREE receipt is recorded.
/// </summary>
public static class MemoryBenchValueGate
{
    /// <summary>Committed policy file relative to the repository root.</summary>
    public const string RelativePath = "docs/benchmarks/h7a-value-gate.json";

    /// <summary>Loads the committed H7a policy document.</summary>
    public static MemoryBenchValueGateDocument Load(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var path = Path.Combine(repositoryRoot, RelativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException("H7a value-gate policy is missing.", path);

        var document = JsonSerializer.Deserialize<MemoryBenchValueGateDocument>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return document ?? throw new InvalidOperationException("H7a value-gate policy deserialized to null.");
    }

    /// <summary>
    /// True only when the committed policy file has agree=true.
    /// Tests and CI must not treat a missing file as agreed.
    /// </summary>
    public static bool IsH7aAgreed(string? repositoryRoot = null)
    {
        try
        {
            var root = repositoryRoot;
            if (string.IsNullOrWhiteSpace(root))
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory is not null)
                {
                    if (File.Exists(Path.Combine(directory.FullName, RelativePath)))
                    {
                        root = directory.FullName;
                        break;
                    }

                    directory = directory.Parent;
                }
            }

            if (string.IsNullOrWhiteSpace(root))
                return false;

            return Load(root).Agree;
        }
        catch (IOException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}

/// <summary>
/// FR-MCP-MEMORY-018-47: Serializable H7a policy.
/// </summary>
public sealed class MemoryBenchValueGateDocument
{
    /// <summary>Gate name.</summary>
    public string Gate { get; set; } = "H7a";

    /// <summary>True only after hostile/operator AGREE.</summary>
    public bool Agree { get; set; }

    /// <summary>H7b remains blocked until H7a AGREE.</summary>
    public bool H7bBlockedUntilH7aAgree { get; set; } = true;

    /// <summary>Default H-done requirement.</summary>
    public string HDoneDefaultRequires { get; set; } = "H7a";

    /// <summary>H7b is a follow-on slice unless the operator expands scope.</summary>
    public bool H7bFollowOn { get; set; } = true;

    /// <summary>Non-Grok CI cells are not required until AGREE.</summary>
    public bool NonGrokCiRequired { get; set; }

    /// <summary>Default BenchMemory plugin.</summary>
    public string DefaultPlugin { get; set; } = "grok";

    /// <summary>Hostile/operator H7a receipt path cited when agree is true.</summary>
    public string Receipt { get; set; } = string.Empty;

    /// <summary>Policy note, including the H7a receipt citation.</summary>
    public string Note { get; set; } = string.Empty;
}
