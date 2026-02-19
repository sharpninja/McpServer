using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace McpServer.VsExtension.McpTodo;

[Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
public sealed class McpServerMcpTodoToolWindowPane : ToolWindowPane
{
    public McpServerMcpTodoToolWindowPane() : base(null)
    {
        Caption = "MCP Todo";
        Content = new McpServerMcpTodoToolWindowControl();
    }
}
