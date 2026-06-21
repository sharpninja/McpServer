// FR-MCP-REPL-003: Command Namespace Parity - memory workflow command shapes
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - memory command contracts

namespace McpServer.Repl.Core;

/// <summary>
/// Defines YAML command shapes for the <c>workflow.memory.*</c> namespace.
/// </summary>
public static class MemoryCommandShapes
{
    /// <summary>Namespace prefix for memory workflow commands.</summary>
    public const string MethodNamespace = "workflow.memory";

    /// <summary>Method: <c>workflow.memory.list</c>.</summary>
    public const string ListMethod = "workflow.memory.list";

    /// <summary>Method: <c>workflow.memory.get</c>.</summary>
    public const string GetMethod = "workflow.memory.get";

    /// <summary>Method: <c>workflow.memory.add</c>.</summary>
    public const string AddMethod = "workflow.memory.add";

    /// <summary>Method: <c>workflow.memory.update</c>.</summary>
    public const string UpdateMethod = "workflow.memory.update";

    /// <summary>Method: <c>workflow.memory.remove</c>.</summary>
    public const string RemoveMethod = "workflow.memory.remove";
}
