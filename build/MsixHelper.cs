/// <summary>
/// Utilities for MSIX packaging: SDK tool resolution and AppxManifest generation.
/// Ported from scripts/Package-McpServerMsix.ps1.
/// </summary>
static class MsixHelper
{
    private static readonly string WindowsKitsRoot = @"C:\Program Files (x86)\Windows Kits\10\bin";

    /// <summary>
    /// Searches for a Windows SDK tool (makeappx.exe, signtool.exe) on PATH
    /// and in the Windows 10 SDK installation directory.
    /// </summary>
    public static string? FindSdkTool(string toolName)
    {
        // Check PATH first
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        foreach (var dir in pathDirs)
        {
            var candidate = Path.Combine(dir, toolName);
            if (File.Exists(candidate))
                return candidate;
        }

        // Search Windows SDK directories
        if (!Directory.Exists(WindowsKitsRoot))
            return null;

        return Directory.EnumerateFiles(WindowsKitsRoot, toolName, SearchOption.AllDirectories)
            .Where(f => f.Contains(@"\x64\", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f)
            .FirstOrDefault();
    }

    /// <summary>
    /// Generates AppxManifest.xml content for the MSIX package.
    /// </summary>
    public static string GenerateManifest(string packageName, string publisher, string version)
    {
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
                     xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
                     xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
                     IgnorableNamespaces="uap rescap">
              <Identity Name="{packageName}" Publisher="{publisher}" Version="{version}" />
              <Properties>
                <DisplayName>{packageName}</DisplayName>
                <PublisherDisplayName>FunWasHad</PublisherDisplayName>
                <Logo>Square44x44Logo.png</Logo>
              </Properties>
              <Dependencies>
                <TargetDeviceFamily Name="Windows.Universal" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22631.0" />
              </Dependencies>
              <Resources>
                <Resource Language="en-us" />
              </Resources>
              <Capabilities>
                <rescap:Capability Name="runFullTrust" />
              </Capabilities>
              <Applications>
                <Application Id="McpServer" Executable="McpServer.Support.Mcp.exe" EntryPoint="Windows.FullTrustApplication">
                  <uap:VisualElements DisplayName="{packageName}" Square44x44Logo="Square44x44Logo.png" Square150x150Logo="Square150x150Logo.png" Description="FunWasHad MCP Server" BackgroundColor="transparent" />
                </Application>
              </Applications>
            </Package>
            """;
    }

    /// <summary>
    /// Creates a 1x1 transparent PNG placeholder for required MSIX logo assets.
    /// </summary>
    public static byte[] CreatePlaceholderPng()
    {
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5oY0QAAAAASUVORK5CYII=");
    }
}
