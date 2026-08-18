namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-SURFACE-001: Stable HTTP status mapping for handoff ErrorCode values.</summary>
public static class HandoffHttpStatus
{
    /// <summary>Maps a persisted or returned ErrorCode to an HTTP status code.</summary>
    public static int FromErrorCode(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            return 400;

        return errorCode switch
        {
            HandoffErrorCodes.RunNotFound => 404,
            HandoffErrorCodes.InProgress => 409,
            HandoffErrorCodes.TodoCollision => 409,
            HandoffErrorCodes.LostOwnership => 409,
            HandoffErrorCodes.ConcurrencyConflict => 409,
            HandoffErrorCodes.RunNotApprovable => 409,
            HandoffErrorCodes.SourceOversized => 413,
            HandoffErrorCodes.ProcessingFailed => 500,
            HandoffErrorCodes.CompensationFailed => 500,
            HandoffErrorCodes.TodoCreateFailed => 500,
            HandoffErrorCodes.Cancelled => 499,
            _ => 400,
        };
    }
}
