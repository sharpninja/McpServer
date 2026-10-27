// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using QBrainAi.Support.Mcp.Storage;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// BUG-BASELINE-008: the Linux-native requirements export case that the BUG-TRIAGE-139 harness inventories require
/// (BugTriage139FourteenthHarnessTests expects this type with one test, BugTriage139FifteenthHarnessTests expects the method in the
/// Linux inventory). It was named there but never committed. Compiled only on Linux: QBrainAi.Support.Mcp.Tests.csproj removes it on
/// Windows. Uses a temporary root holding a named pipe; no mocks.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class RequirementsFourteenthLinuxNativeTests
{
    /// <summary>
    /// A contained read of a FIFO that has no writer is rejected within a bound instead of blocking in open(2):
    /// WorkspaceContainedFileSystem opens with O_NONBLOCK and then requires an ordinary regular file. The open runs on a worker with a
    /// 5 s limit so a regression fails the test instead of hanging it; on that path a writer is opened to release the blocked reader.
    /// </summary>
    [Fact]
    public async Task OpenFileForRead_LinuxFifoWithoutWriter_IsRejectedWithinBound()
    {
        var root = Path.Combine(Path.GetTempPath(), "requirements-fourteenth-fifo", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var fifo = Path.Combine(root, "requirements.md");
        Assert.Equal(0, CreateFifo(fifo, Convert.ToUInt32("600", 8)));
        var open = Task.Run(() => WorkspaceContainedFileSystem.OpenFileForRead(root, fifo).Dispose(), TestContext.Current.CancellationToken);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var exception = await Assert.ThrowsAsync<IOException>(() => open.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Opening the FIFO took {stopwatch.Elapsed}.");
            Assert.Contains("not an ordinary regular file", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (!open.IsCompleted)
            {
                // A racing reader can finish between IsCompleted and cleanup. Never block opening its writer.
                var writer = OpenFifoNonBlocking(fifo, 1 | 0x800); // O_WRONLY | O_NONBLOCK
                if (writer >= 0)
                    _ = CloseDescriptor(writer);
                _ = await Task.WhenAny(open, Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None));
            }

            Directory.Delete(root, recursive: true);
        }
    }

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int OpenFifoNonBlocking([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int CloseDescriptor(int descriptor);
    [DllImport("libc", EntryPoint = "mkfifo", SetLastError = true)]
    private static extern int CreateFifo(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        uint mode);
}
