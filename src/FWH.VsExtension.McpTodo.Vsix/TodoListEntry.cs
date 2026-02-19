using FWH.VsExtension.McpTodo.Models;

namespace FWH.VsExtension.McpTodo;

internal sealed class TodoListEntry
{
    public string PriorityGroup { get; set; } = "";
    public string DisplayLine { get; set; } = "";
    public TodoFlatItem? Item { get; set; }
}
