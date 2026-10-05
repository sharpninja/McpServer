using System.Runtime.InteropServices;

/// <summary>TR-MCP-SERVICEUPDATE-001: deployment defaults resolved before invoking platform-specific APIs.</summary>
internal sealed record ServiceUpdatePlatform(bool IsWindows, string DefaultServiceName, string DefaultInstallPath,
    string ExecutableName, string RuntimeIdentifier)
{
    /// <summary>Resolves the actual build host.</summary>
    public static ServiceUpdatePlatform Current => Resolve(OperatingSystem.IsWindows(), OperatingSystem.IsLinux(), RuntimeInformation.OSArchitecture);

    /// <summary>Nonthrowing parameter defaults for all Nuke targets; deployment compatibility is checked on execution.</summary>
    public static ServiceUpdatePlatform ParameterDefaults(bool linux) => Resolve(!linux, linux, Architecture.X64);

    /// <summary>Retains Windows x64 compatibility and supports Linux x64/arm64; rejects unsupported hosts.</summary>
    public static ServiceUpdatePlatform Resolve(bool windows, bool linux, Architecture architecture)
    {
        if (windows && !linux && architecture is Architecture.X64 or Architecture.Arm64)
            return new(true, "QBrainAi", @"C:\ProgramData\QBrainAi", "QBrainAi.Support.Mcp.exe", "win-x64");
        if (linux && !windows && architecture is Architecture.X64 or Architecture.Arm64)
            return new(false, "mcpserver.service", "/opt/mcpserver/app", "QBrainAi.Support.Mcp",
                architecture == Architecture.X64 ? "linux-x64" : "linux-arm64");
        throw new PlatformNotSupportedException("UpdateService supports Windows or Linux x64/arm64.");
    }
}
