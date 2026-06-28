using System.Text.Json;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-QBOLLAMA-001: Verifies OpenAI-compatible brain-slot response extraction preserves Ollama thinking-model
/// output fields before they are logged into MCP session processing dialog.
/// </summary>
public sealed class BrainSlotChatClientFactoryTests
{
    /// <summary>TEST-MCP-QBOLLAMA-001: content is preferred over provider-specific thinking fields.</summary>
    [Fact]
    public void ExtractOpenAiCompatibleMessageText_WhenContentPresent_ReturnsContent()
    {
        var json = """
                   {
                     "choices": [
                       {
                         "message": {
                           "content": "content-field-captured",
                           "reasoning": "reasoning-field-ignored",
                           "reasoning_content": "reasoning-content-field-ignored"
                         }
                       }
                     ]
                   }
                   """;

        var result = BrainSlotChatClientFactory.ExtractOpenAiCompatibleMessageText(json);

        Assert.Equal("content-field-captured", result);
    }

    /// <summary>TEST-MCP-QBOLLAMA-001: reasoning is used when Ollama returns empty content.</summary>
    [Fact]
    public void ExtractOpenAiCompatibleMessageText_WhenContentEmptyAndReasoningPresent_ReturnsReasoning()
    {
        var json = """
                   {
                     "choices": [
                       {
                         "message": {
                           "content": "",
                           "reasoning": "reasoning-field-captured",
                           "reasoning_content": "reasoning-content-field-ignored"
                         }
                       }
                     ]
                   }
                   """;

        var result = BrainSlotChatClientFactory.ExtractOpenAiCompatibleMessageText(json);

        Assert.Equal("reasoning-field-captured", result);
    }

    /// <summary>TEST-MCP-QBOLLAMA-001: reasoning_content is used when content and reasoning are empty.</summary>
    [Fact]
    public void ExtractOpenAiCompatibleMessageText_WhenReasoningContentPresent_ReturnsReasoningContent()
    {
        var json = """
                   {
                     "choices": [
                       {
                         "message": {
                           "content": "",
                           "reasoning": "",
                           "reasoning_content": "reasoning-content-field-captured"
                         }
                       }
                     ]
                   }
                   """;

        var result = BrainSlotChatClientFactory.ExtractOpenAiCompatibleMessageText(json);

        Assert.Equal("reasoning-content-field-captured", result);
    }

    /// <summary>TEST-MCP-QBLIVE-001: OpenAI-compatible brain-slot requests include temperature only when supplied by orchestration.</summary>
    [Fact]
    public void BuildOpenAiCompatibleRequestJson_WhenTemperatureSupplied_IncludesTemperature()
    {
        var slot = Slot();

        using var withTemperature = JsonDocument.Parse(BrainSlotChatClientFactory.BuildOpenAiCompatibleRequestJson(slot, "input", 0.0));
        using var withoutTemperature = JsonDocument.Parse(BrainSlotChatClientFactory.BuildOpenAiCompatibleRequestJson(slot, "input", null));

        Assert.True(withTemperature.RootElement.TryGetProperty("temperature", out var temperature));
        Assert.Equal(0.0, temperature.GetDouble());
        Assert.False(withoutTemperature.RootElement.TryGetProperty("temperature", out _));
    }

    private static BrainSlotDefinitionEntity Slot()
        => new()
        {
            WorkspaceId = string.Empty,
            SlotId = "right-main",
            Role = BrainSlotRoles.RightHemisphere,
            ProviderKind = "OpenAICompatible",
            ModelId = "model",
            Endpoint = "http://localhost:11434/v1",
            CredentialReference = "env:OLLAMA_API_KEY",
            PartyId = "brain-slot:right-hemisphere",
            Enabled = true,
            TimeoutSeconds = 30,
            MaxOutputTokens = 1024,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
}
