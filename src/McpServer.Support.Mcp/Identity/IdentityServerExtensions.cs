using Duende.IdentityServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// Extension methods to register the embedded IdentityServer in the MCP Server host.
/// </summary>
internal static class IdentityServerExtensions
{
    public static IServiceCollection AddMcpIdentityServer(
        this IServiceCollection services,
        IConfiguration configuration,
        string dataFolder)
    {
        var options = configuration.GetSection(IdentityServerOptions.SectionName).Get<IdentityServerOptions>()
            ?? new IdentityServerOptions();

        if (!options.Enabled)
            return services;

        var identityDbPath = Path.IsPathRooted(options.DatabaseFile)
            ? options.DatabaseFile
            : Path.Combine(dataFolder, options.DatabaseFile);

        var identityConnectionString = $"Data Source={identityDbPath}";

        // ASP.NET Core Identity
        services.AddDbContext<McpIdentityDbContext>(opts =>
            opts.UseSqlite(identityConnectionString));

        services.AddIdentity<McpUser, IdentityRole>(opts =>
        {
            opts.Password.RequireDigit = false;
            opts.Password.RequiredLength = 4;
            opts.Password.RequireNonAlphanumeric = false;
            opts.Password.RequireUppercase = false;
            opts.Password.RequireLowercase = false;
            opts.User.RequireUniqueEmail = false;
        })
        .AddEntityFrameworkStores<McpIdentityDbContext>()
        .AddDefaultTokenProviders();

        // Duende IdentityServer
        var isBuilder = services.AddIdentityServer(idsvr =>
        {
            if (!string.IsNullOrWhiteSpace(options.IssuerUri))
                idsvr.IssuerUri = options.IssuerUri;

            idsvr.EmitStaticAudienceClaim = true;
        })
        .AddAspNetIdentity<McpUser>()
        .AddInMemoryIdentityResources(IdentityServerConfig.GetIdentityResources())
        .AddInMemoryApiScopes(IdentityServerConfig.GetApiScopes(options))
        .AddInMemoryApiResources(IdentityServerConfig.GetApiResources(options))
        .AddInMemoryClients(IdentityServerConfig.GetClients(options));

        return services;
    }

    public static WebApplication UseMcpIdentityServer(this WebApplication app)
    {
        var options = app.Configuration.GetSection(IdentityServerOptions.SectionName).Get<IdentityServerOptions>()
            ?? new IdentityServerOptions();

        if (!options.Enabled)
            return app;

        app.UseIdentityServer();

        return app;
    }
}
