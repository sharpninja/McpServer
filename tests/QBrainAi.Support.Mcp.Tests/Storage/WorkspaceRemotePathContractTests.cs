using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-PATHNORM-001 / TR-MCP-PATHNORM-001 (plan P1.4b): separator-agnostic Windows path
/// predicates. A remote UNC path is rejected before any native open whichever separator spells it
/// (the canonical form is <c>//server/share</c>), and the local administrative share alias resolves to
/// the drive form from either spelling. BDP v4 (plan section 2a): MockData fixture then Real fixture.
/// </summary>
public abstract class WorkspaceRemotePathContractTests
{
    /// <summary>Resolves a Windows-platform workspace path to its canonical normalized form.</summary>
    protected abstract Task<string> ResolveWindowsAsync(string path);

    /// <summary>Remote UNC paths are not supported for workspace identity, in either spelling.</summary>
    [Theory]
    [InlineData(@"\\192.0.2.1\pathnorm-share\child")]
    [InlineData("//192.0.2.1/pathnorm-share/child")]
    public async Task RemoteUnc_EitherSpelling_IsRejected(string path)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows physical resolution only runs on a Windows host.");

        await Assert.ThrowsAsync<NotSupportedException>(() => ResolveWindowsAsync(path));
    }

    /// <summary>The local administrative share alias resolves to the drive form from either spelling.</summary>
    [Theory]
    [InlineData(@"\\localhost\C$\Windows")]
    [InlineData("//localhost/C$/Windows")]
    public async Task LocalAdministrativeShare_EitherSpelling_ResolvesToDrive(string path)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows physical resolution only runs on a Windows host.");

        Assert.Equal("C:/Windows", await ResolveWindowsAsync(path), ignoreCase: true);
    }
}

/// <summary>BDP v4 step 2: the remote-path contract against scripted outcomes.</summary>
public sealed class WorkspaceRemotePathMockDataTests : WorkspaceRemotePathContractTests
{
    private static readonly Dictionary<string, string?> Outcomes = new(StringComparer.Ordinal)
    {
        [@"\\192.0.2.1\pathnorm-share\child"] = null,
        ["//192.0.2.1/pathnorm-share/child"] = null,
        [@"\\localhost\C$\Windows"] = "C:/Windows",
        ["//localhost/C$/Windows"] = "C:/Windows",
    };

    /// <inheritdoc />
    protected override Task<string> ResolveWindowsAsync(string path)
    {
        if (!Outcomes.TryGetValue(path, out var resolved))
            throw new InvalidOperationException($"No scripted resolution for '{path}'.");

        return resolved is null
            ? Task.FromException<string>(new NotSupportedException($"Remote path '{path}' is not supported."))
            : Task.FromResult(resolved);
    }
}

/// <summary>BDP v4 step 3: the remote-path contract against <see cref="WorkspaceIdentityPath"/>.</summary>
public sealed class WorkspaceRemotePathRealTests : WorkspaceRemotePathContractTests
{
    /// <inheritdoc />
    protected override async Task<string> ResolveWindowsAsync(string path)
    {
        var result = await WorkspaceIdentityPath.ResolveAsync(
                path,
                WorkspacePathPlatform.Windows,
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        return result.NormalizedPath;
    }
}
