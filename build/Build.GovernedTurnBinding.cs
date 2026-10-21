// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using Nuke.Common;

partial class Build
{
    /// <summary>TEST-QB-008/014/069: Select the mock assertion proof or production binding provider.</summary>
    [Parameter("A8 selection contract provider: Mock or Production")]
    readonly string A8BindingMode = "Mock";

    /// <summary>TEST-QB-008/014/069: Execute the isolated PowerShell selection contract.</summary>
    public Target TestGovernedTurnBinding => _ => _
        .Description("Run the A8 ordinary-write binding contract with explicit mock/production evidence")
        .Executes(() =>
        {
            if (A8BindingMode is not ("Mock" or "Production"))
                throw new ArgumentException("A8BindingMode must be Mock or Production.");
            var start = new ProcessStartInfo("pwsh") { UseShellExecute = false, WorkingDirectory = RootDirectory };
            foreach (var argument in new[]
            {
                "-NoProfile", "-NonInteractive", "-File",
                (RootDirectory / "plugins/core/test-fixtures/GovernedTurnBinding.Contract.ps1").ToString(),
                "-Mode", A8BindingMode, "-ReceiptPath",
                (RootDirectory / $"TestResults/a8-binding-{A8BindingMode.ToLowerInvariant()}.json").ToString(),
            }) start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("PowerShell did not start.");
            process.WaitForExit();
            if (process.ExitCode != 0) throw new InvalidOperationException($"A8 binding contract failed: {process.ExitCode}.");
        });
}
