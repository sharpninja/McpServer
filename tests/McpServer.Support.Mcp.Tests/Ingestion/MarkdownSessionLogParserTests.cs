using McpServer.Support.Mcp.Ingestion;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>TR-PLANNED-013: Unit tests for MarkdownSessionLogParser.</summary>
public sealed class MarkdownSessionLogParserTests
{
    [Fact]
    public void WhenValidSessionLog_ThenParsesCorrectly()
    {
        var md = """
            # Copilot Session Log - FTS5 Implementation

            **Date:** 2026-02-16
            **Duration:** ~3 hours
            **Branch:** feature/fts5
            **Status:** ✅ Complete

            ## 1. Session Overview
            Implemented FTS5 full-text search for MCP context.

            ## 2. Changes Made
            - Created Fts5SearchService
            - Updated ContextController
            - Added migration
            """;

        var result = MarkdownSessionLogParser.TryParse(md, "copilot-SESSION-LOG-2026-02-16.md");

        Assert.NotNull(result);
        Assert.Equal("copilot", result!.SourceType);
        Assert.Contains("FTS5 Implementation", result.Title, StringComparison.Ordinal);
        Assert.Equal("Complete", result.Status);
        Assert.NotNull(result.Turns);
        Assert.Single(result.Turns!);
    }

    [Fact]
    public void WhenNotASessionLog_ThenReturnsNull()
    {
        var md = """
            # Regular Document

            This is not a session log, just some markdown.
            """;

        var result = MarkdownSessionLogParser.TryParse(md, "readme.md");

        Assert.Null(result);
    }

    [Fact]
    public void WhenEmptyContent_ThenReturnsNull()
    {
        Assert.Null(MarkdownSessionLogParser.TryParse("", "test.md"));
        Assert.Null(MarkdownSessionLogParser.TryParse("  ", "test.md"));
    }

    [Fact]
    public void WhenChangesExist_ThenExtractsActions()
    {
        var md = """
            # Session Log - Test Changes

            **Date:** 2026-02-16
            **Status:** 🚧 In Progress

            ## 3. Changes Made
            - Created new service
            - Updated controller
            * Fixed bug in parser

            ## 4. Testing
            All tests pass.
            """;

        var result = MarkdownSessionLogParser.TryParse(md, "cursor-SESSION-LOG-2026-02-16.md");

        Assert.NotNull(result);
        Assert.Equal("cursor", result!.SourceType);
        var entry = result.Turns![0];
        Assert.Equal(3, entry.Actions!.Count);
        Assert.Equal("Created new service", entry.Actions[0].Description);
    }

    [Fact]
    public void WhenBranchProvided_ThenWorkspaceContainsBranch()
    {
        var md = """
            # Session Log - Branch Test

            **Date:** 2026-02-16
            **Branch:** feature/test-branch
            **Status:** Complete

            ## Session Overview
            Testing branch extraction.
            """;

        var result = MarkdownSessionLogParser.TryParse(md, "copilot-SESSION-LOG-2026-02-16.md");

        Assert.NotNull(result);
        Assert.Equal("feature/test-branch", result!.Workspace?.Branch);
    }

    [Fact]
    public void WhenDateProvided_ThenStartedIsPopulated()
    {
        var md = """
            # Session Log - Date Test

            **Date:** 2026-02-16
            **Status:** Complete
            """;

        var result = MarkdownSessionLogParser.TryParse(md, "copilot-test.md");

        Assert.NotNull(result);
        Assert.NotNull(result!.Started);
        Assert.Contains("2026-02-16", result.Started, StringComparison.Ordinal);
    }

    // --- Phase 4b: Enhanced parser tests ---

    [Fact]
    public void WhenModelProvided_ThenModelIsExtracted()
    {
        var md = """
            # Session Log - Model Test

            **Date:** 2026-02-16
            **Model:** claude-sonnet-4
            **Status:** Complete
            """;

        var result = MarkdownSessionLogParser.TryParse(md, "copilot-test.md");

        Assert.NotNull(result);
        Assert.Equal("claude-sonnet-4", result!.Model);
        // Model should also be on the summary entry
        Assert.Equal("claude-sonnet-4", result.Turns![0].Model);
    }

    [Fact]
    public void WhenDurationProvided_ThenDurationIsInResponse()
    {
        var md = """
            # Session Log - Duration Test

            **Date:** 2026-02-16
            **Duration:** ~5 hours
            **Status:** Complete

            ## Session Overview
            Testing duration extraction.
            """;

        var result = MarkdownSessionLogParser.TryParse(md, "copilot-test.md");

        Assert.NotNull(result);
        var summaryEntry = result!.Turns![0];
        Assert.Contains("Duration: ~5 hours", summaryEntry.Response, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenFilesSummaryProvided_ThenSectionIsInResponse()
    {
        var md = """
            # Session Log - Files Summary Test

            **Date:** 2026-02-16
            **Status:** Complete

            ## 7. Files Summary
            - Source Code: 5 files
            - Tests: 3 files
            - Documentation: 2 files
            """;

        var result = MarkdownSessionLogParser.TryParse(md, "copilot-test.md");

        Assert.NotNull(result);
        var summaryEntry = result!.Turns![0];
        Assert.Contains("Files Summary", summaryEntry.Response, StringComparison.Ordinal);
        Assert.Contains("Source Code: 5 files", summaryEntry.Response, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenMultipleRequestEntries_ThenAllAreExtracted()
    {
        var md = """
            # Session Log - Multi Request Test

            **Date:** 2026-02-16
            **Model:** gpt-4o
            **Status:** Complete

            ## Session Overview
            Testing multi-entry extraction.

            ### Request 1: Fix parser bug
            Fixed the regex pattern for title matching.

            ### Request 2: Add unit tests
            Added 5 new unit tests for the parser.

            ### Request 3: Update documentation
            Updated the README with new API endpoints.
            """;

        var result = MarkdownSessionLogParser.TryParse(md, "copilot-SESSION-LOG-2026-02-16.md");

        Assert.NotNull(result);
        // 1 summary + 3 request turns
        Assert.Equal(4, result!.Turns!.Count);
        Assert.Equal("Session Summary", result.Turns[0].QueryTitle);
        Assert.Contains("1: Fix parser bug", result.Turns[1].QueryTitle, StringComparison.Ordinal);
        Assert.Contains("2: Add unit tests", result.Turns[2].QueryTitle, StringComparison.Ordinal);
        Assert.Contains("3: Update documentation", result.Turns[3].QueryTitle, StringComparison.Ordinal);
        Assert.Contains("Fixed the regex pattern", result.Turns[1].Response, StringComparison.Ordinal);
        Assert.Equal("gpt-4o", result.Turns[1].Model);
    }

    [Fact]
    public void WhenNumberedSections_ThenAllSectionsAreExtracted()
    {
        var md = """
            # Session Log - Numbered Sections Test

            **Date:** 2026-02-16
            **Status:** Complete

            ## 1. Session Overview
            Overview content here.

            ## 2. Changes Made
            - Change 1
            - Change 2

            ## 3. Technical Requirements
            - TR-001: New requirement

            ## 4. Testing
            All 15 tests passing.

            ## 5. Documentation
            Updated Technical-Requirements.md

            ## 6. Files Summary
            - Source: 3 files
            - Tests: 2 files
            """;

        var result = MarkdownSessionLogParser.TryParse(md, "copilot-test.md");

        Assert.NotNull(result);
        var response = result!.Turns![0].Response!;
        Assert.Contains("Session Overview", response, StringComparison.Ordinal);
        Assert.Contains("Overview content here", response, StringComparison.Ordinal);
        Assert.Contains("Changes Made", response, StringComparison.Ordinal);
        Assert.Contains("Technical Requirements", response, StringComparison.Ordinal);
        Assert.Contains("Testing", response, StringComparison.Ordinal);
        Assert.Contains("All 15 tests passing", response, StringComparison.Ordinal);
        Assert.Contains("Files Summary", response, StringComparison.Ordinal);
    }
}
