using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// Seeds the IdentityServer databases with default configuration and an admin user on first run.
/// </summary>
internal static class IdentityServerSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IdentityServerOptions options)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var identityDb = sp.GetRequiredService<McpIdentityDbContext>();
        await EnsureIdentitySchemaAsync(identityDb).ConfigureAwait(false);

        // Seed default admin user
        var userManager = sp.GetRequiredService<UserManager<McpUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in new[] { "admin", "agent-manager" })
        {
            if (!await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
                await roleManager.CreateAsync(new IdentityRole(roleName)).ConfigureAwait(false);
        }

        var adminUser = await userManager.FindByNameAsync(options.DefaultAdminUser).ConfigureAwait(false);
        if (adminUser is null)
        {
            adminUser = new McpUser
            {
                UserName = options.DefaultAdminUser,
                Email = $"{options.DefaultAdminUser}@localhost",
                EmailConfirmed = true,
                DisplayName = "MCP Administrator",
            };
            var result = await userManager.CreateAsync(adminUser, options.DefaultAdminPassword).ConfigureAwait(false);
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(adminUser, ["admin", "agent-manager"]).ConfigureAwait(false);
            }
        }

        // Seed additional users
        await EnsureUserAsync(userManager, "plbyrd", "plbyrd", "P.L. Byrd", ["admin", "agent-manager"]).ConfigureAwait(false);
    }

    private static async Task EnsureIdentitySchemaAsync(McpIdentityDbContext identityDb)
    {
        await identityDb.Database.EnsureCreatedAsync().ConfigureAwait(false);

        if (!identityDb.Database.IsRelational())
            return;

        try
        {
            _ = await identityDb.Roles.AsNoTracking().AnyAsync().ConfigureAwait(false);
        }
        catch (DbException ex) when (IsMissingIdentityTable(ex))
        {
            var creator = identityDb.GetService<IRelationalDatabaseCreator>();
            await creator.CreateTablesAsync().ConfigureAwait(false);
        }
    }

    private static bool IsMissingIdentityTable(DbException ex)
        => ex.Message.Contains("AspNetRoles", StringComparison.OrdinalIgnoreCase)
           || ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase)
           || ex.Message.Contains("invalid object name", StringComparison.OrdinalIgnoreCase);

    private static async Task EnsureUserAsync(
        UserManager<McpUser> userManager,
        string userName,
        string password,
        string displayName,
        string[] roles)
    {
        var existing = await userManager.FindByNameAsync(userName).ConfigureAwait(false);
        if (existing is not null)
            return;

        var user = new McpUser
        {
            UserName = userName,
            Email = $"{userName}@localhost",
            EmailConfirmed = true,
            DisplayName = displayName,
        };
        var result = await userManager.CreateAsync(user, password).ConfigureAwait(false);
        if (result.Succeeded)
        {
            await userManager.AddToRolesAsync(user, roles).ConfigureAwait(false);
        }
    }
}
