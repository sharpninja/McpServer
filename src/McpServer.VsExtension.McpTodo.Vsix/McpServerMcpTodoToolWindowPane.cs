using System.Runtime.InteropServices;
using McpServer.UI;
using Microsoft.VisualStudio.Shell;

namespace McpServer.VsExtension.McpTodo;

[Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
public sealed class McpServerMcpTodoToolWindowPane : ToolWindowPane
{
    public McpServerMcpTodoToolWindowPane() : base(null)
    {
        Caption = "MCP Todo";

        var client = new McpTodoClient();
        var editorService = TodoEditorService.Instance ?? new TodoEditorService(client);

        var viewModel = new TodoToolWindowViewModel(
            client,
            editorService,
            openFileInEditor: McpServerMcpTodoToolWindowControl.OpenFileInEditor,
            showCompletionInfoBar: McpServerMcpTodoToolWindowControl.ShowCompletionInfoBar);

        Content = new McpServerMcpTodoToolWindowControl(viewModel);
    }
}
