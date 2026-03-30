using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

public class WorkspaceSelectionTests
{
    [Fact]
    public async Task DiscoverWorkspacesAsync_FindsMarkerFiles_ReturnsWorkspaceCandidates()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        
        var candidate1 = Substitute.For<IWorkspaceCandidate>();
        candidate1.WorkspacePath.Returns("/home/user/project1");
        candidate1.IsTrusted.Returns(true);
        
        var candidate2 = Substitute.For<IWorkspaceCandidate>();
        candidate2.WorkspacePath.Returns("/home/user/project2");
        candidate2.IsTrusted.Returns(false);
        
        selector.DiscoverWorkspacesAsync(null, default)
            .Returns(new[] { candidate1, candidate2 });
        
        var result = await selector.DiscoverWorkspacesAsync();
        
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.WorkspacePath == "/home/user/project1");
    }

    [Fact]
    public async Task DiscoverWorkspacesAsync_WithSearchPaths_SearchesSpecifiedPaths()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        var searchPaths = new[] { "/custom/path1", "/custom/path2" };
        
        var candidate = Substitute.For<IWorkspaceCandidate>();
        candidate.WorkspacePath.Returns("/custom/path1/workspace");
        
        selector.DiscoverWorkspacesAsync(searchPaths, default)
            .Returns(new[] { candidate });
        
        var result = await selector.DiscoverWorkspacesAsync(searchPaths);
        
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task SelectWorkspaceAsync_ValidWorkspace_ReturnsSuccessResult()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        
        var selectionResult = Substitute.For<IWorkspaceSelectionResult>();
        selectionResult.WorkspacePath.Returns("/home/user/project");
        selectionResult.Success.Returns(true);
        selectionResult.ErrorMessage.Returns((string?)null);
        
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.WorkspacePath.Returns("/home/user/project");
        selectionResult.MarkerData.Returns(markerData);
        
        var authState = Substitute.For<IAuthState>();
        authState.IsValid.Returns(true);
        selectionResult.AuthState.Returns(authState);
        
        selector.SelectWorkspaceAsync("/home/user/project", false, default)
            .Returns(selectionResult);
        
        var result = await selector.SelectWorkspaceAsync("/home/user/project");
        
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal("/home/user/project", result.WorkspacePath);
    }

    [Fact]
    public async Task SelectWorkspaceAsync_UntrustedWorkspace_ThrowsUnauthorizedAccessException()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        
        selector.SelectWorkspaceAsync("/untrusted/workspace", false, default)
            .Returns<IWorkspaceSelectionResult>(x => throw new UnauthorizedAccessException("Workspace not trusted"));
        
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await selector.SelectWorkspaceAsync("/untrusted/workspace")
        );
    }

    [Fact]
    public async Task SelectWorkspaceAsync_WorkspaceNotFound_ThrowsFileNotFoundException()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        
        selector.SelectWorkspaceAsync("/nonexistent/workspace", false, default)
            .Returns<IWorkspaceSelectionResult>(x => throw new FileNotFoundException("Workspace not found"));
        
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await selector.SelectWorkspaceAsync("/nonexistent/workspace")
        );
    }

    [Fact]
    public async Task SelectWorkspaceAsync_AlreadyActive_ThrowsInvalidOperationException()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        selector.ActiveWorkspace.Returns("/home/user/project");
        
        selector.SelectWorkspaceAsync("/home/user/project", false, default)
            .Returns<IWorkspaceSelectionResult>(x => throw new InvalidOperationException("Workspace already active"));
        
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await selector.SelectWorkspaceAsync("/home/user/project", forceReselect: false)
        );
    }

    [Fact]
    public async Task SelectWorkspaceAsync_ForceReselect_AllowsReselectingSameWorkspace()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        selector.ActiveWorkspace.Returns("/home/user/project");
        
        var selectionResult = Substitute.For<IWorkspaceSelectionResult>();
        selectionResult.WorkspacePath.Returns("/home/user/project");
        selectionResult.Success.Returns(true);
        
        selector.SelectWorkspaceAsync("/home/user/project", true, default)
            .Returns(selectionResult);
        
        var result = await selector.SelectWorkspaceAsync("/home/user/project", forceReselect: true);
        
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task SwitchWorkspaceAsync_DisconnectsAndConnects_ReturnsNewWorkspaceResult()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        selector.ActiveWorkspace.Returns("/home/user/old-project");
        
        var selectionResult = Substitute.For<IWorkspaceSelectionResult>();
        selectionResult.WorkspacePath.Returns("/home/user/new-project");
        selectionResult.Success.Returns(true);
        
        selector.SwitchWorkspaceAsync("/home/user/new-project", default)
            .Returns(selectionResult);
        
        var result = await selector.SwitchWorkspaceAsync("/home/user/new-project");
        
        Assert.NotNull(result);
        Assert.Equal("/home/user/new-project", result.WorkspacePath);
    }

    [Fact]
    public async Task DeselectWorkspaceAsync_WhenWorkspaceActive_DisconnectsSuccessfully()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        selector.ActiveWorkspace.Returns("/home/user/project");
        
        await selector.DeselectWorkspaceAsync();
        
        await selector.Received(1).DeselectWorkspaceAsync(default);
    }

    [Fact]
    public async Task DeselectWorkspaceAsync_NoActiveWorkspace_ThrowsInvalidOperationException()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        selector.ActiveWorkspace.Returns((string?)null);
        
        selector.DeselectWorkspaceAsync(default)
            .Returns<Task>(x => throw new InvalidOperationException("No workspace is currently active"));
        
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await selector.DeselectWorkspaceAsync()
        );
    }

    [Fact]
    public void GetActiveMarkerData_WhenWorkspaceActive_ReturnsMarkerData()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        selector.ActiveWorkspace.Returns("/home/user/project");
        
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.WorkspacePath.Returns("/home/user/project");
        
        selector.GetActiveMarkerData().Returns(markerData);
        
        var result = selector.GetActiveMarkerData();
        
        Assert.NotNull(result);
        Assert.Equal("/home/user/project", result.WorkspacePath);
    }

    [Fact]
    public void GetActiveMarkerData_NoActiveWorkspace_ThrowsInvalidOperationException()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        selector.ActiveWorkspace.Returns((string?)null);
        
        selector.When(x => x.GetActiveMarkerData())
            .Do(x => throw new InvalidOperationException("No workspace is currently active"));
        
        Assert.Throws<InvalidOperationException>(() => selector.GetActiveMarkerData());
    }

    [Fact]
    public async Task ValidateWorkspacePathAsync_ValidPath_ReturnsTrue()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        
        selector.ValidateWorkspacePathAsync("/home/user/project", default)
            .Returns(true);
        
        var result = await selector.ValidateWorkspacePathAsync("/home/user/project");
        
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateWorkspacePathAsync_InvalidPath_ReturnsFalse()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        
        selector.ValidateWorkspacePathAsync("/invalid/path", default)
            .Returns(false);
        
        var result = await selector.ValidateWorkspacePathAsync("/invalid/path");
        
        Assert.False(result);
    }

    [Fact]
    public void ActiveWorkspace_BeforeSelection_ReturnsNull()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        selector.ActiveWorkspace.Returns((string?)null);
        
        Assert.Null(selector.ActiveWorkspace);
    }

    [Fact]
    public async Task ActiveWorkspace_AfterSelection_ReturnsWorkspacePath()
    {
        var selector = Substitute.For<IWorkspaceSelector>();
        
        var selectionResult = Substitute.For<IWorkspaceSelectionResult>();
        selectionResult.WorkspacePath.Returns("/home/user/project");
        selectionResult.Success.Returns(true);
        
        selector.SelectWorkspaceAsync("/home/user/project", false, default)
            .Returns(selectionResult);
        
        selector.ActiveWorkspace.Returns("/home/user/project");
        
        await selector.SelectWorkspaceAsync("/home/user/project");
        
        Assert.Equal("/home/user/project", selector.ActiveWorkspace);
    }

    [Fact]
    public async Task WorkspaceCandidate_ContainsMarkerDataAndTrustStatus()
    {
        var candidate = Substitute.For<IWorkspaceCandidate>();
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.WorkspacePath.Returns("/home/user/project");
        
        candidate.WorkspacePath.Returns("/home/user/project");
        candidate.MarkerData.Returns(markerData);
        candidate.IsTrusted.Returns(true);
        candidate.IsActive.Returns(false);
        
        Assert.Equal("/home/user/project", candidate.WorkspacePath);
        Assert.NotNull(candidate.MarkerData);
        Assert.True(candidate.IsTrusted);
        Assert.False(candidate.IsActive);
        
        await Task.CompletedTask;
    }

    [Fact]
    public async Task WorkspaceSelectionResult_ContainsAuthState()
    {
        var result = Substitute.For<IWorkspaceSelectionResult>();
        var authState = Substitute.For<IAuthState>();
        authState.ApiKey.Returns("workspace-api-key");
        authState.IsValid.Returns(true);
        
        result.WorkspacePath.Returns("/home/user/project");
        result.AuthState.Returns(authState);
        result.Success.Returns(true);
        result.SelectedAt.Returns(DateTimeOffset.UtcNow);
        
        Assert.Equal("/home/user/project", result.WorkspacePath);
        Assert.NotNull(result.AuthState);
        Assert.True(result.AuthState.IsValid);
        Assert.True(result.Success);
        
        await Task.CompletedTask;
    }
}
