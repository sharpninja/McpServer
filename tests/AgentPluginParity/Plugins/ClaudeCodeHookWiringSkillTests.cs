using System;
using System.Collections.Generic;
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
[Trait("Category", "Integration")]
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
            AssertInstalledHooksUseStableBridge(hooks);
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
            AssertInstalledHooksUseStableBridge(hooks);

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
    /// BUG-TRIAGE-014 / TEST-MCP-PLUGIN-PSONLY-001: the user-level sync-logs Stop
    /// hook must be stateful, allow no-op and already-reconciled transcript states,
    /// preserve the stop-hook recursion guard, and block once for fresh material tool
    /// activity that needs reconciliation.
    /// </summary>
    [Fact]
    public void ClaudeCode_SyncLogStopHook_BlocksOnlyUnreconciledMaterialTranscriptChanges()
    {
        var scriptPath = Path.Combine(
            PluginRoot,
            "skills",
            "claude-hook-wiring",
            "scripts",
            "sync-log-stop.ps1");
        Assert.True(File.Exists(scriptPath), $"Claude sync-log stop hook missing: {scriptPath}");

        var tempRoot = Path.Combine(Path.GetTempPath(), $"claude-sync-log-stop-{Guid.NewGuid():N}");
        var stateRoot = Path.Combine(tempRoot, "state");
        var transcriptPath = Path.Combine(tempRoot, "transcript.jsonl");
        Directory.CreateDirectory(tempRoot);
        try
        {
            File.WriteAllText(
                transcriptPath,
                """
                {"type":"assistant","message":{"content":[{"type":"text","text":"No tool work happened."}]}}

                """);

            var noOpOutput = RunSyncLogStopHook(scriptPath, stateRoot, transcriptPath, stopHookActive: false);
            Assert.True(string.IsNullOrWhiteSpace(noOpOutput), $"No-op transcript should not block. Output: {noOpOutput}");

            File.AppendAllText(
                transcriptPath,
                """
                {"type":"assistant","message":{"content":[{"type":"tool_use","name":"Edit","input":{"file_path":"src/App.cs"}}]}}

                """);

            var blockOutput = RunSyncLogStopHook(scriptPath, stateRoot, transcriptPath, stopHookActive: false);
            Assert.Contains("\"decision\":\"block\"", blockOutput, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sync-logs", blockOutput, StringComparison.OrdinalIgnoreCase);

            var activeOutput = RunSyncLogStopHook(scriptPath, stateRoot, transcriptPath, stopHookActive: true);
            Assert.True(string.IsNullOrWhiteSpace(activeOutput), $"stop_hook_active continuation should not block. Output: {activeOutput}");

            var reconciledOutput = RunSyncLogStopHook(scriptPath, stateRoot, transcriptPath, stopHookActive: false);
            Assert.True(string.IsNullOrWhiteSpace(reconciledOutput), $"Already-reconciled transcript should not block. Output: {reconciledOutput}");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// BUG-TRIAGE-021 / TEST-MCP-PLUGIN-PSONLY-001: active Claude settings must
    /// route through a stable bridge that can resolve the current plugin root even
    /// when an old cache root hint has been deleted.
    /// </summary>
    [Fact]
    public void ClaudeCode_HookBridge_UsesCurrentPluginRootWhenHintWasDeleted()
    {
        var bridgeSourcePath = Path.Combine(
            PluginRoot,
            "skills",
            "claude-hook-wiring",
            "scripts",
            "claude-mcp-hook-bridge.ps1");
        Assert.True(File.Exists(bridgeSourcePath), $"Claude hook bridge missing: {bridgeSourcePath}");

        var tempRoot = Path.Combine(Path.GetTempPath(), $"claude-hook-bridge-{Guid.NewGuid():N}");
        var stableHookRoot = Path.Combine(tempRoot, "stable-hooks");
        var currentPluginRoot = Path.Combine(tempRoot, "current-plugin");
        var stalePluginRoot = Path.Combine(tempRoot, "deleted-plugin");
        var fakeHookDirectory = Path.Combine(currentPluginRoot, "hooks", "scripts");
        Directory.CreateDirectory(stableHookRoot);
        Directory.CreateDirectory(fakeHookDirectory);
        Directory.CreateDirectory(stalePluginRoot);
        try
        {
            var bridgePath = Path.Combine(stableHookRoot, "claude-mcp-hook-bridge.ps1");
            File.Copy(bridgeSourcePath, bridgePath);
            File.WriteAllText(Path.Combine(stableHookRoot, "current-plugin-root.txt"), currentPluginRoot);
            File.WriteAllText(
                Path.Combine(fakeHookDirectory, "echo-hook.ps1"),
                """
                #Requires -Version 7.0
                [CmdletBinding()]
                param(
                    [Parameter(ValueFromRemainingArguments = $true)]
                    [string[]]$RemainingArguments
                )

                Write-Output 'bridge-ok'
                exit 0
                """);

            Directory.Delete(stalePluginRoot, recursive: true);

            var output = RunBridge(bridgePath, "echo-hook.ps1", stalePluginRoot);

            Assert.Contains("bridge-ok", output, StringComparison.OrdinalIgnoreCase);
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

    private static string RunBridge(string bridgePath, string scriptName, string pluginRootHint)
    {
        var startInfo = new ProcessStartInfo("pwsh")
        {
            WorkingDirectory = Path.GetDirectoryName(bridgePath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(bridgePath);
        startInfo.ArgumentList.Add("-ScriptName");
        startInfo.ArgumentList.Add(scriptName);
        startInfo.ArgumentList.Add("-PluginRootHint");
        startInfo.ArgumentList.Add(pluginRootHint);

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(30000), "Claude hook bridge timed out.");
        Assert.True(process.ExitCode == 0, $"Bridge failed with exit code {process.ExitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        return stdout;
    }

    private static string RunSyncLogStopHook(string scriptPath, string stateRoot, string transcriptPath, bool stopHookActive)
    {
        var startInfo = new ProcessStartInfo("pwsh")
        {
            WorkingDirectory = PluginRoot,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-StateRoot");
        startInfo.ArgumentList.Add(stateRoot);

        using var process = Process.Start(startInfo)!;
        var payload = new JsonObject
        {
            ["session_id"] = "ClaudeCode-test-session",
            ["transcript_path"] = transcriptPath,
            ["stop_hook_active"] = stopHookActive,
        };
        process.StandardInput.Write(payload.ToJsonString());
        process.StandardInput.Close();

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(30000), "Claude sync-log stop hook timed out.");
        Assert.True(process.ExitCode == 0, $"Sync-log stop hook failed with exit code {process.ExitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
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

    private static void AssertInstalledHooksUseStableBridge(JsonObject hooks)
    {
        foreach (var commandText in EnumerateHookCommands(hooks))
        {
            Assert.DoesNotContain("${CLAUDE_PLUGIN_ROOT}", commandText, StringComparison.OrdinalIgnoreCase);
        }

        AssertHookCommandWithFragments(hooks, "UserPromptSubmit", "claude-mcp-hook-bridge.ps1", "user-prompt-submit.ps1");
        AssertHookCommandWithFragments(hooks, "Stop", "claude-mcp-hook-bridge.ps1", "stop-gate.ps1");
        AssertHookCommandWithFragments(hooks, "PostToolUse", "claude-mcp-hook-bridge.ps1", "code-verify.ps1");
    }

    private static void AssertHookCommandWithFragments(JsonObject hooks, string eventName, params string[] fragments)
    {
        Assert.True(hooks.TryGetPropertyValue(eventName, out var eventNode), $"{eventName} hook missing.");
        foreach (var hookGroup in eventNode!.AsArray())
        {
            var commands = hookGroup?["hooks"]?.AsArray();
            if (commands is null)
                continue;

            foreach (var command in commands)
            {
                var commandText = command?["command"]?.GetValue<string>() ?? string.Empty;
                var containsAll = true;
                foreach (var fragment in fragments)
                {
                    if (!commandText.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                    {
                        containsAll = false;
                        break;
                    }
                }

                if (containsAll)
                    return;
            }
        }

        Assert.Fail($"{eventName} hook command did not contain all fragments: {string.Join(", ", fragments)}");
    }

    private static IEnumerable<string> EnumerateHookCommands(JsonObject hooks)
    {
        foreach (var hookEvent in hooks)
        {
            var hookGroups = hookEvent.Value?.AsArray();
            if (hookGroups is null)
                continue;

            foreach (var hookGroup in hookGroups)
            {
                var commands = hookGroup?["hooks"]?.AsArray();
                if (commands is null)
                    continue;

                foreach (var command in commands)
                {
                    var commandText = command?["command"]?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(commandText))
                        yield return commandText;
                }
            }
        }
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
