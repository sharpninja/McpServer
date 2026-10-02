using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TEST-MCP-QBCLI-001: Invokes Grok Build CLI or Codex CLI for a QuadBrain slot and reuses the
/// CLI conversation id across turns (Grok <c>--resume</c>, Codex <c>exec resume</c>).
/// </summary>
internal sealed class CliBrainSlotChatClient(
    IProcessSpawner processSpawner,
    IProcessEnvironmentService processEnvironment,
    CliBrainSlotSessionStore sessionStore,
    IOptionsMonitor<BrainSlotOptions> brainSlotOptions,
    ILogger logger) : IBrainSlotChatClient
{
    /// <inheritdoc />
    public async Task<string> CompleteAsync(
        BrainSlotDefinitionEntity slot,
        string input,
        double? temperature,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (!CliBrainSlotEndpoint.TryParse(slot.Endpoint, out var strategy))
            throw new InvalidOperationException($"Cli brain slot '{slot.SlotId}' has an invalid endpoint.");

        var options = brainSlotOptions.CurrentValue;
        var workingDirectory = string.IsNullOrWhiteSpace(options.CliWorkingDirectory)
            ? Environment.CurrentDirectory
            : options.CliWorkingDirectory;
        var prompt = BuildPrompt(slot, input);
        var tempDirectory = ResolveTempDirectory();

        if (string.Equals(strategy, AgentExecutionStrategyNames.GrokCli, StringComparison.OrdinalIgnoreCase))
            return await CompleteGrokAsync(slot, prompt, tempDirectory, options, cancellationToken).ConfigureAwait(false);

        return await CompleteCodexAsync(slot, prompt, workingDirectory, tempDirectory, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads a Codex JSONL stream for a thread/session id.</summary>
    internal static string? ExtractCodexSessionId(string? jsonl)
    {
        if (string.IsNullOrWhiteSpace(jsonl))
            return null;

        foreach (var line in jsonl.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] != '{')
                continue;

            try
            {
                using var document = JsonDocument.Parse(trimmed);
                if (TryReadId(document.RootElement, out var id))
                    return id;
            }
            catch (JsonException)
            {
                // Skip non-JSON log lines.
            }
        }

        return null;
    }

    /// <summary>
    /// Reads assistant text from a Codex <c>--json</c> JSONL stream. Raw JSONL is not a usable role result.
    /// </summary>
    internal static string? ExtractCodexAssistantText(string? jsonl)
    {
        if (string.IsNullOrWhiteSpace(jsonl))
            return null;

        var parts = new List<string>();
        foreach (var line in jsonl.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] != '{')
                continue;

            try
            {
                using var document = JsonDocument.Parse(trimmed);
                if (TryReadAssistantText(document.RootElement, out var text)
                    && !string.IsNullOrWhiteSpace(text))
                {
                    parts.Add(text.Trim());
                }
            }
            catch (JsonException)
            {
            }
        }

        return parts.Count == 0 ? null : string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    private static bool LooksLikeJsonl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;
            return trimmed[0] == '{';
        }

        return false;
    }

    private static bool TryReadAssistantText(JsonElement element, out string text)
    {
        text = string.Empty;
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        var type = element.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String
            ? typeEl.GetString()
            : null;

        if (string.Equals(type, "item.completed", StringComparison.OrdinalIgnoreCase)
            && element.TryGetProperty("item", out var item))
        {
            return TryReadItemAssistantText(item, out text);
        }

        if (string.Equals(type, "event_msg", StringComparison.OrdinalIgnoreCase)
            && element.TryGetProperty("payload", out var payload)
            && payload.ValueKind == JsonValueKind.Object)
        {
            var payloadType = payload.TryGetProperty("type", out var payloadTypeEl) && payloadTypeEl.ValueKind == JsonValueKind.String
                ? payloadTypeEl.GetString()
                : null;
            if (string.Equals(payloadType, "agent_message", StringComparison.OrdinalIgnoreCase))
                return TryReadStringField(payload, ["message", "text"], out text);
        }

        if (string.Equals(type, "agent_message", StringComparison.OrdinalIgnoreCase))
            return TryReadStringField(element, ["text", "message"], out text);

        if (string.Equals(type, "message", StringComparison.OrdinalIgnoreCase))
        {
            var role = element.TryGetProperty("role", out var roleEl) && roleEl.ValueKind == JsonValueKind.String
                ? roleEl.GetString()
                : null;
            if (role is null || string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase))
                return TryReadContent(element, out text);
        }

        return false;
    }

    private static bool TryReadItemAssistantText(JsonElement item, out string text)
    {
        text = string.Empty;
        var itemType = item.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String
            ? typeEl.GetString()
            : null;
        if (itemType is not null
            && !string.Equals(itemType, "agent_message", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(itemType, "message", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return TryReadStringField(item, ["text", "message"], out text) || TryReadContent(item, out text);
    }

    private static bool TryReadContent(JsonElement element, out string text)
    {
        text = string.Empty;
        if (!element.TryGetProperty("content", out var content))
            return false;

        if (content.ValueKind == JsonValueKind.String)
        {
            text = content.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(text);
        }

        if (content.ValueKind != JsonValueKind.Array)
            return false;

        var parts = new List<string>();
        foreach (var part in content.EnumerateArray())
        {
            if (part.ValueKind == JsonValueKind.String)
            {
                var value = part.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    parts.Add(value);
                continue;
            }

            if (part.ValueKind == JsonValueKind.Object
                && TryReadStringField(part, ["text", "message"], out var nested)
                && !string.IsNullOrWhiteSpace(nested))
            {
                parts.Add(nested);
            }
        }

        if (parts.Count == 0)
            return false;

        text = string.Join(Environment.NewLine, parts);
        return true;
    }

    private static bool TryReadStringField(JsonElement element, string[] names, out string text)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property)
                && property.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(property.GetString()))
            {
                text = property.GetString()!;
                return true;
            }
        }

        text = string.Empty;
        return false;
    }

    private async Task<string> CompleteGrokAsync(
        BrainSlotDefinitionEntity slot,
        string prompt,
        string tempDirectory,
        BrainSlotOptions options,
        CancellationToken cancellationToken)
    {
        var resume = sessionStore.TryGet(slot.SlotId, out var sessionId);
        sessionId ??= Guid.NewGuid().ToString();
        var grokCwd = Path.Combine(tempDirectory, "grok-work");
        Directory.CreateDirectory(grokCwd);
        var promptFilePath = Path.Combine(tempDirectory, $"grok-prompt-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(promptFilePath, prompt, cancellationToken).ConfigureAwait(false);
        try
        {
            var (exitCode, stdout, stderr) = await LaunchGrokAsync(
                    slot,
                    grokCwd,
                    promptFilePath,
                    sessionId,
                    resume,
                    tempDirectory,
                    options,
                    cancellationToken)
                .ConfigureAwait(false);

            if (exitCode != 0 && resume && IsGrokSessionMissing(stderr, stdout))
            {
                sessionStore.Remove(slot.SlotId);
                sessionId = Guid.NewGuid().ToString();
                resume = false;
                (exitCode, stdout, stderr) = await LaunchGrokAsync(
                        slot,
                        grokCwd,
                        promptFilePath,
                        sessionId,
                        resume,
                        tempDirectory,
                        options,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (exitCode != 0 && !IsGrokMaxTurnsReached(stderr, stdout))
                throw new InvalidOperationException($"Grok CLI failed with exit {exitCode}: {FirstNonEmpty(stderr, stdout)}");

            sessionStore.Set(slot.SlotId, sessionId);
            return stdout.Trim();
        }
        finally
        {
            TryDelete(promptFilePath);
        }
    }

    private async Task<string> CompleteCodexAsync(
        BrainSlotDefinitionEntity slot,
        string prompt,
        string workingDirectory,
        string tempDirectory,
        BrainSlotOptions options,
        CancellationToken cancellationToken)
    {
        sessionStore.TryGet(slot.SlotId, out var sessionId);
        var outputPath = Path.Combine(tempDirectory, $"codex-out-{Guid.NewGuid():N}.txt");
        var psi = CreateStartInfo("codex", workingDirectory);
        foreach (var argument in CodexCliAgentExecutionStrategy.BuildPersistentCodexArgumentList(
                     workingDirectory,
                     outputPath,
                     slot.ModelId,
                     sessionId))
        {
            psi.ArgumentList.Add(argument);
        }

        processEnvironment.ApplyAll(psi, options.CliRunAs, options.CliGitHubToken);
        psi.FileName = processEnvironment.ResolveExecutable(psi, psi.FileName);
        ApplySharedTempEnvironment(psi, tempDirectory);
        WrapWindowsCommandShim(psi);

        var (exitCode, stdout, stderr) = await RunAsync(psi, prompt, cancellationToken).ConfigureAwait(false);
        var body = File.Exists(outputPath)
            ? await File.ReadAllTextAsync(outputPath, cancellationToken).ConfigureAwait(false)
            : stdout;
        TryDelete(outputPath);
        if (exitCode != 0)
            throw new InvalidOperationException($"Codex CLI failed with exit {exitCode}: {FirstNonEmpty(stderr, stdout, body)}");

        var extracted = ExtractCodexSessionId(stdout) ?? ExtractCodexSessionId(body);
        if (!string.IsNullOrWhiteSpace(extracted))
            sessionStore.Set(slot.SlotId, extracted);

        var assistant = ExtractCodexAssistantText(stdout) ?? ExtractCodexAssistantText(body);
        if (!string.IsNullOrWhiteSpace(assistant))
            return assistant.Trim();

        if (!LooksLikeJsonl(body) && !string.IsNullOrWhiteSpace(body))
            return body.Trim();
        if (!LooksLikeJsonl(stdout) && !string.IsNullOrWhiteSpace(stdout))
            return stdout.Trim();

        return (body.Length > 0 ? body : stdout).Trim();
    }

    private async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        ProcessStartInfo psi,
        string? stdin,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Launching QuadBrain CLI: {File} {Args}", psi.FileName, string.Join(' ', psi.ArgumentList));
        var process = processSpawner.Spawn(psi);
        try
        {
            if (stdin is not null && process.StandardInput is not null)
            {
                try
                {
                    await process.StandardInput.WriteAsync(stdin.AsMemory(), cancellationToken).ConfigureAwait(false);
                    await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
                    process.StandardInput.Close();
                }
                catch (IOException)
                {
                    // Child closed stdin (invalid args or early exit). Capture stdout/stderr below.
                }
            }
            else
            {
                process.StandardInput?.Close();
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            return (process.ExitCode, stdout, stderr);
        }
        finally
        {
            process.Dispose();
        }
    }

    private static ProcessStartInfo CreateStartInfo(string fileName, string workingDirectory)
        => new()
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

    private static string BuildPrompt(BrainSlotDefinitionEntity slot, string input)
    {
        if (string.IsNullOrWhiteSpace(slot.SystemPrompt))
            return input;

        return slot.SystemPrompt.Trim() + Environment.NewLine + Environment.NewLine + input;
    }

    /// <summary>
    /// Prompt and CLI scratch files must be readable by <see cref="BrainSlotOptions.CliRunAs"/>.
    /// LocalSystem <see cref="Path.GetTempPath"/> is <c>C:\WINDOWS\SystemTemp</c>, which that
    /// profile cannot read. Prefer ProgramData with BuiltinUsers Modify, matching Grok/Codex oneshot.
    /// </summary>
    internal static string ResolveTempDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("MCPSERVER_QUADBRAIN_CLI_TEMP")
                         ?? Environment.GetEnvironmentVariable("MCPSERVER_ONESHOT_TEMP");
        var hasConfiguredDirectory = !string.IsNullOrWhiteSpace(configured);
        var preferredDirectory = hasConfiguredDirectory
            ? configured!
            : DefaultSharedTempDirectory();

        try
        {
            EnsureSharedTempDirectory(preferredDirectory);
            return preferredDirectory;
        }
        catch (Exception ex) when (!hasConfiguredDirectory && OperatingSystem.IsWindows() &&
                                   (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException))
        {
            var fallbackDirectory = Path.Combine(Path.GetTempPath(), "mcpserver-quadbrain-cli");
            Directory.CreateDirectory(fallbackDirectory);
            return fallbackDirectory;
        }
    }

    /// <summary>
    /// Production default under CommonApplicationData. Unit tests must not require this directory
    /// to be writable; they set <c>MCPSERVER_QUADBRAIN_CLI_TEMP</c> instead of assuming the Windows service ACL.
    /// </summary>
    internal static string DefaultSharedTempDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "McpServer",
                "Temp",
                "quadbrain-cli");
        }

        return Path.Combine(Path.GetTempPath(), "mcpserver-quadbrain-cli");
    }

    private static void EnsureSharedTempDirectory(string path)
    {
        Directory.CreateDirectory(path);
        if (OperatingSystem.IsWindows())
            EnsureWindowsUsersCanModify(path);
    }

    [SupportedOSPlatform("windows")]
    private static void EnsureWindowsUsersCanModify(string path)
    {
        var directoryInfo = new DirectoryInfo(path);
        var security = directoryInfo.GetAccessControl();
        security.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
            FileSystemRights.Modify | FileSystemRights.Synchronize,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));
        directoryInfo.SetAccessControl(security);
    }

    private static void ApplySharedTempEnvironment(ProcessStartInfo psi, string tempDirectory)
    {
        psi.Environment["TEMP"] = tempDirectory;
        psi.Environment["TMP"] = tempDirectory;
    }

    private async Task<(int ExitCode, string Stdout, string Stderr)> LaunchGrokAsync(
        BrainSlotDefinitionEntity slot,
        string grokCwd,
        string promptFilePath,
        string sessionId,
        bool resume,
        string tempDirectory,
        BrainSlotOptions options,
        CancellationToken cancellationToken)
    {
        var psi = CreateStartInfo(
            GrokCliAgentExecutionStrategy.ResolveGrokExecutable(null),
            grokCwd);
        foreach (var argument in GrokCliAgentExecutionStrategy.BuildPersistentGrokArgumentList(
                     grokCwd,
                     promptFilePath,
                     slot.ModelId,
                     sessionId,
                     resume))
        {
            psi.ArgumentList.Add(argument);
        }

        processEnvironment.ApplyAll(psi, options.CliRunAs, options.CliGitHubToken);
        psi.FileName = processEnvironment.ResolveExecutable(psi, psi.FileName);
        ApplySharedTempEnvironment(psi, tempDirectory);
        DisableGrokPluginEnvironment(psi);
        WrapWindowsCommandShim(psi);
        return await RunAsync(psi, stdin: null, cancellationToken).ConfigureAwait(false);
    }

    private static void DisableGrokPluginEnvironment(ProcessStartInfo psi)
    {
        psi.Environment.Remove("GROK_PLUGIN_ROOT");
        psi.Environment.Remove("GROK_HOME");
        psi.Environment["GROK_AGENT_DASHBOARD"] = "0";
        psi.Environment["GROK_MEMORY"] = "0";
        psi.Environment["GROK_SUBAGENTS"] = "0";
    }

    private static bool IsGrokMaxTurnsReached(string stderr, string stdout)
        => ContainsOrdinal(stderr, "Max turns reached") || ContainsOrdinal(stdout, "Max turns reached");

    private static bool IsGrokSessionMissing(string stderr, string stdout)
        => ContainsOrdinal(stderr, "not found locally") || ContainsOrdinal(stdout, "not found locally");

    private static bool ContainsOrdinal(string? text, string value)
        => !string.IsNullOrWhiteSpace(text)
           && text.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool TryReadId(JsonElement element, out string id)
    {
        foreach (var name in new[] { "thread_id", "threadId", "session_id", "sessionId", "conversation_id" })
        {
            if (element.TryGetProperty(name, out var property)
                && property.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(property.GetString()))
            {
                id = property.GetString()!;
                return true;
            }
        }

        id = string.Empty;
        return false;
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void WrapWindowsCommandShim(ProcessStartInfo psi)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var extension = Path.GetExtension(psi.FileName);
        if (!string.Equals(extension, ".cmd", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".bat", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var command = string.Join(' ', new[] { QuoteCmd(psi.FileName) }.Concat(psi.ArgumentList.Select(QuoteCmd)));
        psi.ArgumentList.Clear();
        psi.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
        psi.ArgumentList.Add("/d");
        psi.ArgumentList.Add("/s");
        psi.ArgumentList.Add("/c");
        psi.ArgumentList.Add(command);
    }

    private static string QuoteCmd(string value)
    {
        if (value.Length == 0)
            return "\"\"";

        var escaped = value.Replace("\"", "\\\"", StringComparison.Ordinal);
        return escaped.Any(char.IsWhiteSpace) ? $"\"{escaped}\"" : escaped;
    }
}
