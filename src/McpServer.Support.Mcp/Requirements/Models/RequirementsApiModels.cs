namespace McpServer.Support.Mcp.Requirements.Models;

/// <summary>Request payload for creating a Functional Requirement entry.</summary>
/// <param name="Id">Requirement identifier (e.g. FR-MCP-040).</param>
/// <param name="Title">Requirement title.</param>
/// <param name="Body">Requirement body text.</param>
public sealed record CreateFrRequest(string Id, string Title, string Body);

/// <summary>Request payload for updating a Functional Requirement entry.</summary>
/// <param name="Title">Requirement title.</param>
/// <param name="Body">Requirement body text.</param>
public sealed record UpdateFrRequest(string Title, string Body);

/// <summary>Request payload for creating a Technical Requirement entry.</summary>
/// <param name="Id">Requirement identifier (e.g. TR-MCP-REQ-002).</param>
/// <param name="Title">Optional title rendered as bold text before the em dash.</param>
/// <param name="Body">Requirement body text.</param>
public sealed record CreateTrRequest(string Id, string? Title, string Body);

/// <summary>Request payload for updating a Technical Requirement entry.</summary>
/// <param name="Title">Optional title rendered as bold text before the em dash.</param>
/// <param name="Body">Requirement body text.</param>
public sealed record UpdateTrRequest(string? Title, string Body);

/// <summary>Request payload for creating a Testing Requirement entry.</summary>
/// <param name="Id">Requirement identifier (e.g. TEST-MCP-039).</param>
/// <param name="Condition">Test condition text.</param>
public sealed record CreateTestRequest(string Id, string Condition);

/// <summary>Request payload for updating a Testing Requirement entry.</summary>
/// <param name="Condition">Test condition text.</param>
public sealed record UpdateTestRequest(string Condition);

/// <summary>Request payload for creating or updating an FR-to-TR mapping row.</summary>
/// <param name="TrIds">List of TR identifiers mapped to the FR row.</param>
public sealed record UpsertFrTrMappingRequest(IReadOnlyList<string> TrIds);
