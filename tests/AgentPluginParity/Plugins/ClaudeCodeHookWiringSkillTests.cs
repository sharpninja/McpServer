using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using Xunit;

namespace McpServer.AgentPluginParity.Tests.Plugins;

/// <summary>
/// TEST-MCP-PLUGIN-PSONLY-001: Claude Code plugin must include a skill that wires
/// MCP enforcement hooks into the active Claude settings file without replacing
/// existing user hooks.
/// </summary>
public sealed class ClaudeCodeHookWiringSkillTests
{
    private static readonly string PluginRoot = ResolvePluginRoot();

    /// <summary>
    /// TEST-MCP-PLUGIN-PSONLY-001: the Claude hook wiring skill documents when to
    /// merge MCP hooks into active settings and how to verify the effective hooks.
    /// </summary>
    [Fact]
    public void ClaudeCode_HookWiringSkill_DocumentsActiveSettingsMerge()
    {
        var skillPath = Path.Combine(PluginRoot, "skills", "claude-hook-wiring", "SKILL.md");

        Assert.True(File.Exists(skillPath), $"Claude hook wiring skill missing: {skillPath}");
        var content = File.ReadAllText(skillPath);

        Assert.Contains("active Claude settings", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("settings.json", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hooks/hooks.json", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UserPromptSubmit", content, StringComparison.Ordinal);
        Assert.Contains("Stop", content, StringComparison.Ordinal);
        Assert.Contains("PostToolUse", content, StringComparison.Ordinal);
        Assert.Contains("do not replace", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("restart Claude Code", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-PLUGIN-PSONLY-001: the installer script merges MCP hooks into
    /// settings.json idempotently and preserves existing user hook entries.
    /// </summary>
    [Fact]
    public void ClaudeCode_HookWiringScript_MergesHooksIdempotently()
    {
        var scriptPath = Path.Combine(
            PluginRoot,
            "skills",
            "claude-hook-wiring",
            "scripts",
            "install-claude-mcp-hooks.ps1");
        Assert.True(File.Exists(scriptPath), $"Claude hook wiring installer missing: {scriptPath}");

        var tempRoot = Path.Combine(Path.GetTempPath(), $"claude-hook-wiring-{Guid.NewGuid():N}");
        var settingsPath = Path.Combine(tempRoot, "settings.json");
        Directory.CreateDirectory(tempRoot);
        try
        {
            File.WriteAllText(settingsPath, """
            {
              "hooks": {
                "UserPromptSubmit": [
                  {
                    "hooks": [
                      {
                        "type": "command",
                        "command": "pwsh -NoProfile -NonInteractive -Command \"Write-Output timestamp\"",
                        "timeout": 10
                      }
                    ]
                  }
                ]
              },
              "defaultShell": "powershell"
            }
            """);

            RunInstaller(scriptPath, settingsPath);
            RunInstaller(scriptPath, settingsPath);

            var settings = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();
            var hooks = settings["hooks"]!.AsObject();

            AssertHookCommandCount(hooks, "UserPromptSubmit", "timestamp", 1);
            AssertHookCommandCount(hooks, "UserPromptSubmit", "user-prompt-submit.ps1", 1);
            AssertHookCommandCount(hooks, "Stop", "stop-gate.ps1", 1);
            AssertHookCommandCount(hooks, "PostToolUse", "code-verify.ps1", 1);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// TEST-MCP-PLUGIN-PSONLY-001: the Claude hook validation skill is explicitly
    /// triggerable from the workspace marker and installs missing hooks through the
    /// hook-wiring skill.
    /// </summary>
    [Fact]
    public void ClaudeCode_HookValidationSkill_DocumentsMarkerTriggeredInstall()
    {
        var skillPath = Path.Combine(PluginRoot, "skills", "claude-hook-validation", "SKILL.md");

        Assert.True(File.Exists(skillPath), $"Claude hook validation skill missing: {skillPath}");
        var content = File.ReadAllText(skillPath);

        Assert.Contains("marker file", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AGENTS-README-FIRST.yaml", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("claude-hook-wiring", content, StringComparison.Ordinal);
        Assert.Contains("install-claude-mcp-hooks.ps1", content, StringComparison.Ordinal);
        Assert.Contains("UserPromptSubmit", content, StringComparison.Ordinal);
        Assert.Contains("Stop", content, StringComparison.Ordinal);
        Assert.Contains("PostToolUse", content, StringComparison.Ordinal);
        Assert.Contains("restart Claude Code", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-PLUGIN-PSONLY-001: the validation script repairs an active Claude
    /// settings file by invoking the hook wiring installer when MCP hooks are absent.
    /// </summary>
    [Fact]
    public void ClaudeCode_HookValidationScript_InstallsMissingHooks()
    {
        var scriptPath = Path.Combine(
            PluginRoot,
            "skills",
            "claude-hook-validation",
            "scripts",
            "validate-claude-mcp-hooks.ps1");
        Assert.True(File.Exists(scriptPath), $"Claude hook validation script missing: {scriptPath}");

        var tempRoot = Path.Combine(Path.GetTempPath(), $"claude-hook-validation-{Guid.NewGuid():N}");
        var settingsPath = Path.Combine(tempRoot, "settings.json");
        Directory.CreateDirectory(tempRoot);
        try
        {
            File.WriteAllText(settingsPath, """
            {
              "hooks": {
                "UserPromptSubmit": [
                  {
                    "hooks": [
                      {
                        "type": "command",
                        "command": "pwsh -NoProfile -NonInteractive -Command \"Write-Output timestamp\"",
                        "timeout": 10
                      }
                    ]
                  }
                ]
              }
            }
            """);

            var firstOutput = RunInstaller(scriptPath, settingsPath);
            Assert.Contains("\"status\":\"installed\"", firstOutput, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"restartRequired\":true", firstOutput, StringComparison.OrdinalIgnoreCase);

            var settings = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();
            var hooks = settings["hooks"]!.AsObject();
            AssertHookCommandCount(hooks, "UserPromptSubmit", "timestamp", 1);
            AssertHookCommandCount(hooks, "UserPromptSubmit", "user-prompt-submit.ps1", 1);
            AssertHookCommandCount(hooks, "Stop", "stop-gate.ps1", 1);
            AssertHookCommandCount(hooks, "PostToolUse", "code-verify.ps1", 1);

            var secondOutput = RunInstaller(scriptPath, settingsPath);
            Assert.Contains("\"status\":\"valid\"", secondOutput, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"restartRequired\":false", secondOutput, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// TEST-MCP-PLUGIN-PSONLY-001: the rendered marker template tells Claude Code
    /// to trigger the hook validation skill during plugin bootstrap.
    /// </summary>
    [Fact]
    public void MarkerTemplate_TriggersClaudeHookValidationSkill()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var templatePath = Path.Combine(repoRoot, "templates", "prompt-templates.yaml");

        var content = File.ReadAllText(templatePath);

        Assert.Contains("claude-hook-validation", content, StringComparison.Ordinal);
        Assert.Contains("active Claude settings", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("install-claude-mcp-hooks.ps1", content, StringComparison.Ordinal);
        Assert.Contains("UserPromptSubmit", content, StringComparison.Ordinal);
        Assert.Contains("Stop", content, StringComparison.Ordinal);
        Assert.Contains("PostToolUse", content, StringComparison.Ordinal);
    }

    private static string RunInstaller(string scriptPath, string settingsPath)
    {
        var startInfo = new ProcessStartInfo("pwsh")
        {
            WorkingDirectory = PluginRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-SettingsPath");
        startInfo.ArgumentList.Add(settingsPath);
        startInfo.ArgumentList.Add("-PluginRoot");
        startInfo.ArgumentList.Add(PluginRoot);
        startInfo.ArgumentList.Add("-NoBackup");

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(30000), "Claude hook wiring installer timed out.");
        Assert.True(process.ExitCode == 0, $"Installer failed with exit code {process.ExitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        return stdout;
    }

    private static void AssertHookCommandCount(JsonObject hooks, string eventName, string commandFragment, int expectedCount)
    {
        Assert.True(hooks.TryGetPropertyValue(eventName, out var eventNode), $"{eventName} hook missing.");
        var count = 0;
        foreach (var hookGroup in eventNode!.AsArray())
        {
            var commands = hookGroup?["hooks"]?.AsArray();
            if (commands is null)
                continue;

            foreach (var command in commands)
            {
                var commandText = command?["command"]?.GetValue<string>() ?? string.Empty;
                if (commandText.Contains(commandFragment, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
        }

        Assert.Equal(expectedCount, count);
    }

    private static string ResolvePluginRoot()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var sibling = Path.GetFullPath(Path.Combine(repoRoot, "..", "mcpserver-claude-code-plugin"));
        if (Directory.Exists(sibling))
            return sibling;

        var envRoot = Environment.GetEnvironmentVariable("CLAUDE_PLUGIN_ROOT");
        if (!string.IsNullOrWhiteSpace(envRoot) && Directory.Exists(envRoot))
            return envRoot;

        return sibling;
    }
}
