using McpServer.Support.Mcp.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Tests.Identity;

public sealed class IdentityServerSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesIdentityTablesWhenDatabaseAlreadyContainsOtherTables()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync().ConfigureAwait(true);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE ExistingData (Id INTEGER PRIMARY KEY);";
            await command.ExecuteNonQueryAsync().ConfigureAwait(true);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<McpIdentityDbContext>(options => options.UseSqlite(connection));
        services.AddIdentity<McpUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 4;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.User.RequireUniqueEmail = false;
        })
        .AddEntityFrameworkStores<McpIdentityDbContext>()
        .AddDefaultTokenProviders();

        await using var provider = services.BuildServiceProvider();

        await IdentityServerSeeder.SeedAsync(
            provider,
            new IdentityServerOptions
            {
                DefaultAdminUser = "admin",
                DefaultAdminPassword = "McpAdmin1!",
            }).ConfigureAwait(true);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpIdentityDbContext>();

        Assert.True(await db.Roles.AnyAsync(role => role.Name == "admin").ConfigureAwait(true));
        Assert.True(await db.Roles.AnyAsync(role => role.Name == "agent-manager").ConfigureAwait(true));
        Assert.True(await db.Users.AnyAsync(user => user.UserName == "admin").ConfigureAwait(true));
    }
}
