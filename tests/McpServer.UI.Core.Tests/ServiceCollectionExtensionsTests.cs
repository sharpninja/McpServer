using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Tests.TestInfrastructure;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.UI.Core.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddUiCore_RegistersVoiceViewModelAlias()
    {
        using var sp = UiCoreTestHost.Create();
        var registry = sp.GetRequiredService<IViewModelRegistry>();

        Assert.True(registry.ViewModels.ContainsKey("voice-session"));
        Assert.Equal(typeof(VoiceViewModel), registry.ViewModels["voice-session"]);
    }

    [Fact]
    public void AddUiCore_ResolvesVoiceViewModelAsTransient()
    {
        using var sp = UiCoreTestHost.Create();

        var first = sp.GetRequiredService<VoiceViewModel>();
        var second = sp.GetRequiredService<VoiceViewModel>();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }
}
