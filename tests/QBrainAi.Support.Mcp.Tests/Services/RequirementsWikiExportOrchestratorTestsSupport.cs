using System.Diagnostics;
using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Requirements.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-DOCFXWIKI-001: the helpers that both halves of RequirementsWikiExportOrchestratorTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-DOCFXWIKI-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from RequirementsWikiExportOrchestratorTests (RecordingDocFxWorkflowRunner and RequirementsWikiWorkspace) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class RequirementsWikiExportOrchestratorTestsSupport
{
    /// <summary>A DocFX workflow runner double that counts its calls and returns the scripted documents, or throws the scripted failure.</summary>
    /// <remarks>2026-11-07, TEST-MCP-DOCFXWIKI-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: summary added (slice S8, hostile validation finding R14-F4): this helper type became internal when slice S7 moved it into this shared support file, so it is documented like the other shared members; its code is unchanged.</remarks>
    internal sealed class RecordingDocFxWorkflowRunner : IRequirementsDocFxWorkflowRunner
    {
        public int CallCount { get; private set; }

        public IReadOnlyList<RequirementsRenderedDocument> Documents { get; init; } = [];

        public Exception? Failure { get; init; }

        public Task<IReadOnlyList<RequirementsRenderedDocument>> RunAsync(RequirementsWikiExportConfig config, CancellationToken ct = default)
        {
            CallCount++;
            if (Failure is not null)
                throw Failure;

            return Task.FromResult(Documents);
        }
    }

    /// <summary>A temporary workspace folder with a docs/Project folder, whose CreateOptions points the requirements documents into it.</summary>
    /// <remarks>2026-11-07, TEST-MCP-DOCFXWIKI-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: summary added (slice S8, hostile validation finding R14-F4): this helper type became internal when slice S7 moved it into this shared support file, so it is documented like the other shared members; its code is unchanged.</remarks>
    internal sealed class RequirementsWikiWorkspace : IDisposable
    {
        public RequirementsWikiWorkspace()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mcp-wiki-orchestrator-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "docs", "Project"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "docs"));
        }

        public string Path { get; }

        public RequirementsOptions CreateOptions() => new()
        {
            FunctionalRequirementsPath = System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.FunctionalFileName),
            TechnicalRequirementsPath = System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.TechnicalFileName),
            TestingRequirementsPath = System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.TestingFileName),
            MappingPath = System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.MappingFileName),
            MatrixPath = System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.MatrixFileName),
            WikiConfigPath = System.IO.Path.Combine(Path, "docs", "wiki.yaml")
        };

        public void SeedCanonicalDocs()
        {
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.FunctionalFileName), """
                # Functional Requirements (QBrain.AI)

                ## FR-MCP-DOCFX-001 DocFX wiki export

                Export DocFX output into wiki artifacts.
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.TechnicalFileName), """
                # Technical Requirements (QBrain.AI)

                ## TR-MCP-DOCFX-001

                Shared orchestrator merges generated and DocFX wiki documents.
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.TestingFileName), """
                # Testing Requirements (QBrain.AI)

                ### TEST-MCP-DOCFX-001

                Given DocFX wiki export, when generated, then shared orchestration is used.
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.MappingFileName), """
                # TR per FR Mapping (QBrain.AI)

                | FR | Primary TRs | Tests |
                | --- | --- | --- |
                | FR-MCP-DOCFX-001 | TR-MCP-DOCFX-001 | TEST-MCP-DOCFX-001 |
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.MatrixFileName), """
                # Requirements Matrix (QBrain.AI)

                | ID | Status | Source |
                | --- | --- | --- |
                | EXISTING | Existing matrix row | Manual |
                """);
        }

        public void WriteWikiConfigWithoutDocFx()
        {
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "wiki.yaml"), """
                schema: mcp-wiki-export/v1
                documents:
                - id: home
                  title: Home
                  source: generated:home
                  target: Home.md
                navigation:
                - document: home
                """);
        }

        public RequirementsWikiExportRequest CreateRequest(string outputRoot) =>
            new(
                outputRoot,
                new DateTimeOffset(2026, 7, 13, 23, 0, 0, TimeSpan.Zero),
                Path,
                CreateOptions(),
                [new FrEntry("FR-MCP-DOCFX-001", "DocFX wiki export", "Export DocFX output into wiki artifacts.")],
                [new TrEntry("TR-MCP-DOCFX-001", "Shared orchestrator", "Shared orchestrator merges generated and DocFX wiki documents.")],
                [new TestEntry("TEST-MCP-DOCFX-001", "Given DocFX wiki export, when generated, then shared orchestration is used.")],
                [new FrTrMapping("FR-MCP-DOCFX-001", ["TR-MCP-DOCFX-001"], ["TEST-MCP-DOCFX-001"])],
                null);

        public void SeedDocFxProject()
        {
            Directory.CreateDirectory(System.IO.Path.Combine(Path, ".config"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "docs", "docfx", "resources"));
            File.WriteAllText(System.IO.Path.Combine(Path, ".config", "dotnet-tools.json"), """
                {
                  "version": 1,
                  "isRoot": true,
                  "tools": {
                    "docfx": {
                      "version": "2.78.3",
                      "commands": [
                        "docfx"
                      ]
                    }
                  }
                }
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "docfx", "docfx.json"), """
                {
                  "build": {
                    "content": [
                      {
                        "files": [
                          "index.md",
                          "guide.md"
                        ]
                      }
                    ],
                    "resource": [
                      {
                        "files": [
                          "resources/raw-note.md"
                        ]
                      }
                    ],
                    "dest": "_site",
                    "globalMetadata": {
                      "_appTitle": "MCP DocFX Scratch"
                    }
                  }
                }
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "docfx", "index.md"), """
                # Scratch DocFX Home

                This page was built by DocFX.
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "docfx", "guide.md"), """
                # Scratch DocFX Guide

                This guide page was built by DocFX.
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "docfx", "resources", "raw-note.md"), """
                # Raw copied markdown

                Copied as a DocFX resource.
                """);
        }

        public void WriteWikiConfigWithDocFx(IReadOnlyList<string>? platforms = null, IReadOnlyList<string>? arguments = null)
        {
            platforms ??= ["github", "azure"];
            arguments ??= ["docfx", "docfx.json"];
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "docs", "docfx"));
            var platformLines = string.Join(Environment.NewLine, platforms.Select(platform => $"    - {platform}"));
            var argumentLines = string.Join(Environment.NewLine, arguments.Select(argument => $"    - {argument}"));
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "wiki.yaml"), $$"""
                schema: mcp-wiki-export/v1
                docfx:
                  workflows:
                  - id: api
                    executable: dotnet
                    arguments:
                {{argumentLines}}
                    workingDirectory: docs/docfx
                    outputRoot: docs/docfx/_site
                    targetRoot: api
                    platforms:
                {{platformLines}}
                    timeoutSeconds: 120
                documents:
                - id: home
                  title: Home
                  source: generated:home
                  target: Home.md
                  platforms:
                  - github
                  - azure
                navigation:
                - document: home
                """);
        }

        public void Dispose()
        {
            ClearReadOnly(Path);
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }

        private static void ClearReadOnly(string root)
        {
            if (!Directory.Exists(root))
                return;

            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var attributes = File.GetAttributes(file);
                if (attributes.HasFlag(FileAttributes.ReadOnly))
                    File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
            }
        }
    }
}
