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

    /// <summary>
    /// TR-MCP-QBRAIN-006: Keeps an existing McpServer Windows service and install directory
    /// when the caller is still on the QBrainAi defaults. Explicit parameter overrides are left unchanged.
    /// The executable installed into that directory is always the current QBrainAi host.
    /// </summary>
    /// <param name="requestedServiceName">Nuke service-name parameter.</param>
    /// <param name="requestedInstallPath">Nuke install-path parameter.</param>
    /// <param name="canonicalServiceExists">True when the QBrainAi service is already registered.</param>
    /// <param name="legacyServiceExists">True when the McpServer service is already registered.</param>
    /// <param name="canonicalInstallExists">True when C:\ProgramData\QBrainAi exists.</param>
    /// <param name="legacyInstallExists">True when C:\ProgramData\McpServer exists.</param>
    /// <returns>The service and directory that should be updated in place.</returns>
    public static WindowsInstallIdentity ResolveExistingWindowsInstall(
        string requestedServiceName,
        string requestedInstallPath,
        bool canonicalServiceExists,
        bool legacyServiceExists,
        bool canonicalInstallExists,
        bool legacyInstallExists)
    {
        const string canonicalService = "QBrainAi";
        const string legacyService = "McpServer";
        const string canonicalInstall = @"C:\ProgramData\QBrainAi";
        const string legacyInstall = @"C:\ProgramData\McpServer";
        var serviceName = requestedServiceName;
        var installPath = requestedInstallPath;
        var usingDefaults = string.Equals(requestedServiceName, canonicalService, StringComparison.OrdinalIgnoreCase)
            && string.Equals(TrimDirectory(requestedInstallPath), canonicalInstall, StringComparison.OrdinalIgnoreCase);
        if (usingDefaults)
        {
            if (canonicalServiceExists)
            {
                serviceName = canonicalService;
                installPath = canonicalInstall;
            }
            else if (legacyServiceExists)
            {
                serviceName = legacyService;
                installPath = legacyInstall;
            }
            else if (!canonicalInstallExists && legacyInstallExists)
            {
                installPath = legacyInstall;
            }
        }

        return new WindowsInstallIdentity(serviceName, installPath, "QBrainAi.Support.Mcp.exe");
    }

    private static string TrimDirectory(string path)
        => path.Trim().TrimEnd('\\', '/');
}

/// <summary>TR-MCP-QBRAIN-006: Windows service identity selected for an in-place update.</summary>
/// <param name="ServiceName">Service registration to update.</param>
/// <param name="InstallPath">Directory that receives the new host.</param>
/// <param name="ExecutableName">Current host file name.</param>
internal readonly record struct WindowsInstallIdentity(string ServiceName, string InstallPath, string ExecutableName);
