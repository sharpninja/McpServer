// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Text.Json;
using QBrainAi.Support.Mcp.Tests.Infrastructure;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-QB-003/005, TR-QB-AUDIT-001, TEST-QB-008/009/069:
/// cumulative coverage executes the exact PowerShell scenario runner first against
/// the independent state model and then against real SQLite through its frozen seam.
/// </summary>
public sealed class ServerAuditEventContractTests
{
    /// <summary>
    /// All26 original event scenarios plus exact captured-envelope and cross-workspace UUID scenarios,
    /// 81 rejecting controls and10 positive probes gate the SAME28 real SQLite interactions, including rollback, concurrent
    /// connections and retained database reopen. The additional3 upgrade scenarios and13 controls
    /// exercise provider migration/readiness and exact legacy retention. Every receipt/database is retained.
    /// </summary>
    [Theory]
    [InlineData("Event", 28, 81)]
    [InlineData("Upgrade", 3, 13)]
    public async Task IndependentGateThenSameRealSqliteScenarios_RejectLossAndPreserveCommittedIdentity(
        string contractSet, int scenarios, int negativeControls)
    {
        var root = RepositoryEvidenceTestSupport.ResolveRepositoryRoot();
        var artifacts = Path.Combine(root, ".mcpServer", "server-audit-contract", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(artifacts);
        var mock = Path.Combine(artifacts, "mock.json");
        var production = Path.Combine(artifacts, "production.json");
        await RunAsync("Mock", mock, true).ConfigureAwait(true);
        await RunAsync("Mock", mock, false).ConfigureAwait(true);
        AssertReceipt(mock, negativeControls, scenarios);
        await RunAsync("Production", production, true).ConfigureAwait(true);
        await RunAsync("Production", production, false).ConfigureAwait(true);
        AssertReceipt(production, 0, scenarios);

        async Task RunAsync(string mode, string receipt, bool whatIf)
        {
            var start = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh",
                WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true,
            };
            foreach (var argument in new[]
            {
                "-NoProfile", "-NonInteractive", "-File",
                Path.Combine(root, "plugins", "core", "test-fixtures", "ServerAuditEvent.Sqlite.ps1"),
                "-Mode", mode, "-ContractSet", contractSet, "-ReceiptPath", receipt, "-ArtifactRoot", artifacts,
                "-AssemblyDirectory", AppContext.BaseDirectory, "-MockReceiptPath", mock,
            })
                start.ArgumentList.Add(argument);
            if (whatIf)
                start.ArgumentList.Add("-WhatIf");
            using var process = Process.Start(start) ?? throw new InvalidOperationException("PowerShell fixture did not start");
            var stdout = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            var stderr = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            await process.WaitForExitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var output = await stdout.ConfigureAwait(true);
            var error = await stderr.ConfigureAwait(true);
            Assert.True(process.ExitCode == 0, $"Frozen {mode} fixture exit{process.ExitCode}: {output}\n{error}");
            Assert.True(string.IsNullOrEmpty(error), error);
        }

        static void AssertReceipt(string path, int controls, int expectedScenarios)
        {
            using var receipt = JsonDocument.Parse(File.ReadAllText(path));
            var body = receipt.RootElement;
            Assert.Equal(expectedScenarios, body.GetProperty("cases").GetInt32());
            Assert.Equal(controls, body.GetProperty(body.TryGetProperty("controls", out _) ? "controls" : "negativeControls").GetInt32());
            Assert.Equal(0, body.GetProperty("failed").GetInt32());
            Assert.Equal(0, body.GetProperty("skipped").GetInt32());
            Assert.Equal(expectedScenarios, body.GetProperty("observations").GetArrayLength());
        }
    }
}
