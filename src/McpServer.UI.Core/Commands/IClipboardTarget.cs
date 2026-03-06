using System.Threading.Tasks;
using McpServer.UI.Core.Models.Json;

namespace McpServer.UI.Core.Commands;

/// <summary>
/// Clipboard copy operations.
/// </summary>
public interface IClipboardTarget
{
    Task CopyText(string text);
    Task CopyOriginalJson(UnifiedRequestEntry? entry);
}

