using System.Text.Json;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-HANDOFF-001 / TEST-HANDOFF-006: strict enums and HTTP status mapping.</summary>
public sealed class HandoffStrictEnumAndHttpTests
{
    /// <summary>P1-7: numeric JSON 999 is rejected by the string-only converter.</summary>
    [Fact]
    public void StrictEnumConverter_Numeric999_Throws()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<HandoffIngestionRequest>("""{"sourceKind":"Content","content":"x","mode":999}""", options));
        Assert.Contains("Integer values are not allowed", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>P1-7: an undefined name is rejected.</summary>
    [Fact]
    public void StrictEnumConverter_UndefinedName_Throws()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<HandoffIngestionRequest>("""{"sourceKind":"Content","content":"x","mode":"NotAMode"}""", options));
    }

    /// <summary>P2-4: ErrorCode maps to stable HTTP statuses.</summary>
    [Theory]
    [InlineData(HandoffErrorCodes.RunNotFound, 404)]
    [InlineData(HandoffErrorCodes.InProgress, 409)]
    [InlineData(HandoffErrorCodes.TodoCollision, 409)]
    [InlineData(HandoffErrorCodes.LostOwnership, 409)]
    [InlineData(HandoffErrorCodes.SourceOversized, 413)]
    [InlineData(HandoffErrorCodes.ProcessingFailed, 500)]
    [InlineData(HandoffErrorCodes.CompensationFailed, 500)]
    [InlineData(HandoffErrorCodes.InvalidMode, 400)]
    public void FromErrorCode_MapsStableStatuses(string code, int status)
        => Assert.Equal(status, HandoffHttpStatus.FromErrorCode(code));

    /// <summary>P2-4: ingest/get/approve use the same ErrorCode mapping and 5xx does not leak provider text.</summary>
    [Fact]
    public async Task Controller_MapsErrorCodesAndRedactsInternalErrors()
    {
        var service = Substitute.For<IHandoffIngestionService>();
        service.IngestAsync(Arg.Any<HandoffIngestionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = false, Error = "SqlException deadlock on HandoffIngestionRuns", ErrorCode = HandoffErrorCodes.ProcessingFailed });
        service.GetRunAsync("missing", Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = false, Error = "gone", ErrorCode = HandoffErrorCodes.RunNotFound });
        service.ApproveAsync("busy", Arg.Any<HandoffApprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = false, Error = "busy", ErrorCode = HandoffErrorCodes.InProgress });
        var sut = new HandoffController(service);

        var ingest = await sut.IngestAsync(new HandoffIngestionRequest { SourceKind = HandoffSourceKind.Content, Content = "x" }, TestContext.Current.CancellationToken);
        var get = await sut.GetRunAsync("missing", TestContext.Current.CancellationToken);
        var approve = await sut.ApproveAsync("busy", new HandoffApprovalRequest { Approved = true }, TestContext.Current.CancellationToken);

        var ingestObject = Assert.IsType<ObjectResult>(ingest.Result);
        Assert.Equal(500, ingestObject.StatusCode);
        var leaked = Assert.IsType<HandoffIngestionResult>(ingestObject.Value);
        Assert.Equal("Handoff processing failed.", leaked.Error);
        Assert.DoesNotContain("SqlException", leaked.Error, StringComparison.Ordinal);
        Assert.IsType<NotFoundObjectResult>(get.Result);
        Assert.IsType<ConflictObjectResult>(approve.Result);
    }
}
