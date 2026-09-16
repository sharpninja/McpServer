using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using McpServer.Support.Mcp.Tests.Infrastructure;

namespace McpServer.Support.Mcp.Tests.Documentation;

/// <summary>
/// TEST-MCP-USECASE-020-AC008: Consumer and executable-boundary proof for the sixteenth evidence
/// supersession and the formerly indirect F17, G37, and G38 command cells.
/// </summary>
[Collection(RepositoryRootProcessStateCollection.Name)]
public sealed class BugTriage139SixteenthEvidenceTests
{
    private const string FifteenthReceiptSha256 =
        "04BA22D37B5F29042A45B6D9A55FCE11EE23F4C33148B19BCDCE20052000F91C";
    private const string F17EvidenceSha256 =
        "1E75979A339DFE9B789042302CB9175F355C2821CB9162FFE146F6351E9326EC";
    private const string SixteenthReviewSha256 =
        "47CAD88F29DE6A4131D7B50F8CF0CD337B362316DAD41001BB49FB7F556C60D7";

    /// <summary>
    /// Proves the fifteenth receipt remains byte-identical to its reviewed candidate while the
    /// append-only current index carries its rejection and supersession.
    /// </summary>
    [Fact]
    public void FifteenthReceipt_IsImmutableAndCurrentIndexDeclaresRejection()
    {
        var root = GetRepositoryRoot();
        var receiptBytes = File.ReadAllBytes(
            Path.Combine(root, "docs", "receipts", "BUG-TRIAGE-139-fifteenth-remediation.md"));
        Assert.Equal(
            FifteenthReceiptSha256,
            Convert.ToHexString(SHA256.HashData(receiptBytes)));

        var index = File.ReadAllText(
            Path.Combine(root, "docs", "receipts", "BUG-TRIAGE-139-evidence-index.md"));
        Assert.Contains(
            "f410963f94d75064197ff576de3ee163a5f10f90",
            index,
            StringComparison.Ordinal);
        Assert.Contains(
            "was not approved",
            index,
            StringComparison.OrdinalIgnoreCase);

        var reviewPath = Path.Combine(
            root,
            "docs",
            "receipts",
            "artifacts",
            "BUG-TRIAGE-139",
            "reviews",
            "sixteenth-independent-review.txt");
        using var reviewStream = File.OpenRead(reviewPath);
        Assert.Equal(
            SixteenthReviewSha256,
            Convert.ToHexString(SHA256.HashData(reviewStream)));
    }

    /// <summary>
    /// Parses the replacement ledger, executes every safe Git/ripgrep boundary command with its
    /// exact argument vector, and independently proves every historical commit-message rewrite
    /// preserved its tree.
    /// </summary>
    [Fact]
    public async Task IndirectCells_HaveCompleteExecutableLedgerWithVerifiedBoundaries()
    {
        var root = GetRepositoryRoot();
        var ledgerPath = Path.Combine(
            root,
            "docs",
            "receipts",
            "artifacts",
            "BUG-TRIAGE-139",
            "sixteenth-remediation",
            "superseded-command-ledger.tsv");
        Assert.True(File.Exists(ledgerPath), $"Missing command ledger: {ledgerPath}");

        var rows = File.ReadAllLines(ledgerPath)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseLedgerRow)
            .ToArray();
        Assert.Equal(23, rows.Length);
        Assert.Equal(2, rows.Count(row => row.Cell == "F17"));
        Assert.Equal(7, rows.Count(row => row.Cell == "G37"));
        Assert.Equal(14, rows.Count(row => row.Cell == "G38"));
        Assert.DoesNotContain(
            rows,
            row => row.CommandDisplay.Contains("See ", StringComparison.OrdinalIgnoreCase) ||
                   row.CommandDisplay.Contains("mapped output", StringComparison.OrdinalIgnoreCase));

        var f17Artifact = Path.Combine(
            root,
            "docs",
            "receipts",
            "artifacts",
            "BUG-TRIAGE-139",
            "fifteenth-remediation",
            "final",
            "failed",
            "native-cleanup-probe-quoting-error.txt");
        await using (var stream = File.OpenRead(f17Artifact))
        {
            Assert.Equal(
                F17EvidenceSha256,
                Convert.ToHexString(
                    await SHA256.HashDataAsync(
                        stream,
                        TestContext.Current.CancellationToken)));
        }

        var executed = new Dictionary<string, CommandResult>(StringComparer.Ordinal);
        foreach (var row in rows.Where(row => row.Execute))
        {
            var result = await RunCommandAsync(root, row).ConfigureAwait(true);
            Assert.True(
                result.ExitCode == row.ExpectedExit,
                $"{row.Key} exit {result.ExitCode}, expected {row.ExpectedExit}.{Environment.NewLine}" +
                result.StandardOutput + result.StandardError);
            executed.Add(row.Key, result);
        }

        for (var pair = 1; pair <= 7; pair++)
        {
            var oldResult = executed[$"G38.{pair:00}.old"];
            var newResult = executed[$"G38.{pair:00}.new"];
            Assert.Equal(oldResult.StandardOutput.Trim(), newResult.StandardOutput.Trim());
        }
    }

    private static LedgerRow ParseLedgerRow(string line)
    {
        var fields = line.Split('	');
        Assert.Equal(9, fields.Length);
        var arguments = JsonSerializer.Deserialize<string[]>(fields[4]);
        Assert.NotNull(arguments);
        return new LedgerRow(
            fields[0],
            fields[1],
            fields[2],
            fields[3],
            arguments!,
            int.Parse(fields[5], System.Globalization.CultureInfo.InvariantCulture),
            bool.Parse(fields[6]),
            fields[7],
            fields[8]);
    }

    private static async Task<CommandResult> RunCommandAsync(string root, LedgerRow row)
    {
        var startInfo = new ProcessStartInfo(row.Executable)
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in row.Arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {row.Key}.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(true);
            await Task.WhenAll(standardOutput, standardError)
                .WaitAsync(deadline.Token)
                .ConfigureAwait(true);
        }
        catch
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw;
        }

        return new CommandResult(
            process.ExitCode,
            await standardOutput.ConfigureAwait(true),
            await standardError.ConfigureAwait(true));
    }

    private static string GetRepositoryRoot()
    {
        var metadata = typeof(BugTriage139SixteenthEvidenceTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute =>
                string.Equals(attribute.Key, "McpRepositoryRoot", StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(metadata.Value));
        return metadata.Value!;
    }

    private sealed record LedgerRow(
        string Cell,
        string Step,
        string Key,
        string Executable,
        string[] Arguments,
        int ExpectedExit,
        bool Execute,
        string CommandDisplay,
        string Evidence);

    private sealed record CommandResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
