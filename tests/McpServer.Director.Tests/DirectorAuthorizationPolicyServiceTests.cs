using McpServer.Director.Auth;
using McpServer.UI.Core.Authorization;
using Xunit;

namespace McpServer.Director.Tests;

public sealed class DirectorAuthorizationPolicyServiceTests
{
    [Fact]
    public void UnauthenticatedUser_CanViewViewerArea()
    {
        var policy = new DirectorAuthorizationPolicyService(new FakeRoleContext(authenticated: false));

        Assert.True(policy.CanViewArea(McpArea.Todo));
    }

    [Fact]
    public void UnauthenticatedUser_CannotViewAdminArea()
    {
        var policy = new DirectorAuthorizationPolicyService(new FakeRoleContext(authenticated: false));

        Assert.False(policy.CanViewArea(McpArea.Workspaces));
        Assert.False(policy.CanViewArea(McpArea.Policy));
    }

    [Fact]
    public void AdminRole_CanViewAllAreas()
    {
        var policy = new DirectorAuthorizationPolicyService(new FakeRoleContext(roles: [McpRoles.Admin]));

        foreach (var area in Enum.GetValues<McpArea>())
            Assert.True(policy.CanViewArea(area));
    }

    [Fact]
    public void AgentManagerRole_CanViewAgentArea()
    {
        var policy = new DirectorAuthorizationPolicyService(new FakeRoleContext(roles: [McpRoles.AgentManager]));

        Assert.True(policy.CanViewArea(McpArea.Agents));
    }

    [Fact]
    public void AgentManagerRole_CannotViewAdminOnlyArea()
    {
        var policy = new DirectorAuthorizationPolicyService(new FakeRoleContext(roles: [McpRoles.AgentManager]));

        Assert.False(policy.CanViewArea(McpArea.Workspaces));
        Assert.False(policy.CanViewArea(McpArea.Policy));
    }

    [Fact]
    public void ViewerRole_CannotViewAdminOnlyAreas()
    {
        var policy = new DirectorAuthorizationPolicyService(new FakeRoleContext(roles: [McpRoles.Viewer]));

        Assert.False(policy.CanViewArea(McpArea.Workspaces));
        Assert.False(policy.CanViewArea(McpArea.Policy));
    }

    [Fact]
    public void ViewerEquivalentUser_CanExecuteViewerAction()
    {
        var policy = new DirectorAuthorizationPolicyService(new FakeRoleContext(authenticated: false));

        Assert.True(policy.CanExecuteAction(McpActionKeys.VoiceCreateSession));
    }

    [Fact]
    public void ViewerRole_CannotExecuteAdminAction()
    {
        var policy = new DirectorAuthorizationPolicyService(new FakeRoleContext(roles: [McpRoles.Viewer]));

        Assert.False(policy.CanExecuteAction(McpActionKeys.WorkspaceCreate));
    }

    [Theory]
    [InlineData(McpActionKeys.WorkspaceCreate, McpRoles.Admin)]
    [InlineData(McpActionKeys.AgentDefinitionList, McpRoles.AgentManager)]
    [InlineData(McpActionKeys.GitHubIssueList, McpRoles.Viewer)]
    [InlineData(McpActionKeys.RequirementsGenerate, McpRoles.Viewer)]
    [InlineData(McpActionKeys.ToolRegistryMutate, McpRoles.Admin)]
    [InlineData(McpActionKeys.VoiceStatus, McpRoles.Viewer)]
    public void GetRequiredRole_ForKnownActionKeys_ReturnsExpectedRole(string actionKey, string expectedRole)
    {
        var policy = new DirectorAuthorizationPolicyService(new FakeRoleContext());

        var role = policy.GetRequiredRole(actionKey);

        Assert.Equal(expectedRole, role);
    }

    [Fact]
    public void UnknownAction_DefaultsToViewer()
    {
        var policy = new DirectorAuthorizationPolicyService(new FakeRoleContext());

        Assert.Equal(McpRoles.Viewer, policy.GetRequiredRole("unknown.action"));
    }

    private sealed class FakeRoleContext : IRoleContext
    {
        private readonly HashSet<string> _roles;

        public FakeRoleContext(bool authenticated = true, IReadOnlyList<string>? roles = null)
        {
            IsAuthenticated = authenticated;
            Roles = roles ?? [];
            _roles = new HashSet<string>(Roles, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsAuthenticated { get; }

        public IReadOnlyList<string> Roles { get; }

        public bool HasRole(string role) => _roles.Contains(role);
    }
}
