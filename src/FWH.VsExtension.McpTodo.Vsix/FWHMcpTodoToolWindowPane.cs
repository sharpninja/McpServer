using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace FWH.VsExtension.McpTodo;

[Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
public sealed class FWHMcpTodoToolWindowPane : ToolWindowPane
{
    public FWHMcpTodoToolWindowPane() : base(null)
    {
        Caption = "MCP Todo";
        Content = new FWHMcpTodoToolWindowControl();
    }
}
