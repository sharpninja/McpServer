using System.Security.Cryptography;
using System.Text.Json;

namespace McpServer.Support.Mcp.Services;

internal static class WindowsServiceDeploymentGuard
{
    internal static void EnsureApprovedDeployment(string baseDirectory, Action<string>? logFailure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var yamlPath = Path.Combine(baseDirectory, "appsettings.yaml");
        var jsonPath = Path.Combine(baseDirectory, "appsettings.json");
        var manifestPath = Path.Combine(baseDirectory, ".mcpservice-deployment.json");

        if (!File.Exists(yamlPath))
            Fail("Windows service deployment is missing appsettings.yaml. Redeploy with scripts\\Update-McpService.ps1.");

        if (File.Exists(jsonPath))
            Fail("Legacy appsettings.json was found in the Windows service install directory. Remove it and redeploy with scripts\\Update-McpService.ps1.");

        if (!File.Exists(manifestPath))
            Fail("Windows service deployment is missing .mcpservice-deployment.json. Redeploy with scripts\\Update-McpService.ps1.");

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (!document.RootElement.TryGetProperty("generatedBy", out var generatedByElement) ||
                generatedByElement.ValueKind != JsonValueKind.String ||
                !string.Equals(generatedByElement.GetString(), "scripts\\Update-McpService.ps1", StringComparison.Ordinal))
            {
                Fail("Windows service deployment was not prepared by scripts\\Update-McpService.ps1. Redeploy with that script.");
            }

            if (!document.RootElement.TryGetProperty("executableHashes", out var hashesElement) ||
                hashesElement.ValueKind != JsonValueKind.Array)
            {
                Fail("Windows service deployment manifest is missing executable hashes. Redeploy with scripts\\Update-McpService.ps1.");
            }

            var expectedHashes = hashesElement.EnumerateArray()
                .Select(static element => new
                {
                    Name = element.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                        ? nameElement.GetString()
                        : null,
                    Sha256 = element.TryGetProperty("sha256", out var hashElement) && hashElement.ValueKind == JsonValueKind.String
                        ? hashElement.GetString()
                        : null,
                })
                .Where(static entry => !string.IsNullOrWhiteSpace(entry.Name) && !string.IsNullOrWhiteSpace(entry.Sha256))
                .ToDictionary(
                    static entry => entry.Name!,
                    static entry => entry.Sha256!,
                    StringComparer.OrdinalIgnoreCase);

            if (expectedHashes.Count == 0)
                Fail("Windows service deployment manifest does not contain any executable hashes. Redeploy with scripts\\Update-McpService.ps1.");

            var actualExecutables = Directory.GetFiles(baseDirectory, "*.exe", SearchOption.TopDirectoryOnly)
                .Select(path => new
                {
                    Name = Path.GetFileName(path),
                    Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant(),
                })
                .ToDictionary(static entry => entry.Name, static entry => entry.Sha256, StringComparer.OrdinalIgnoreCase);

            if (expectedHashes.Count != actualExecutables.Count)
                Fail("Windows service deployment manifest does not match the deployed executable set. Redeploy with scripts\\Update-McpService.ps1.");

            foreach (var expectedHash in expectedHashes)
            {
                if (!actualExecutables.TryGetValue(expectedHash.Key, out var actualHash) ||
                    !string.Equals(actualHash, expectedHash.Value, StringComparison.OrdinalIgnoreCase))
                {
                    Fail($"Windows service deployment manifest hash mismatch for '{expectedHash.Key}'. Redeploy with scripts\\Update-McpService.ps1.");
                }
            }
        }
        catch (JsonException exception)
        {
            Fail($"Windows service deployment manifest is invalid: {exception.Message}", exception);
        }

        void Fail(string message, Exception? innerException = null)
        {
            logFailure?.Invoke(message);
            throw new InvalidOperationException(message, innerException);
        }
    }
}
