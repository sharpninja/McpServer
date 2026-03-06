using System.Threading.Tasks;
using McpServer.Cqrs;

namespace McpServer.UI.Core.Commands;

// --- Navigation Commands ---

public sealed record NavigateBackCommand() : ICommand<bool>;

public sealed class NavigateBackHandler(INavigationTarget target) : ICommandHandler<NavigateBackCommand, bool>
{
    public Task<Result<bool>> HandleAsync(NavigateBackCommand command, CallContext context)
    {
        target.NavigateBack();
        return Task.FromResult(Result<bool>.Success(true));
    }
}

public sealed record NavigateForwardCommand() : ICommand<bool>;

public sealed class NavigateForwardHandler(INavigationTarget target) : ICommandHandler<NavigateForwardCommand, bool>
{
    public Task<Result<bool>> HandleAsync(NavigateForwardCommand command, CallContext context)
    {
        target.NavigateForward();
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Refresh ---

public sealed record RefreshViewCommand() : ICommand<bool>;

public sealed class RefreshViewHandler(INavigationTarget target) : ICommandHandler<RefreshViewCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(RefreshViewCommand command, CallContext context)
    {
        await target.RefreshAsync();
        return Result<bool>.Success(true);
    }
}

// --- Request Details ---

public sealed record ShowRequestDetailsCommand(Models.Json.SearchableEntry Entry) : ICommand<bool>;

public sealed class ShowRequestDetailsHandler(IRequestDetailsTarget target) : ICommandHandler<ShowRequestDetailsCommand, bool>
{
    public Task<Result<bool>> HandleAsync(ShowRequestDetailsCommand command, CallContext context)
    {
        target.ShowRequestDetails(command.Entry);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

public sealed record CloseRequestDetailsCommand() : ICommand<bool>;

public sealed class CloseRequestDetailsHandler(IRequestDetailsTarget target) : ICommandHandler<CloseRequestDetailsCommand, bool>
{
    public Task<Result<bool>> HandleAsync(CloseRequestDetailsCommand command, CallContext context)
    {
        target.CloseRequestDetails();
        return Task.FromResult(Result<bool>.Success(true));
    }
}

public sealed record NavigateToPreviousRequestCommand() : ICommand<bool>;

public sealed class NavigateToPreviousRequestHandler(IRequestDetailsTarget target) : ICommandHandler<NavigateToPreviousRequestCommand, bool>
{
    public Task<Result<bool>> HandleAsync(NavigateToPreviousRequestCommand command, CallContext context)
    {
        target.NavigateToPreviousRequest();
        return Task.FromResult(Result<bool>.Success(true));
    }
}

public sealed record NavigateToNextRequestCommand() : ICommand<bool>;

public sealed class NavigateToNextRequestHandler(IRequestDetailsTarget target) : ICommandHandler<NavigateToNextRequestCommand, bool>
{
    public Task<Result<bool>> HandleAsync(NavigateToNextRequestCommand command, CallContext context)
    {
        target.NavigateToNextRequest();
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Selection ---

public sealed record SelectSearchEntryCommand(Models.Json.SearchableEntry Entry) : ICommand<bool>;

public sealed class SelectSearchEntryHandler(IRequestDetailsTarget target) : ICommandHandler<SelectSearchEntryCommand, bool>
{
    public Task<Result<bool>> HandleAsync(SelectSearchEntryCommand command, CallContext context)
    {
        target.SelectSearchEntry(command.Entry);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Clipboard ---

public sealed record CopyTextCommand(string Text) : ICommand<bool>;

public sealed class CopyTextHandler(IClipboardTarget target) : ICommandHandler<CopyTextCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(CopyTextCommand command, CallContext context)
    {
        await target.CopyText(command.Text);
        return Result<bool>.Success(true);
    }
}

public sealed record CopyOriginalJsonCommand(Models.Json.UnifiedRequestEntry? Entry) : ICommand<bool>;

public sealed class CopyOriginalJsonHandler(IClipboardTarget target) : ICommandHandler<CopyOriginalJsonCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(CopyOriginalJsonCommand command, CallContext context)
    {
        await target.CopyOriginalJson(command.Entry);
        return Result<bool>.Success(true);
    }
}

// --- Preview/Markdown ---

public sealed record OpenPreviewInBrowserCommand() : ICommand<bool>;

public sealed class OpenPreviewInBrowserHandler(IPreviewTarget target) : ICommandHandler<OpenPreviewInBrowserCommand, bool>
{
    public Task<Result<bool>> HandleAsync(OpenPreviewInBrowserCommand command, CallContext context)
    {
        target.OpenPreviewInBrowser();
        return Task.FromResult(Result<bool>.Success(true));
    }
}

public sealed record ToggleShowRawMarkdownCommand() : ICommand<bool>;

public sealed class ToggleShowRawMarkdownHandler(IPreviewTarget target) : ICommandHandler<ToggleShowRawMarkdownCommand, bool>
{
    public Task<Result<bool>> HandleAsync(ToggleShowRawMarkdownCommand command, CallContext context)
    {
        target.ToggleShowRawMarkdown();
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Archive ---

public sealed record ArchiveCurrentCommand() : ICommand<bool>;

public sealed class ArchiveCurrentHandler(IArchiveTarget target) : ICommandHandler<ArchiveCurrentCommand, bool>
{
    public Task<Result<bool>> HandleAsync(ArchiveCurrentCommand command, CallContext context)
    {
        target.Archive();
        return Task.FromResult(Result<bool>.Success(true));
    }
}

public sealed record ArchiveTreeItemCommand(Models.FileNode? Node) : ICommand<bool>;

public sealed class ArchiveTreeItemHandler(IArchiveTarget target) : ICommandHandler<ArchiveTreeItemCommand, bool>
{
    public Task<Result<bool>> HandleAsync(ArchiveTreeItemCommand command, CallContext context)
    {
        target.ArchiveTreeItem(command.Node);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Tree Operations ---

public sealed record OpenTreeItemCommand(Models.FileNode? Node) : ICommand<bool>;

public sealed class OpenTreeItemHandler(INavigationTarget target) : ICommandHandler<OpenTreeItemCommand, bool>
{
    public Task<Result<bool>> HandleAsync(OpenTreeItemCommand command, CallContext context)
    {
        target.OpenTreeItem(command.Node);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Config ---

public sealed record OpenAgentConfigCommand() : ICommand<bool>;

public sealed class OpenAgentConfigHandler(IConfigTarget target) : ICommandHandler<OpenAgentConfigCommand, bool>
{
    public Task<Result<bool>> HandleAsync(OpenAgentConfigCommand command, CallContext context)
    {
        target.OpenAgentConfig();
        return Task.FromResult(Result<bool>.Success(true));
    }
}

public sealed record OpenPromptTemplatesCommand() : ICommand<bool>;

public sealed class OpenPromptTemplatesHandler(IConfigTarget target) : ICommandHandler<OpenPromptTemplatesCommand, bool>
{
    public Task<Result<bool>> HandleAsync(OpenPromptTemplatesCommand command, CallContext context)
    {
        target.OpenPromptTemplates();
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Phone Navigation ---

public sealed record PhoneNavigateSectionCommand(string? SectionKey) : ICommand<bool>;

public sealed class PhoneNavigateSectionHandler(INavigationTarget target) : ICommandHandler<PhoneNavigateSectionCommand, bool>
{
    public Task<Result<bool>> HandleAsync(PhoneNavigateSectionCommand command, CallContext context)
    {
        target.PhoneNavigateSection(command.SectionKey);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Tree Item Tap ---

public sealed record TreeItemTappedCommand(Models.FileNode? Node) : ICommand<bool>;

public sealed class TreeItemTappedHandler(INavigationTarget target) : ICommandHandler<TreeItemTappedCommand, bool>
{
    public Task<Result<bool>> HandleAsync(TreeItemTappedCommand command, CallContext context)
    {
        target.TreeItemTapped(command.Node);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- JSON Node Double-Tap ---

public sealed record JsonNodeDoubleTappedCommand(Models.Json.JsonTreeNode? Node) : ICommand<bool>;

public sealed class JsonNodeDoubleTappedHandler(INavigationTarget target) : ICommandHandler<JsonNodeDoubleTappedCommand, bool>
{
    public Task<Result<bool>> HandleAsync(JsonNodeDoubleTappedCommand command, CallContext context)
    {
        target.JsonNodeDoubleTapped(command.Node);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Search Row Tap ---

public sealed record SearchRowTappedCommand(Models.Json.SearchableEntry? Entry) : ICommand<bool>;

public sealed class SearchRowTappedHandler(IRequestDetailsTarget target) : ICommandHandler<SearchRowTappedCommand, bool>
{
    public Task<Result<bool>> HandleAsync(SearchRowTappedCommand command, CallContext context)
    {
        target.SearchRowTapped(command.Entry);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Search Row Double-Tap ---

public sealed record SearchRowDoubleTappedCommand(Models.Json.SearchableEntry? Entry) : ICommand<bool>;

public sealed class SearchRowDoubleTappedHandler(IRequestDetailsTarget target) : ICommandHandler<SearchRowDoubleTappedCommand, bool>
{
    public Task<Result<bool>> HandleAsync(SearchRowDoubleTappedCommand command, CallContext context)
    {
        target.SearchRowDoubleTapped(command.Entry);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

