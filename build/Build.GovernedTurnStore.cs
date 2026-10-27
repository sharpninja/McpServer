// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using Nuke.Common;

partial class Build
{
    /// <summary>TEST-QB-005/008/009/014/069: Select the A8 durable-store contract provider.</summary>
    [Parameter("A8 store contract provider: Mock or Production")]
    readonly string A8StoreMode = "Mock";

    /// <summary>TEST-QB-005/008/009/014/069: Verify A8 store behavior in an isolated directory.</summary>
    public Target TestGovernedTurnStore => _ => _
        .Description("Run the PowerShell A8 durable lifecycle contract")
        .Executes(() =>
        {
            if (A8StoreMode is not ("Mock" or "Production"))
                throw new ArgumentException("A8StoreMode must be Mock or Production.");
            var start = new ProcessStartInfo("pwsh") { UseShellExecute = false, WorkingDirectory = RootDirectory };
            foreach (var argument in new[]
            {
                "-NoProfile", "-NonInteractive", "-File",
                (RootDirectory / "plugins/core/test-fixtures/GovernedTurnStore.Contract.ps1").ToString(),
                "-Mode", A8StoreMode, "-ReceiptPath",
                (RootDirectory / $"TestResults/a8-store-{A8StoreMode.ToLowerInvariant()}.json").ToString(),
            }) start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("PowerShell did not start.");
            process.WaitForExit();
            if (process.ExitCode != 0) throw new InvalidOperationException($"A8 store contract failed: {process.ExitCode}.");
        });
}
