using System.Text.Json;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Cqrs.Tests;

/// <summary>Tests for <see cref="ViewModelRegistry"/> and <see cref="ViewModelCommandAttribute"/>.</summary>
public class ViewModelRegistryTests
{
    private static (ServiceProvider sp, IViewModelRegistry registry) BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddCqrs(typeof(ViewModelRegistryTests).Assembly);
        services.AddTransient<WorkspaceListViewModel>();
        services.AddTransient<WorkspaceContextViewModel>();
        services.AddTransient<WorkspacePolicyViewModel>();

        var sp = services.BuildServiceProvider();
        var registry = new ViewModelRegistry(sp, [typeof(WorkspaceListViewModel).Assembly]);
        return (sp, registry);
    }

    [Fact]
    public void Discovers_ViewModels_ByAlias()
    {
        var (sp, registry) = BuildRegistry();
        using var _ = sp;

        Assert.True(registry.ViewModels.ContainsKey("list-workspaces"));
        Assert.True(registry.ViewModels.ContainsKey("update-policy"));
    }

    [Fact]
    public void Discovers_ViewModels_ByClassName()
    {
        var (sp, registry) = BuildRegistry();
        using var _ = sp;

        Assert.True(registry.ViewModels.ContainsKey("WorkspaceListViewModel"));
        Assert.True(registry.ViewModels.ContainsKey("WorkspacePolicyViewModel"));
    }

    [Fact]
    public void Resolve_ByAlias_ReturnsCorrectType()
    {
        var (sp, registry) = BuildRegistry();
        using var _ = sp;

        var vm = registry.Resolve("list-workspaces");
        Assert.IsType<WorkspaceListViewModel>(vm);
    }

    [Fact]
    public void Resolve_Unknown_Throws()
    {
        var (sp, registry) = BuildRegistry();
        using var _ = sp;

        Assert.Throws<InvalidOperationException>(() => registry.Resolve("nonexistent"));
    }

    [Fact]
    public void GetPrimaryCommand_FindsCommand()
    {
        var (sp, registry) = BuildRegistry();
        using var _ = sp;

        var vm = registry.Resolve("list-workspaces");
        var cmd = registry.GetPrimaryCommand(vm);
        Assert.NotNull(cmd);
    }

    [Fact]
    public void SetProperties_SetsValues()
    {
        var (sp, registry) = BuildRegistry();
        using var _ = sp;

        var vm = registry.Resolve("list-workspaces");
        var json = JsonSerializer.Deserialize<JsonElement>("{\"errorMessage\":\"/test/path\",\"totalCount\":7}");
        registry.SetProperties(vm, json);

        var listVm = (WorkspaceListViewModel)vm;
        Assert.Equal("/test/path", listVm.ErrorMessage);
        Assert.Equal(7, listVm.TotalCount);
    }

    [Fact]
    public void GetResult_ReturnsLastResult()
    {
        var (sp, registry) = BuildRegistry();
        using var _ = sp;

        var vm = registry.Resolve("list-workspaces");
        // Before execution, LastResult should be null
        var result = registry.GetResult(vm);
        Assert.Null(result);
    }
}
