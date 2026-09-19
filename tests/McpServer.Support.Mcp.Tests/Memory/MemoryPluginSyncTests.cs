using System.Text.Json;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-017:
/// Grok plugin is the S5 pilot; other plugins are required after H7a;
/// missing required verbs fail the checklist guard.
/// </summary>
public sealed class MemoryPluginSyncTests
{
    /// <summary>AC-FR-MCP-MEMORY-017-03: Plugin sync order is grok first; others after the value gate.</summary>
    [Fact]
    public void GrokFirst_ThenOthersAfterValueGate()
    {
        var checklist = ReadChecklist();
        Assert.Equal("grok", checklist.RequiredNow[0]);
        Assert.Equal(MemoryS5Catalog.AllPlugins, checklist.RequiredNow);
        Assert.Empty(checklist.DeferredUntilH7a);
        Assert.Equal("grok", checklist.Rows[0].Plugin);
        Assert.Equal(8, checklist.Rows.Count);
    }

    /// <summary>AC-FR-MCP-MEMORY-017-08: Plugin validation fails when a synced skill omits a required new verb.</summary>
    [Fact]
    public void MissingVerb_FailsValidation()
    {
        var validatorType = typeof(McpServer.Support.Mcp.Services.MemoryLimits).Assembly.GetTypes()
            .FirstOrDefault(type => type.Name == "MemoryPluginVerbChecklist");
        Assert.NotNull(validatorType);
        var validate = validatorType!.GetMethod("Validate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(validate);
        var missing = validate!.Invoke(null, ["skill mentions memory_add only", MemoryS5Catalog.NewVerbs]) as IReadOnlyList<string>;
        Assert.NotNull(missing);
        Assert.Contains("memory_remember", missing!);
        Assert.NotEmpty(missing);

        var grokSkill = File.ReadAllText(GrokSkillPath());
        var grokMissing = validate.Invoke(null, [grokSkill, MemoryS5Catalog.NewVerbs]) as IReadOnlyList<string>;
        Assert.NotNull(grokMissing);
        Assert.Empty(grokMissing!);
    }

    /// <summary>AC-FR-MCP-MEMORY-017-12: S5 checklist has an explicit grok pass/fail row before other plugins.</summary>
    [Fact]
    public void ChecklistReceipt_GrokBeforeOthers()
    {
        var checklist = ReadChecklist();
        Assert.Equal("S7b", checklist.Slice);
        var grok = Assert.Single(checklist.Rows, row => row.Plugin == "grok");
        Assert.True(grok.Required);
        Assert.Equal("pass", grok.Status, ignoreCase: true);
        Assert.True(checklist.Rows.FindIndex(row => row.Plugin == "grok") == 0);
        Assert.Empty(checklist.DeferredUntilH7a);
        Assert.Equal(8, checklist.Rows.Count);
        Assert.All(checklist.Rows, row =>
        {
            Assert.True(row.Required);
            Assert.Equal("pass", row.Status, ignoreCase: true);
        });
    }

    /// <summary>AC-FR-MCP-MEMORY-017-13: Grok plugin skill/descriptor/shim for new verbs exists before other plugin sync.</summary>
    [Fact]
    public void ImplementGrokPlugin_BeforeOthers()
    {
        var root = MemoryS5Catalog.FindRepoRoot();
        var skill = GrokSkillPath();
        var descriptor = Path.Combine(root, "plugins", "core", "hosts", "grok", "memory-descriptor.json");
        var shim = Path.Combine(root, "plugins", "core", "lib-node", "src", "tools", "memory.ts");
        Assert.True(File.Exists(skill), "Grok memory skill is required.");
        Assert.True(File.Exists(descriptor), "Grok memory descriptor is required.");
        Assert.True(File.Exists(shim), "Shared memory shim is required.");

        var skillText = File.ReadAllText(skill);
        var descriptorText = File.ReadAllText(descriptor);
        var shimText = File.ReadAllText(shim);
        foreach (var verb in MemoryS5Catalog.NewVerbs)
        {
            Assert.Contains(verb, skillText, StringComparison.Ordinal);
            Assert.Contains(verb, descriptorText, StringComparison.Ordinal);
            Assert.Contains(verb, shimText, StringComparison.Ordinal);
        }

        Assert.Contains("injection", skillText + descriptorText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fallback", skillText + descriptorText, StringComparison.OrdinalIgnoreCase);

        foreach (var plugin in MemoryS5Catalog.DeferredPlugins)
        {
            var hostDescriptor = Path.Combine(root, "plugins", "core", "hosts", plugin, "memory-descriptor.json");
            var hostSkill = Path.Combine(root, "plugins", "core", "hosts", plugin, "SKILL.md");
            Assert.True(File.Exists(hostDescriptor), plugin + " memory descriptor is required after H7a.");
            Assert.True(File.Exists(hostSkill), plugin + " memory skill is required after H7a.");
            var hostDescriptorText = File.ReadAllText(hostDescriptor);
            var hostSkillText = File.ReadAllText(hostSkill);
            foreach (var verb in MemoryS5Catalog.NewVerbs)
            {
                Assert.Contains(verb, hostDescriptorText, StringComparison.Ordinal);
                Assert.Contains(verb, hostSkillText, StringComparison.Ordinal);
            }

            Assert.Contains("injection", hostSkillText + hostDescriptorText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("fallback", hostSkillText + hostDescriptorText, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>AC-FR-MCP-MEMORY-017-14: Non-Grok plugin sync is documented as blocked until H7a.</summary>
    [Fact]
    public void NonGrok_BlockedUntilH7a()
    {
        var checklist = ReadChecklist();
        Assert.Contains("H7a", checklist.DeferralNote, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("agree:true", checklist.DeferralNote, StringComparison.Ordinal);
        Assert.Contains("hostile-validator-20260919T081530Z.md", checklist.DeferralNote, StringComparison.Ordinal);
        Assert.True(MemoryBenchValueGate.IsH7aAgreed(MemoryS5Catalog.FindRepoRoot()));
        foreach (var plugin in MemoryS5Catalog.DeferredPlugins)
        {
            Assert.DoesNotContain(plugin, checklist.DeferredUntilH7a);
            Assert.Contains(checklist.Rows, row => row.Plugin == plugin && row.Required && row.Status == "pass");
        }
    }

    private static string GrokSkillPath()
        => Path.Combine(MemoryS5Catalog.FindRepoRoot(), "plugins", "core", "skills", "memory", "SKILL.md");

    private static PluginChecklist ReadChecklist()
    {
        var path = Path.Combine(
            MemoryS5Catalog.FindRepoRoot(),
            "docs",
            "plugins",
            "mcp-memory-002-s5-plugin-checklist.json");
        Assert.True(File.Exists(path), "S5 plugin checklist JSON is required.");
        var parsed = JsonSerializer.Deserialize<PluginChecklist>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(parsed);
        return parsed!;
    }

    private sealed class PluginChecklist
    {
        public string Slice { get; set; } = string.Empty;
        public List<string> RequiredNow { get; set; } = [];
        public List<string> DeferredUntilH7a { get; set; } = [];
        public List<PluginChecklistRow> Rows { get; set; } = [];
        public string DeferralNote { get; set; } = string.Empty;
    }

    private sealed class PluginChecklistRow
    {
        public string Plugin { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
