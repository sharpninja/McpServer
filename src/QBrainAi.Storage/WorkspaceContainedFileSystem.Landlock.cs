// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace QBrainAi.Support.Mcp.Storage;

/// <summary>
/// BUG-BASELINE-001 (TR-MCP-USECASE-017): kernel-enforced confinement for Linux export mutations that
/// have no contained-resolution primitive (directory creation and nested deletion). Each such mutation
/// runs on a dedicated thread that restricts itself with the Landlock LSM to the pinned hierarchy before
/// it touches the filesystem, so a parent moved outside that hierarchy makes the kernel deny the call
/// instead of letting it act outside. Where Landlock is unavailable callers keep the fail-closed refusal.
/// </summary>
public static partial class WorkspaceContainedFileSystem
{
    private const long LandlockCreateRulesetSystemCall = 444;
    private const long LandlockAddRuleSystemCall = 445;
    private const long LandlockRestrictSelfSystemCall = 446;
    private const uint LandlockCreateRulesetVersion = 1;
    private const int LandlockRulePathBeneath = 1;
    private const int PrSetNoNewPrivs = 38;

    private const ulong LandlockAccessWriteFile = 1UL << 1;
    private const ulong LandlockAccessRemoveDirectory = 1UL << 4;
    private const ulong LandlockAccessRemoveFile = 1UL << 5;
    private const ulong LandlockAccessMakeCharacter = 1UL << 6;
    private const ulong LandlockAccessMakeDirectory = 1UL << 7;
    private const ulong LandlockAccessMakeRegular = 1UL << 8;
    private const ulong LandlockAccessMakeSocket = 1UL << 9;
    private const ulong LandlockAccessMakeFifo = 1UL << 10;
    private const ulong LandlockAccessMakeBlock = 1UL << 11;
    private const ulong LandlockAccessMakeSymbolicLink = 1UL << 12;
    private const ulong LandlockAccessRefer = 1UL << 13;
    private const ulong LandlockAccessTruncate = 1UL << 14;

    // Test-only: replaces the detected Landlock ABI version (0 selects the fail-closed fallback).
    private static Func<int>? s_landlockAbiVersionOverride = null;

    // Test-only: runs on the confined thread, with the relative entry path, after the parent is pinned
    // and before the mutating system call.
    private static Action<string>? s_beforeLinuxConfinedMutation = null;

    /// <summary>
    /// Returns the Landlock ABI version the running kernel supports, or 0 when Landlock is unavailable
    /// (not Linux, not built in, or not enabled in the active LSM list).
    /// </summary>
    internal static int GetLandlockAbiVersion()
    {
        var overrideVersion = Volatile.Read(ref s_landlockAbiVersionOverride);
        if (overrideVersion is not null)
            return overrideVersion();
        if (!OperatingSystem.IsLinux())
            return 0;

        var version = LandlockQueryAbi(LandlockCreateRulesetSystemCall, IntPtr.Zero, 0, LandlockCreateRulesetVersion);
        return version > 0 ? (int)version : 0;
    }

    /// <summary>Invokes the confined-mutation test seam on the current (confined) thread.</summary>
    private static void InvokeBeforeLinuxConfinedMutation(string relativePath) =>
        Volatile.Read(ref s_beforeLinuxConfinedMutation)?.Invoke(relativePath);

    /// <summary>
    /// Runs <paramref name="operation"/> on a dedicated thread confined by Landlock so that it may create,
    /// remove, write, or truncate only beneath <paramref name="hierarchy"/>. The thread restricts itself before
    /// the operation starts and exits afterwards, so the restriction never reaches another operation.
    /// </summary>
    /// <typeparam name="T">Operation result type.</typeparam>
    /// <param name="hierarchy">Pinned directory descriptor whose subtree the operation may mutate.</param>
    /// <param name="operation">Synchronous filesystem operation.</param>
    /// <returns>The operation result.</returns>
    /// <exception cref="IOException">Landlock confinement could not be established.</exception>
    [SupportedOSPlatform("linux")]
    internal static T RunLandlockConfined<T>(SafeFileHandle hierarchy, Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(operation);
        var abi = GetLandlockAbiVersion();
        if (abi < 1)
            throw new IOException("Linux export confinement requires the Landlock LSM, which is unavailable.");

        var handled = LandlockAccessWriteFile | LandlockAccessRemoveDirectory | LandlockAccessRemoveFile |
                      LandlockAccessMakeCharacter | LandlockAccessMakeDirectory | LandlockAccessMakeRegular |
                      LandlockAccessMakeSocket | LandlockAccessMakeFifo | LandlockAccessMakeBlock |
                      LandlockAccessMakeSymbolicLink;
        if (abi >= 2)
            handled |= LandlockAccessRefer;
        if (abi >= 3)
            handled |= LandlockAccessTruncate;

        T? result = default;
        ExceptionDispatchInfo? failure = null;
        var added = false;
        hierarchy.DangerousAddRef(ref added);
        try
        {
            var thread = new Thread(
                () =>
                {
                    try
                    {
                        ConfineCurrentThread(hierarchy, handled);
                        result = operation();
                    }
                    catch (Exception exception)
                    {
                        failure = ExceptionDispatchInfo.Capture(exception);
                    }
                })
            {
                IsBackground = true,
                Name = "McpLandlockConfinedMutation",
            };
            thread.Start();
            thread.Join();
        }
        finally
        {
            if (added)
                hierarchy.DangerousRelease();
        }

        failure?.Throw();
        return result!;
    }

    [SupportedOSPlatform("linux")]
    private static void ConfineCurrentThread(SafeFileHandle hierarchy, ulong handled)
    {
        var attributes = new LandlockRulesetAttributes { HandledAccessFileSystem = handled };
        var ruleset = LandlockCreateRuleset(
            LandlockCreateRulesetSystemCall,
            ref attributes,
            (nuint)sizeof(ulong),
            0);
        if (ruleset < 0)
            throw ConfinementFailure("create the Landlock ruleset");

        try
        {
            var rule = new LandlockPathBeneathAttributes
            {
                AllowedAccess = handled,
                ParentDescriptor = hierarchy.DangerousGetHandle().ToInt32(),
            };
            if (LandlockAddRule(LandlockAddRuleSystemCall, (int)ruleset, LandlockRulePathBeneath, ref rule, 0) != 0)
                throw ConfinementFailure("add the export hierarchy rule");
            if (PrctlUnix(PrSetNoNewPrivs, 1, 0, 0, 0) != 0)
                throw ConfinementFailure("set no_new_privs");
            if (LandlockRestrictSelf(LandlockRestrictSelfSystemCall, (int)ruleset, 0) != 0)
                throw ConfinementFailure("restrict the mutation thread");
        }
        finally
        {
            _ = CloseUnix((int)ruleset);
        }
    }

    private static IOException ConfinementFailure(string step) =>
        new($"Linux export confinement could not {step}.", new Win32Exception(Marshal.GetLastPInvokeError()));

    [StructLayout(LayoutKind.Sequential)]
    private struct LandlockRulesetAttributes
    {
        internal ulong HandledAccessFileSystem;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct LandlockPathBeneathAttributes
    {
        internal ulong AllowedAccess;
        internal int ParentDescriptor;
    }

    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static extern long LandlockQueryAbi(long number, IntPtr attributes, nuint size, uint flags);

    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static extern long LandlockCreateRuleset(
        long number,
        ref LandlockRulesetAttributes attributes,
        nuint size,
        uint flags);

    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static extern long LandlockAddRule(
        long number,
        int rulesetDescriptor,
        int ruleType,
        ref LandlockPathBeneathAttributes attributes,
        uint flags);

    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static extern long LandlockRestrictSelf(long number, int rulesetDescriptor, uint flags);

    [DllImport("libc", EntryPoint = "prctl", SetLastError = true)]
    private static extern int PrctlUnix(int option, ulong argument2, ulong argument3, ulong argument4, ulong argument5);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int CloseUnix(int descriptor);
}
