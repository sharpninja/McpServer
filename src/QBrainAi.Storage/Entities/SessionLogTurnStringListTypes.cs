namespace QBrainAi.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-082 / TR-MCP-BYRD-002 (triage-report-04659db1): the one definition of the
/// <see cref="SessionLogTurnStringListEntity.ListType"/> values that session-log writers store and readers
/// filter on, so a reader and a writer cannot disagree on the spelling of a list.
/// </summary>
public static class SessionLogTurnStringListTypes
{
    /// <summary>List type of a turn's design decisions.</summary>
    public const string DesignDecision = "DesignDecision";

    /// <summary>List type of a turn's discovered requirements.</summary>
    public const string Requirement = "Requirement";

    /// <summary>List type of a turn's modified files.</summary>
    public const string FileModified = "FileModified";

    /// <summary>List type of a turn's blockers.</summary>
    public const string Blocker = "Blocker";
}
