using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tooling;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("MSIX package version (e.g. 1.0.0.0)")]
    readonly string MsixVersion = "1.0.0.0";

    [Parameter("MSIX publisher identity")]
    readonly string Publisher = "CN=FunWasHad";

    [Parameter("Code signing certificate path")]
    readonly string CertificatePath = string.Empty;

    [Parameter("Code signing certificate password")]
    readonly string CertificatePassword = string.Empty;

    /// <summary>Package McpServer.Support.Mcp as a Windows MSIX installer.</summary>
    public Target PackageMsix => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var project = SourceDirectory / "McpServer.Support.Mcp" / "McpServer.Support.Mcp.csproj";
            var publishDir = ArtifactsDirectory / "mcp-msix-publish";
            var stagingDir = ArtifactsDirectory / "mcp-msix-staging";
            var outputDir = ArtifactsDirectory / "msix";

            publishDir.CreateOrCleanDirectory();
            stagingDir.CreateOrCleanDirectory();
            outputDir.CreateDirectory();

            DotNetPublish(_ => _
                .SetProject(project)
                .SetConfiguration(Configuration)
                .SetOutput(publishDir));

            // Copy publish output to staging
            publishDir.Copy(stagingDir, Nuke.Common.IO.ExistsPolicy.MergeAndOverwrite);

            // Generate manifest
            var manifestContent = MsixHelper.GenerateManifest("McpServer.Support.Mcp", Publisher, MsixVersion);
            File.WriteAllText(stagingDir / "AppxManifest.xml", manifestContent);

            // Create placeholder logos if missing
            var placeholderPng = MsixHelper.CreatePlaceholderPng();
            var logo44 = stagingDir / "Square44x44Logo.png";
            var logo150 = stagingDir / "Square150x150Logo.png";
            if (!File.Exists(logo44)) File.WriteAllBytes(logo44, placeholderPng);
            if (!File.Exists(logo150)) File.WriteAllBytes(logo150, placeholderPng);

            // Find and run makeappx
            var makeAppx = MsixHelper.FindSdkTool("makeappx.exe")
                ?? throw new InvalidOperationException("makeappx.exe not found. Install Windows SDK.");

            var msixPath = outputDir / $"McpServer.Support.Mcp-{MsixVersion}.msix";
            Log.Information("Creating MSIX: {Path}", msixPath);

            ProcessTasks.StartProcess(makeAppx, $"pack /d \"{stagingDir}\" /p \"{msixPath}\" /o")
                .AssertZeroExitCode();

            // Optional signing
            if (!string.IsNullOrWhiteSpace(CertificatePath))
            {
                if (string.IsNullOrWhiteSpace(CertificatePassword))
                    throw new InvalidOperationException("CertificatePassword is required when CertificatePath is provided.");

                var signtool = MsixHelper.FindSdkTool("signtool.exe")
                    ?? throw new InvalidOperationException("signtool.exe not found. Install Windows SDK.");

                Log.Information("Signing MSIX...");
                ProcessTasks.StartProcess(signtool,
                    $"sign /fd SHA256 /f \"{CertificatePath}\" /p \"{CertificatePassword}\" \"{msixPath}\"")
                    .AssertZeroExitCode();
            }

            Log.Information("MSIX package ready: {Path}", msixPath);
        });
}
