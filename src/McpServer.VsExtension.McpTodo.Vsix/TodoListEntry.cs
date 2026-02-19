using McpServer.VsExtension.McpTodo.Models;

namespace McpServer.VsExtension.McpTodo;

internal sealed class TodoListEntry
{
    public string PriorityGroup { get; set; } = "";
    public string DisplayLine { get; set; } = "";
    public TodoFlatItem? Item { get; set; }
}
