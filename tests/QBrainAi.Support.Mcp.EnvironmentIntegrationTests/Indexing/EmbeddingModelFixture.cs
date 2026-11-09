using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using QBrainAi.Support.Mcp.Indexing;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Indexing;

/// <summary>
/// TR-MCP-EMBEDDING-001: asynchronous class fixture that downloads the product's exact model and vocabulary sources
/// into one uniquely named test-owned directory, loads <see cref="EmbeddingService"/> from the explicit model path,
/// and removes only that directory at teardown.
/// </summary>
/// <remarks>
/// 2026-11-09, FR-MCP-004 embedding-vector-contract; TR-MCP-EMBEDDING-001 test-owned-fixture,
/// unavailable-is-failure and no-user-cache-mutation: added for the PR87 test-only slice. The fixture reads the
/// private product URL constants by reflection so its sources cannot silently diverge, streams each response to its
/// owned root, records source, SHA-256 and length, and sets AutoDownload=false so no default model cache is used.
/// </remarks>
public sealed class EmbeddingModelFixture : IAsyncLifetime
{
    private readonly HttpClient _httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly List<EmbeddingArtifactReceipt> _artifacts = [];

    /// <summary>Creates a fixture with a new unique root under the host's temporary directory.</summary>
    public EmbeddingModelFixture()
        => RootPath = Path.Combine(Path.GetTempPath(), $"qbrainai-embedding-model-{Guid.NewGuid():N}");

    /// <summary>Gets the unique directory owned by this fixture.</summary>
    public string RootPath { get; }

    /// <summary>Gets the required available service after <see cref="InitializeAsync"/> completes.</summary>
    public EmbeddingService Service { get; private set; } = null!;

    /// <summary>Gets the source URL, length and SHA-256 recorded for each downloaded artifact.</summary>
    public IReadOnlyList<EmbeddingArtifactReceipt> Artifacts => _artifacts;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(RootPath);
        var modelPath = Path.Combine(RootPath, "model.onnx");
        var vocabPath = Path.Combine(RootPath, "vocab.txt");

        _artifacts.Add(await DownloadAsync(ReadProductSource("ModelUrl"), modelPath).ConfigureAwait(false));
        _artifacts.Add(await DownloadAsync(ReadProductSource("VocabUrl"), vocabPath).ConfigureAwait(false));

        var logger = new EmbeddingFixtureLogger();
        Service = new EmbeddingService(
            new EmbeddingOptions
            {
                Enabled = true,
                ModelPath = modelPath,
                AutoDownload = false,
                Dimensions = 384,
            },
            logger);
        RequireAvailable(Service, logger.LastException, logger.LastMessage);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Service?.Dispose();
        _httpClient.Dispose();
        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, recursive: true);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// TR-MCP-EMBEDDING-001 unavailable-is-failure: rejects a missing, corrupt or otherwise unloadable model instead
    /// of allowing an embedding case to return successfully before its assertions.
    /// </summary>
    /// <param name="service">The explicitly configured embedding service.</param>
    /// <param name="loadException">The product load exception captured by the fixture logger, when present.</param>
    /// <param name="loadMessage">The product load message captured by the fixture logger, when present.</param>
    internal static void RequireAvailable(
        EmbeddingService service,
        Exception? loadException = null,
        string? loadMessage = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        if (!service.IsAvailable)
        {
            var details = string.IsNullOrWhiteSpace(loadMessage) ? string.Empty : $" Product log: {loadMessage}";
            throw new InvalidOperationException(
                $"The test-owned embedding model and vocabulary did not load.{details}",
                loadException);
        }
    }

    /// <summary>Reads one of the exact upstream source constants used by <see cref="EmbeddingService"/>.</summary>
    /// <param name="fieldName">The private product constant name.</param>
    /// <returns>The product source URI.</returns>
    private static Uri ReadProductSource(string fieldName)
    {
        var field = typeof(EmbeddingService).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"EmbeddingService source constant '{fieldName}' was not found.");
        var value = field.GetRawConstantValue() as string;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"EmbeddingService source constant '{fieldName}' is not an absolute URI.");
        return uri;
    }

    /// <summary>Streams one artifact to a new file under the fixture root and records its immutable evidence.</summary>
    /// <param name="source">The product source URI.</param>
    /// <param name="destination">The new file inside <see cref="RootPath"/>.</param>
    /// <returns>The recorded source, length and SHA-256.</returns>
    private async Task<EmbeddingArtifactReceipt> DownloadAsync(Uri source, string destination)
    {
        using var response = await _httpClient
            .GetAsync(source, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        await using (var input = await response.Content.ReadAsStreamAsync(CancellationToken.None).ConfigureAwait(false))
            await input.CopyToAsync(output, CancellationToken.None).ConfigureAwait(false);

        await using var artifact = new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(artifact, CancellationToken.None).ConfigureAwait(false));
        return new EmbeddingArtifactReceipt(source, artifact.Length, hash);
    }
}

/// <summary>TR-MCP-EMBEDDING-001 test-owned-fixture: immutable evidence for one downloaded fixture artifact.</summary>
/// <param name="Source">The exact product source URL.</param>
/// <param name="Length">The downloaded byte length.</param>
/// <param name="Sha256">The uppercase SHA-256 digest.</param>
public sealed record EmbeddingArtifactReceipt(Uri Source, long Length, string Sha256);

/// <summary>
/// TR-MCP-EMBEDDING-001 model-backed-assertions: resolves the ONNX Runtime native asset shipped under the test
/// output's runtime-specific folder before any test can initialize the dependency's native-method type.
/// </summary>
/// <remarks>
/// 2026-11-09: Microsoft.ML.OnnxRuntime 1.24.1 imports <c>onnxruntime.dll</c>; the portable Linux test host does not
/// map that name to its copied <c>runtimes/linux-*/native/libonnxruntime.so</c> asset without an explicit resolver.
/// The module initializer makes the positive fixture and corrupt-model control independent of xUnit execution order.
/// </remarks>
internal static class EmbeddingNativeRuntime
{
    /// <summary>Registers the integration-test assembly's resolver before xUnit constructs any test or fixture.</summary>
    [ModuleInitializer]
    internal static void Configure()
        => NativeLibrary.SetDllImportResolver(typeof(InferenceSession).Assembly, Resolve);

    /// <summary>Loads the package's native ONNX Runtime asset for the current supported host.</summary>
    /// <param name="libraryName">The unmanaged import name requested by ONNX Runtime.</param>
    /// <param name="assembly">The managed ONNX Runtime assembly requesting the import.</param>
    /// <param name="searchPath">The dependency's requested native library search path.</param>
    /// <returns>The loaded native handle, or zero for unrelated imports and unsupported hosts.</returns>
    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, "onnxruntime.dll", StringComparison.OrdinalIgnoreCase))
            return IntPtr.Zero;

        var architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => null,
        };
        var platform = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsLinux() ? "linux" : OperatingSystem.IsMacOS() ? "osx" : null;
        var fileName = OperatingSystem.IsWindows() ? "onnxruntime.dll" : OperatingSystem.IsLinux() ? "libonnxruntime.so" : "libonnxruntime.dylib";
        if (architecture is null || platform is null)
            return IntPtr.Zero;

        var path = Path.Combine(AppContext.BaseDirectory, "runtimes", $"{platform}-{architecture}", "native", fileName);
        return File.Exists(path) ? NativeLibrary.Load(path, assembly, searchPath) : IntPtr.Zero;
    }
}

/// <summary>
/// TR-MCP-EMBEDDING-001 unavailable-is-failure: captures the product's last load diagnostic so a fixture failure
/// preserves the actual ONNX or vocabulary exception instead of replacing it with a generic availability message.
/// </summary>
internal sealed class EmbeddingFixtureLogger : ILogger<EmbeddingService>
{
    /// <summary>Gets the exception from the most recent product log entry that carried one.</summary>
    internal Exception? LastException { get; private set; }

    /// <summary>Gets the formatted message from the most recent product log entry.</summary>
    internal string? LastMessage { get; private set; }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        LastMessage = formatter(state, exception);
        if (exception is not null)
            LastException = exception;
    }
}
