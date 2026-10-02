using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-018: Twelfth-review adversarial tests for same-object containment and
/// bounded streaming requirements snapshots.
/// </summary>
[Collection(nameof(McpServer.Support.Mcp.Tests.Storage.WorkspaceIdentityNativeProcessStateCollection))]
public sealed class RequirementsTwelfthAdversarialReviewTests
{
    /// <summary>Traversal must reject a file count beyond its explicit limit.</summary>
    [Fact]
    public void EnumerateFiles_FileCountLimit_Throws()
    {
        using var fixture = new TraversalFixture();
        fixture.Write("one.md", "1");
        fixture.Write("two.md", "2");

        AssertTraversalLimit(
            fixture,
            maxFiles: 1,
            maxFileBytes: 32,
            maxAggregateBytes: 64,
            maxDepth: 4);
    }

    /// <summary>Traversal must reject a file larger than its explicit per-file byte limit.</summary>
    [Fact]
    public void EnumerateFiles_PerFileByteLimit_Throws()
    {
        using var fixture = new TraversalFixture();
        fixture.Write("large.md", "12345");

        AssertTraversalLimit(
            fixture,
            maxFiles: 4,
            maxFileBytes: 4,
            maxAggregateBytes: 64,
            maxDepth: 4);
    }

    /// <summary>Traversal must reject files whose combined size exceeds its aggregate limit.</summary>
    [Fact]
    public void EnumerateFiles_AggregateByteLimit_Throws()
    {
        using var fixture = new TraversalFixture();
        fixture.Write("one.md", "1234");
        fixture.Write("two.md", "5678");

        AssertTraversalLimit(
            fixture,
            maxFiles: 4,
            maxFileBytes: 8,
            maxAggregateBytes: 6,
            maxDepth: 4);
    }

    /// <summary>Traversal must reject recursion beyond its explicit depth limit.</summary>
    [Fact]
    public void EnumerateFiles_RecursionDepthLimit_Throws()
    {
        using var fixture = new TraversalFixture();
        fixture.Write(Path.Combine("one", "two", "deep.md"), "deep");

        AssertTraversalLimit(
            fixture,
            maxFiles: 4,
            maxFileBytes: 32,
            maxAggregateBytes: 64,
            maxDepth: 1);
    }

    /// <summary>
    /// Enumeration must be lazy and observe cancellation after the first yielded file.
    /// </summary>
    [Fact]
    public void EnumerateFiles_CancelledAfterFirstYield_StopsWithoutMaterializingAllPaths()
    {
        using var fixture = new TraversalFixture();
        fixture.Write("one.md", "1");
        fixture.Write("two.md", "2");
        fixture.Write("three.md", "3");
        using var cancellation = new CancellationTokenSource();

        var enumerable = EnumerateWithLimits(
            fixture,
            maxFiles: 8,
            maxFileBytes: 32,
            maxAggregateBytes: 64,
            maxDepth: 4,
            cancellation.Token);
        Assert.False(enumerable is IReadOnlyCollection<string>);

        using var enumerator = enumerable.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// A swapping directory alias must never expose external bytes through the same-handle read
    /// contract.
    /// </summary>
#pragma warning disable xUnit1051 // Explicit deadlines must fail visibly instead of becoming runner-cancelled no-result tests.
    [Fact]
    public async Task OpenFileForRead_DirectoryAliasSwap_NeverReadsExternalObject()
    {
        using var fixture = new AliasSwapFixture();
        var method = ResolveContainedOpenMethod("OpenFileForRead");
        using var stop = new CancellationTokenSource();
        var swapTask = fixture.SwapUntilCancelledAsync(stop.Token);
        var successfulAttempts = 0;
        var rejectedRaces = 0;

        try
        {
            for (var attempt = 0; attempt < 500; attempt++)
            {
                try
                {
                    using var stream = Assert.IsType<FileStream>(
                        method.Invoke(null, [fixture.Root, fixture.LiveFile]));
                    using var reader = new StreamReader(
                        stream,
                        Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: true,
                        leaveOpen: false);
                    var content = await reader
                        .ReadToEndAsync(CancellationToken.None)
                        .WaitAsync(TimeSpan.FromSeconds(5))
                        .ConfigureAwait(true);
                    Assert.Equal(AliasSwapFixture.InsideText, content);
                    successfulAttempts++;
                }
                catch (TargetInvocationException exception)
                {
                    var failure = Assert.IsAssignableFrom<Exception>(
                        exception.InnerException);
                    AssertExpectedAliasRaceRejection(failure);
                    rejectedRaces++;
                }

                await Task.Yield();
            }
        }
        finally
        {
            stop.Cancel();
            await swapTask
                .WaitAsync(TimeSpan.FromSeconds(10))
                .ConfigureAwait(true);
        }

        Assert.Equal(500, successfulAttempts + rejectedRaces);
        AssertRaceHadSuccessfulOperation(successfulAttempts, totalAttempts: 500);
        Assert.Equal(
            AliasSwapFixture.OutsideText,
            await File
                .ReadAllTextAsync(fixture.ExternalFile, CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(5))
                .ConfigureAwait(true));
    }

    /// <summary>
    /// A swapping directory alias must never permit rollback-style writes to an external object.
    /// </summary>
    [Fact]
    public async Task OpenFileForWrite_DirectoryAliasSwap_NeverWritesExternalObject()
    {
        using var fixture = new AliasSwapFixture();
        var method = ResolveContainedOpenMethod("OpenFileForWrite");
        using var stop = new CancellationTokenSource();
        var swapTask = fixture.SwapUntilCancelledAsync(stop.Token);
        var payload = Encoding.UTF8.GetBytes(AliasSwapFixture.InsideText);
        var successfulAttempts = 0;
        var rejectedRaces = 0;

        try
        {
            for (var attempt = 0; attempt < 500; attempt++)
            {
                try
                {
                    using var stream = Assert.IsType<FileStream>(
                        method.Invoke(null, [fixture.Root, fixture.LiveFile]));
                    stream.SetLength(0);
                    await stream
                        .WriteAsync(payload, CancellationToken.None)
                        .AsTask()
                        .WaitAsync(TimeSpan.FromSeconds(5))
                        .ConfigureAwait(true);
                    await stream
                        .FlushAsync(CancellationToken.None)
                        .WaitAsync(TimeSpan.FromSeconds(5))
                        .ConfigureAwait(true);
                    successfulAttempts++;
                }
                catch (TargetInvocationException exception)
                {
                    var failure = Assert.IsAssignableFrom<Exception>(
                        exception.InnerException);
                    AssertExpectedAliasRaceRejection(failure);
                    rejectedRaces++;
                }

                await Task.Yield();
            }
        }
        finally
        {
            stop.Cancel();
            await swapTask
                .WaitAsync(TimeSpan.FromSeconds(10))
                .ConfigureAwait(true);
        }

        Assert.Equal(500, successfulAttempts + rejectedRaces);
        AssertRaceHadSuccessfulOperation(successfulAttempts, totalAttempts: 500);
        Assert.Equal(
            AliasSwapFixture.OutsideText,
            await File
                .ReadAllTextAsync(fixture.ExternalFile, CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(5))
                .ConfigureAwait(true));
    }
#pragma warning restore xUnit1051


    internal static void AssertRaceHadSuccessfulOperation(
        int successfulAttempts,
        int totalAttempts)
    {
        Assert.InRange(
            successfulAttempts,
            low: 1,
            high: totalAttempts);
    }

    private static void AssertExpectedAliasRaceRejection(Exception failure)
    {
        var message = failure.Message;
        Assert.False(string.IsNullOrWhiteSpace(message));

        if (failure is UnauthorizedAccessException unauthorized)
        {
            var nativeCode = unauthorized.HResult & 0xFFFF;
            Assert.Contains(nativeCode, new[] { 5, 13 });
            return;
        }

        var ioFailure = Assert.IsAssignableFrom<IOException>(failure);
        var expectedContractMessage = new[]
        {
            "Unable to pin contained directory",
            "Unable to pin contained directory segment",
            "Unable to open contained file",
            "Contained directory is not an ordinary",
            "Contained entry is not an ordinary",
            "Contained file changed while it was opened",
            "Opened entry escaped the workspace root",
        }.Any(prefix =>
            message.StartsWith(prefix, StringComparison.Ordinal));
        var nativeError = ioFailure.InnerException is System.ComponentModel.Win32Exception win32
            ? win32.NativeErrorCode
            : ioFailure.HResult & 0xFFFF;
        var expectedNativeError = OperatingSystem.IsWindows()
            ? new[] { 2, 3, 5, 32, 33, 4390, 4392, 4393 }.Contains(nativeError)
            : new[] { 2, 13, 20, 40, 116 }.Contains(nativeError);

        Assert.True(
            expectedContractMessage || expectedNativeError,
            $"Unexpected alias-race failure: {failure}");
    }

    private static void AssertTraversalLimit(
        TraversalFixture fixture,
        int maxFiles,
        long maxFileBytes,
        long maxAggregateBytes,
        int maxDepth)
    {
        var enumerable = EnumerateWithLimits(
            fixture,
            maxFiles,
            maxFileBytes,
            maxAggregateBytes,
            maxDepth,
            CancellationToken.None);

        Assert.Throws<IOException>(() => enumerable.ToArray());
    }

    private static IEnumerable<string> EnumerateWithLimits(
        TraversalFixture fixture,
        int maxFiles,
        long maxFileBytes,
        long maxAggregateBytes,
        int maxDepth,
        CancellationToken cancellationToken)
    {
        var assembly = typeof(WorkspaceContainedFileSystem).Assembly;
        var limitType = assembly.GetType(
            "McpServer.Support.Mcp.Storage.WorkspaceTraversalLimits",
            throwOnError: false);
        Assert.NotNull(limitType);
        var limits = Activator.CreateInstance(limitType!);
        Assert.NotNull(limits);
        SetLimit(limitType!, limits!, "MaxFiles", maxFiles);
        SetLimit(limitType!, limits!, "MaxFileBytes", maxFileBytes);
        SetLimit(limitType!, limits!, "MaxAggregateBytes", maxAggregateBytes);
        SetLimit(limitType!, limits!, "MaxDepth", maxDepth);

        var method = typeof(WorkspaceContainedFileSystem)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(
                candidate =>
                {
                    if (!string.Equals(candidate.Name, "EnumerateFiles", StringComparison.Ordinal))
                        return false;
                    var parameters = candidate.GetParameters();
                    return parameters.Length == 4 &&
                           parameters[0].ParameterType == typeof(string) &&
                           parameters[1].ParameterType == typeof(string) &&
                           parameters[2].ParameterType == limitType &&
                           parameters[3].ParameterType == typeof(CancellationToken);
                });
        Assert.NotNull(method);

        var result = method!.Invoke(
            null,
            [fixture.Root, fixture.Root, limits, cancellationToken]);
        return Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    private static void SetLimit(Type limitType, object limits, string name, object value)
    {
        var property = limitType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        property!.SetValue(limits, value);
    }

    private static MethodInfo ResolveContainedOpenMethod(string name)
    {
        var method = typeof(WorkspaceContainedFileSystem).GetMethod(
            name,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            [typeof(string), typeof(string)],
            modifiers: null);
        Assert.NotNull(method);
        return method!;
    }



    private sealed class TraversalFixture : IDisposable
    {
        public TraversalFixture()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                "mcp-twelfth-traversal-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Write(string relativePath, string content)
        {
            var path = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }

    private sealed class AliasSwapFixture : IDisposable
    {
        public const string InsideText = "inside-twelfth";
        public const string OutsideText = "outside-twelfth";

        private readonly string _holdingDirectory;
        private readonly string _parkedAlias;

        public AliasSwapFixture()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                "mcp-twelfth-root-" + Guid.NewGuid().ToString("N"));
            ExternalRoot = Path.Combine(
                Path.GetTempPath(),
                "mcp-twelfth-external-" + Guid.NewGuid().ToString("N"));
            LiveDirectory = Path.Combine(Root, "wiki");
            _holdingDirectory = Path.Combine(Root, "wiki-safe");
            _parkedAlias = Path.Combine(Root, "wiki-external-alias");
            LiveFile = Path.Combine(LiveDirectory, "document.md");
            ExternalFile = Path.Combine(ExternalRoot, "document.md");

            Directory.CreateDirectory(LiveDirectory);
            Directory.CreateDirectory(ExternalRoot);
            File.WriteAllText(LiveFile, InsideText);
            File.WriteAllText(ExternalFile, OutsideText);
            CreateDirectoryAlias(_parkedAlias, ExternalRoot);
            Assert.True(IsReparsePoint(_parkedAlias));
        }

        public string Root { get; }

        public string ExternalRoot { get; }

        public string LiveDirectory { get; }

        public string LiveFile { get; }

        public string ExternalFile { get; }

        public async Task SwapUntilCancelledAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (Directory.Exists(LiveDirectory) &&
                        !IsReparsePoint(LiveDirectory) &&
                        Directory.Exists(_parkedAlias) &&
                        IsReparsePoint(_parkedAlias))
                    {
                        Directory.Move(LiveDirectory, _holdingDirectory);
                        Directory.Move(_parkedAlias, LiveDirectory);
                        await Task.Yield();
                        Directory.Move(LiveDirectory, _parkedAlias);
                        Directory.Move(_holdingDirectory, LiveDirectory);
                    }
                }
                catch (IOException)
                {
                    RestoreSafeLayout();
                }
                catch (UnauthorizedAccessException)
                {
                    RestoreSafeLayout();
                }

                await Task.Yield();
            }

            RestoreSafeLayout();
        }

        public void Dispose()
        {
            RestoreSafeLayout();
            DeleteAliasIfPresent(_parkedAlias);
            DeleteAliasIfPresent(LiveDirectory);
            DeleteAliasIfPresent(_holdingDirectory);
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
            if (Directory.Exists(ExternalRoot))
                Directory.Delete(ExternalRoot, recursive: true);
        }

        private void RestoreSafeLayout()
        {
            try
            {
                if (Directory.Exists(LiveDirectory) && IsReparsePoint(LiveDirectory))
                {
                    if (!Directory.Exists(_parkedAlias))
                        Directory.Move(LiveDirectory, _parkedAlias);
                    else
                        Directory.Delete(LiveDirectory);
                }

                if (!Directory.Exists(LiveDirectory) && Directory.Exists(_holdingDirectory))
                    Directory.Move(_holdingDirectory, LiveDirectory);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static bool IsReparsePoint(string path) =>
            Directory.Exists(path) &&
            File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);

        private static void DeleteAliasIfPresent(string path)
        {
            try
            {
                if (IsReparsePoint(path))
                    Directory.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void CreateDirectoryAlias(string aliasPath, string targetPath)
        {
            if (!OperatingSystem.IsWindows())
            {
                Directory.CreateSymbolicLink(aliasPath, targetPath);
                return;
            }

            var escapedAlias = aliasPath.Replace("'", "''", StringComparison.Ordinal);
            var escapedTarget = targetPath.Replace("'", "''", StringComparison.Ordinal);
            var startInfo = new ProcessStartInfo("pwsh.exe")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add(
                $"New-Item -ItemType Junction -Path '{escapedAlias}' -Target '{escapedTarget}' | Out-Null");

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Unable to start junction creation process.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(5000))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("Junction creation timed out.");
            }

            Task.WaitAll(stdout, stderr);
            Assert.True(
                process.ExitCode == 0,
                $"Junction creation failed: {stderr.Result}");
        }
    }
}

/// <summary>
/// TEST-MCP-USECASE-020: deterministic consumer-gate failure tests kept separate from the fixed
/// seven-class external inventory.
/// </summary>
public sealed class RequirementsTwelfthContainmentGateTests
{
    /// <summary>Proves that rejecting every attempted contained operation fails the gate.</summary>
    [Fact]
    public void AllRejectedAttempts_CannotPassConsumerGate()
    {
        _ = Assert.Throws<Xunit.Sdk.InRangeException>(
            () => RequirementsTwelfthAdversarialReviewTests.AssertRaceHadSuccessfulOperation(
                successfulAttempts: 0,
                totalAttempts: 500));
    }
}
