using System.Security.Claims;
using McpServer.Web.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace McpServer.Web.Tests.Authorization;

/// <summary>
/// Unit tests for <see cref="WebRoleContext"/>.
/// </summary>
public sealed class WebRoleContextTests
{
    private static IHttpContextAccessor BuildAccessor(HttpContext? context)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        return accessor;
    }

    private static HttpContext BuildHttpContext(bool isAuthenticated, params string[] roles)
    {
        var claims = new List<Claim>();
        claims.AddRange(roles.Select(static r => new Claim("role", r)));

        var identity = new ClaimsIdentity(
            claims,
            isAuthenticated ? "Test" : null);

        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        return context;
    }

    [Fact]
    public void IsAuthenticated_NullHttpContext_ReturnsFalse()
    {
        var ctx = new WebRoleContext(BuildAccessor(null));
        Assert.False(ctx.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_UnauthenticatedUser_ReturnsFalse()
    {
        var context = BuildHttpContext(isAuthenticated: false);
        var ctx = new WebRoleContext(BuildAccessor(context));
        Assert.False(ctx.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_AuthenticatedUser_ReturnsTrue()
    {
        var context = BuildHttpContext(isAuthenticated: true);
        var ctx = new WebRoleContext(BuildAccessor(context));
        Assert.True(ctx.IsAuthenticated);
    }

    [Fact]
    public void Roles_NullHttpContext_ReturnsEmpty()
    {
        var ctx = new WebRoleContext(BuildAccessor(null));
        Assert.Empty(ctx.Roles);
    }

    [Fact]
    public void Roles_UserWithRoleClaims_ReturnsRoles()
    {
        var context = BuildHttpContext(isAuthenticated: true, "admin", "agent-manager");
        var ctx = new WebRoleContext(BuildAccessor(context));

        Assert.Equal(2, ctx.Roles.Count);
        Assert.Contains("admin", ctx.Roles);
        Assert.Contains("agent-manager", ctx.Roles);
    }

    [Fact]
    public void HasRole_ExactMatch_ReturnsTrue()
    {
        var context = BuildHttpContext(isAuthenticated: true, "admin");
        var ctx = new WebRoleContext(BuildAccessor(context));
        Assert.True(ctx.HasRole("admin"));
    }

    [Fact]
    public void HasRole_CaseInsensitiveMatch_ReturnsTrue()
    {
        var context = BuildHttpContext(isAuthenticated: true, "Admin");
        var ctx = new WebRoleContext(BuildAccessor(context));
        Assert.True(ctx.HasRole("admin"));
        Assert.True(ctx.HasRole("ADMIN"));
    }

    [Fact]
    public void HasRole_MissingRole_ReturnsFalse()
    {
        var context = BuildHttpContext(isAuthenticated: true, "agent-manager");
        var ctx = new WebRoleContext(BuildAccessor(context));
        Assert.False(ctx.HasRole("admin"));
    }

    [Fact]
    public void HasRole_NullHttpContext_ReturnsFalse()
    {
        var ctx = new WebRoleContext(BuildAccessor(null));
        Assert.False(ctx.HasRole("admin"));
    }
}
