namespace QBrainAi.Support.Mcp.Services;

/// <summary>
/// TR-MCP-WS-005 / FR-MCP-018 ac-1, TR-MCP-SESSIONLOG-006 / AC-FR-MCP-SESSIONLOGCTX-001-008:
/// one entry of a single directory listing, as the user-profile scans read it.
/// </summary>
/// <param name="Path">Full path of the entry.</param>
/// <param name="IsDirectory">True when the entry is a directory, or a link that resolves to one.</param>
/// <param name="IsLink">True when the entry is a link or other reparse point (symbolic link, junction, mount point).</param>
internal readonly record struct DirectoryListingEntry(string Path, bool IsDirectory, bool IsLink);
