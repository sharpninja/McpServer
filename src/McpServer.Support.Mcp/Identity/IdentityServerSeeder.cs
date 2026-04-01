using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

        // Apply Identity migrations
        var identityDb = sp.GetRequiredService<McpIdentityDbContext>();
        await identityDb.Database.MigrateAsync();

        // Seed default admin user
        var userManager = sp.GetRequiredService<UserManager<McpUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in new[] { "admin", "agent-manager" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        var adminUser = await userManager.FindByNameAsync(options.DefaultAdminUser);
        if (adminUser is null)
        {
            adminUser = new McpUser
            {
                UserName = options.DefaultAdminUser,
                Email = $"{options.DefaultAdminUser}@localhost",
                EmailConfirmed = true,
                DisplayName = "MCP Administrator",
            };
            var result = await userManager.CreateAsync(adminUser, options.DefaultAdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(adminUser, ["admin", "agent-manager"]);
            }
        }

        // Seed additional users
        await EnsureUserAsync(userManager, "plbyrd", "plbyrd", "P.L. Byrd", ["admin", "agent-manager"]);
    }

    private static async Task EnsureUserAsync(
        UserManager<McpUser> userManager,
        string userName,
        string password,
        string displayName,
        string[] roles)
    {
        var existing = await userManager.FindByNameAsync(userName);
        if (existing is not null)
            return;

        var user = new McpUser
        {
            UserName = userName,
            Email = $"{userName}@localhost",
            EmailConfirmed = true,
            DisplayName = displayName,
        };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRolesAsync(user, roles);
        }
    }
}
