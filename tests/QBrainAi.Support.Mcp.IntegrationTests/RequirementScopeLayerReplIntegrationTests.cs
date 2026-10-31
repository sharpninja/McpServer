using System.Diagnostics;
using System.Text;
using QBrainAi.TestSupport.TemporaryInstance;

namespace QBrainAi.Support.Mcp.IntegrationTests;

/// <summary>
/// TEST-MCP-REQSCOPE-REPL-001 (historical): REPL-driven integration coverage for requirement scope layers, on the
/// shared temporary instance (<see cref="ScratchQBrainAi"/>, TR-MCP-NUKE-019 AC-6).
/// </summary>
/// <remarks>
/// The TEST-MCP-REQSCOPE-REPL-001 citations in this file are historical: that record is not in the requirements store
/// (the committed docs/Project exports list it as planned), so they make no active coverage claim (the class of hostile
/// validation finding R6-F2; plan section 25). The active claim is TR-MCP-NUKE-019 AC-6, the temporary instance.
/// </remarks>
[Trait("Category", "Integration")]
public sealed class RequirementScopeLayerReplIntegrationTests
{
    private static readonly TimeSpan ReplRequestTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// TEST-MCP-REQSCOPE-REPL-001: creates layers, starts a requirement in a later layer,
    /// sunsets a layer and a requirement, then queries current effective requirements before
    /// and after the new layer through a real marker-backed REPL process.
    /// </summary>
    [Fact]
    public async Task RequirementScopeLayer_ReplWorkflow_ExercisesCurrentEffectiveRequirementsBeforeAndAfterLayer()
    {
        await using var scratch = await ScratchQBrainAi.StartAsync().ConfigureAwait(true);
        await using var repl = await ReplProcess.StartAsync(
            scratch.ReplHostAssemblyPath,
            scratch.WorkspacePath,
            scratch.MarkerPath,
            scratch.RootPath,
            scratch.BaseUrl.ToString()).ConfigureAwait(true);

        await RunReplAsync(repl, "reqscope-int-create-layer-2", """
            type: request
            payload:
              requestId: reqscope-int-create-layer-2
              method: workflow.requirements.createLayer
              params:
                key: layer-2
                order: 2
                name: Layer 2

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-create-layer-3", """
            type: request
            payload:
              requestId: reqscope-int-create-layer-3
              method: workflow.requirements.createLayer
              params:
                key: layer-3
                order: 3
                name: Layer 3

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-create-layer-4", """
            type: request
            payload:
              requestId: reqscope-int-create-layer-4
              method: workflow.requirements.createLayer
              params:
                key: layer-4
                order: 4
                name: Layer 4

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-create-new-fr", """
            type: request
            payload:
              requestId: reqscope-int-create-new-fr
              method: workflow.requirements.createFr
              params:
                id: FR-MCP-REQSCOPE-951
                title: New layer requirement
                description: Applies from layer 2 until the layer 2 sunset.
                priority: high
                area: MCP
                scopeStartLayerKey: layer-2
                acceptanceCriteria:
                  - text: TEST-MCP-REQSCOPE-REPL-001 verifies this requirement becomes visible at layer 2.

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-create-old-fr", """
            type: request
            payload:
              requestId: reqscope-int-create-old-fr
              method: workflow.requirements.createFr
              params:
                id: FR-MCP-REQSCOPE-950
                title: Sunset before layer
                description: Starts before layer 2 and is sunset by update.
                priority: high
                area: MCP
                scopeStartLayerKey: layer-1
                acceptanceCriteria:
                  - text: TEST-MCP-REQSCOPE-REPL-001 verifies this requirement is hidden after layer 1.

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-sunset-fr-before-layer", """
            type: request
            payload:
              requestId: reqscope-int-sunset-fr-before-layer
              method: workflow.requirements.updateFr
              params:
                id: FR-MCP-REQSCOPE-950
                scopeEndLayerKey: layer-1

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-sunset-layer", """
            type: request
            payload:
              requestId: reqscope-int-sunset-layer
              method: workflow.requirements.updateLayer
              params:
                key: layer-2
                scopeEndLayerKey: layer-3

            """).ConfigureAwait(true);

        await SetCurrentLayerAsync(repl, "layer-1").ConfigureAwait(true);
        var beforeOutput = await QueryCurrentEffectiveAsync(repl, "reqscope-int-effective-before").ConfigureAwait(true);

        await SetCurrentLayerAsync(repl, "layer-2").ConfigureAwait(true);
        var afterOutput = await QueryCurrentEffectiveAsync(repl, "reqscope-int-effective-after").ConfigureAwait(true);

        await SetCurrentLayerAsync(repl, "layer-3").ConfigureAwait(true);
        var sunsetInclusiveOutput = await QueryCurrentEffectiveAsync(
            repl,
            "reqscope-int-effective-sunset-inclusive").ConfigureAwait(true);

        await SetCurrentLayerAsync(repl, "layer-4").ConfigureAwait(true);
        var afterSunsetOutput = await QueryCurrentEffectiveAsync(repl, "reqscope-int-effective-after-sunset").ConfigureAwait(true);

        Assert.True(File.Exists(scratch.MarkerPath), $"Expected generated marker at '{scratch.MarkerPath}'.");
        // FR-MCP-REPL-008 ac-4 / TR-MCP-NUKE-019 AC-6: bind the product override in the real child process.
        var cachePath = Path.Combine(scratch.RootPath, "repl-cache", "verified-markers.json");
        Assert.True(File.Exists(cachePath), $"Expected the child REPL's verified-marker cache at '{cachePath}'.");
        Assert.Contains("FR-MCP-REQSCOPE-950", beforeOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("FR-MCP-REQSCOPE-951", beforeOutput, StringComparison.Ordinal);
        Assert.Contains("FR-MCP-REQSCOPE-951", afterOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("FR-MCP-REQSCOPE-950", afterOutput, StringComparison.Ordinal);
        Assert.Contains("FR-MCP-REQSCOPE-951", sunsetInclusiveOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("FR-MCP-REQSCOPE-951", afterSunsetOutput, StringComparison.Ordinal);
    }

    private static Task<string> QueryCurrentEffectiveAsync(ReplProcess repl, string requestId)
        => RunReplAsync(repl, requestId, $$"""
            type: request
            payload:
              requestId: {{requestId}}
              method: workflow.requirements.effective
              params: {}

            """);

    private static Task<string> SetCurrentLayerAsync(ReplProcess repl, string layerKey)
        => RunReplAsync(repl, $"reqscope-int-set-current-{layerKey}", $$"""
            type: request
            payload:
              requestId: reqscope-int-set-current-{{layerKey}}
              method: client.workspace.SetCurrentRequirementLayerAsync
              params:
                request:
                  layerKey: {{layerKey}}

            """);

    private static async Task<string> RunReplAsync(ReplProcess repl, string requestId, string yaml)
    {
        await repl.WriteLineAsync(yaml).ConfigureAwait(true);
        var received = await repl.WaitForResponseAsync(requestId, ReplRequestTimeout).ConfigureAwait(true);
        Assert.True(received, $"Timed out waiting for REPL response '{requestId}'.{Environment.NewLine}{repl.Diagnostics}");

        var document = repl.GetResponseDocument(requestId);
        Assert.False(string.IsNullOrWhiteSpace(document), $"Missing REPL document for '{requestId}'.{Environment.NewLine}{repl.Diagnostics}");
        Assert.True(
            document.Contains("type: result", StringComparison.Ordinal),
            $"Expected a result envelope for '{requestId}' but received:{Environment.NewLine}{document}");
        return document;
    }

    private sealed class ReplProcess : IAsyncDisposable
    {
        private readonly List<string> _stdoutDocuments = new();
        private readonly List<string> _stderrLines = new();
        private readonly List<string> _currentDocument = new();
        private readonly object _lock = new();
        private int? _stdoutBlockScalarIndent;
        private Process? _process;

        private ReplProcess(Process process)
        {
            _process = process;
        }

        public string Diagnostics
        {
            get
            {
                lock (_lock)
                {
                    var pending = _currentDocument.Count == 0
                        ? string.Empty
                        : string.Join(Environment.NewLine, _currentDocument);
                    var stdout = _stdoutDocuments.Count == 0
                        ? "<none>"
                        : string.Join($"{Environment.NewLine}--- stdout document ---{Environment.NewLine}", _stdoutDocuments);
                    if (!string.IsNullOrWhiteSpace(pending))
                    {
                        stdout += $"{Environment.NewLine}--- pending stdout document ---{Environment.NewLine}{pending}";
                    }

                    var stderr = _stderrLines.Count == 0 ? "<none>" : string.Join(Environment.NewLine, _stderrLines);
                    return $"STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}";
                }
            }
        }

        /// <summary>
        /// Starts the REPL host against the temporary instance and fails before the process starts unless its
        /// workspace and marker lie inside the instance's root and no inherited product setting reaches it
        /// (TR-MCP-NUKE-019 AC-6; hostile validation findings R8B-F2 and R8B-F3).
        /// </summary>
        public static async Task<ReplProcess> StartAsync(string assemblyPath, string workspacePath, string markerPath, string ownedRoot, string serverAddress)
        {
            var startInfo = OwnedReplStart.CreateStartInfo(assemblyPath, workspacePath, markerPath, ownedRoot, serverAddress);

            var process = new Process { StartInfo = startInfo };
            var repl = new ReplProcess(process);
            process.OutputDataReceived += repl.OnStdoutDataReceived;
            process.ErrorDataReceived += repl.OnStderrDataReceived;
            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start qbrain-ai-repl host process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"qbrain-ai-repl host exited during startup with code {process.ExitCode}.{Environment.NewLine}{repl.Diagnostics}");
            }

            return repl;
        }

        public async Task WriteLineAsync(string yaml)
        {
            if (_process is not { HasExited: false })
            {
                throw new InvalidOperationException($"REPL process is not running.{Environment.NewLine}{Diagnostics}");
            }

            await _process.StandardInput.WriteLineAsync(yaml.AsMemory()).ConfigureAwait(false);
            await _process.StandardInput.WriteLineAsync(string.Empty.AsMemory()).ConfigureAwait(false);
            await _process.StandardInput.FlushAsync().ConfigureAwait(false);
        }

        public async Task<bool> WaitForResponseAsync(string requestId, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                lock (_lock)
                {
                    if (_stdoutDocuments.Any(document => IsFinalResponseForRequest(document, requestId)))
                    {
                        return true;
                    }
                }

                if (_process is { HasExited: true })
                {
                    throw new InvalidOperationException(
                        $"REPL process exited before stdout contained a final response for request '{requestId}'.{Environment.NewLine}{Diagnostics}");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
            }

            return false;
        }

        public string? GetResponseDocument(string requestId)
        {
            lock (_lock)
            {
                return _stdoutDocuments.FirstOrDefault(document => IsFinalResponseForRequest(document, requestId));
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_process is null)
            {
                return;
            }

            try
            {
                if (!_process.HasExited)
                {
                    _process.StandardInput.Close();
                    if (!_process.WaitForExit((int)TimeSpan.FromSeconds(2).TotalMilliseconds))
                    {
                        _process.Kill(entireProcessTree: true);
                    }
                }

                await _process.WaitForExitAsync().ConfigureAwait(false);
                _process.Dispose();
            }
            catch
            {
                // Best-effort cleanup for failed integration-test starts.
            }
        }

        private void OnStdoutDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            lock (_lock)
            {
                var line = e.Data.TrimStart('\uFEFF');
                if (IsTopLevelDocumentStart(line) && _currentDocument.Count > 0)
                {
                    FlushStdoutDocument();
                }

                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    if (_stdoutBlockScalarIndent is null)
                    {
                        FlushStdoutDocument();
                    }
                    else
                    {
                        _currentDocument.Add(line);
                    }

                    return;
                }

                if (_stdoutBlockScalarIndent is int blockIndent
                    && CountLeadingSpaces(line) <= blockIndent)
                {
                    _stdoutBlockScalarIndent = null;
                }

                _currentDocument.Add(line);
                if (StartsBlockScalar(line))
                {
                    _stdoutBlockScalarIndent = CountLeadingSpaces(line);
                }
            }
        }

        private void OnStderrDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            lock (_lock)
            {
                _stderrLines.Add(e.Data);
            }
        }

        private void FlushStdoutDocument()
        {
            if (_currentDocument.Count == 0)
            {
                return;
            }

            _stdoutDocuments.Add(string.Join(Environment.NewLine, _currentDocument));
            _currentDocument.Clear();
            _stdoutBlockScalarIndent = null;
        }

        private static bool IsTopLevelDocumentStart(string line)
            => line.StartsWith("type:", StringComparison.Ordinal);

        private static bool IsFinalResponseForRequest(string document, string requestId)
        {
            return (document.Contains("type: result", StringComparison.Ordinal)
                    || document.Contains("type: error", StringComparison.Ordinal))
                && (document.Contains($"requestId: {requestId}", StringComparison.Ordinal)
                    || document.Contains($"requestId: \"{requestId}\"", StringComparison.Ordinal)
                    || document.Contains($"requestId: '{requestId}'", StringComparison.Ordinal));
        }

        private static int CountLeadingSpaces(string line)
        {
            var count = 0;
            while (count < line.Length && line[count] == ' ')
            {
                count++;
            }

            return count;
        }

        private static bool StartsBlockScalar(string line)
        {
            var trimmed = line.TrimEnd();
            var colonIndex = trimmed.LastIndexOf(':');
            if (colonIndex < 0 || colonIndex == trimmed.Length - 1)
            {
                return false;
            }

            var value = trimmed[(colonIndex + 1)..].TrimStart();
            return value.StartsWith('|') || value.StartsWith('>');
        }
    }
}
