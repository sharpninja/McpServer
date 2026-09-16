namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-QBEXEC-002: Exhaustive mcp_* catalog QuadBrain must execute server-side.
/// Matches McpHostedAgentToolAdapter plus mcp_repo_edit and mutating mcp_git.
/// </summary>
public static class QuadBrainMcpToolCatalog
{
    /// <summary>Every mcp_* name the executor must Handle.</summary>
    public static readonly string[] All =
    [
        "mcp_session_bootstrap",
        "mcp_session_update",
        "mcp_session_turn_begin",
        "mcp_session_turn_update",
        "mcp_session_turn_complete",
        "mcp_session_query_history",
        "mcp_todo_query",
        "mcp_todo_get",
        "mcp_todo_update",
        "mcp_todo_create",
        "mcp_todo_delete",
        "mcp_todo_plan",
        "mcp_todo_status",
        "mcp_todo_implementation",
        "mcp_repo_read",
        "mcp_repo_list",
        "mcp_repo_write",
        "mcp_repo_edit",
        "mcp_desktop_launch",
        "mcp_powershell_session_create",
        "mcp_powershell_session_command",
        "mcp_powershell_session_close",
        "mcp_requirements_list_fr",
        "mcp_requirements_list_tr",
        "mcp_requirements_list_test",
        "mcp_requirements_get_fr",
        "mcp_requirements_get_tr",
        "mcp_requirements_get_test",
        "mcp_requirements_create_fr",
        "mcp_requirements_update_fr",
        "mcp_requirements_create_tr",
        "mcp_requirements_update_tr",
        "mcp_requirements_create_test",
        "mcp_requirements_update_test",
        "mcp_client_invoke",
        "mcp_graphrag_ingest_text",
        "mcp_graphrag_list_documents",
        "mcp_graphrag_get_document_chunks",
        "mcp_graphrag_delete_document",
        "mcp_graphrag_create_entity",
        "mcp_graphrag_list_entities",
        "mcp_graphrag_get_entity",
        "mcp_graphrag_update_entity",
        "mcp_graphrag_delete_entity",
        "mcp_graphrag_create_relationship",
        "mcp_graphrag_list_relationships",
        "mcp_graphrag_get_relationship",
        "mcp_graphrag_update_relationship",
        "mcp_graphrag_delete_relationship",
        "mcp_git",
    ];

    /// <summary>
    /// mcp_* names published by <c>McpHostedAgentToolAdapter.CreateFunctions</c> and by
    /// <c>QBAgentDefinition</c> AllowedTools plus BlockedTools. The executor catalog is this set
    /// plus mcp_repo_edit, mutating mcp_git, and requirements create/update.
    /// </summary>
    public static readonly string[] HostedAgentPublishedNames =
    [
        "mcp_session_bootstrap",
        "mcp_session_update",
        "mcp_session_turn_begin",
        "mcp_session_turn_update",
        "mcp_session_turn_complete",
        "mcp_session_query_history",
        "mcp_todo_query",
        "mcp_todo_get",
        "mcp_todo_update",
        "mcp_todo_create",
        "mcp_todo_delete",
        "mcp_todo_plan",
        "mcp_todo_status",
        "mcp_todo_implementation",
        "mcp_repo_read",
        "mcp_repo_list",
        "mcp_repo_write",
        "mcp_desktop_launch",
        "mcp_powershell_session_create",
        "mcp_powershell_session_command",
        "mcp_powershell_session_close",
        "mcp_requirements_list_fr",
        "mcp_requirements_list_tr",
        "mcp_requirements_list_test",
        "mcp_requirements_get_fr",
        "mcp_requirements_get_tr",
        "mcp_requirements_get_test",
        "mcp_client_invoke",
        "mcp_graphrag_ingest_text",
        "mcp_graphrag_list_documents",
        "mcp_graphrag_get_document_chunks",
        "mcp_graphrag_delete_document",
        "mcp_graphrag_create_entity",
        "mcp_graphrag_list_entities",
        "mcp_graphrag_get_entity",
        "mcp_graphrag_update_entity",
        "mcp_graphrag_delete_entity",
        "mcp_graphrag_create_relationship",
        "mcp_graphrag_list_relationships",
        "mcp_graphrag_get_relationship",
        "mcp_graphrag_update_relationship",
        "mcp_graphrag_delete_relationship",
    ];
}
