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
        IConfiguration configuration)
    {
        var options = configuration.GetSection(IdentityServerOptions.SectionName).Get<IdentityServerOptions>()
            ?? new IdentityServerOptions();

        if (!options.Enabled)
            return services;

        var identityConnectionString = options.ConnectionString;
        if (string.IsNullOrWhiteSpace(identityConnectionString))
        {
            // Default to SQL Server LocalDB
            identityConnectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={options.DatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true";
        }

        // ASP.NET Core Identity backed by SQL Server
        services.AddDbContext<McpIdentityDbContext>(opts =>
            opts.UseSqlServer(identityConnectionString));

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
            idsvr.UserInteraction.DeviceVerificationUrl = "/device";
            idsvr.UserInteraction.DeviceVerificationUserCodeParameter = "userCode";
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
