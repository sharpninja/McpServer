using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>CLI/exec-oriented ViewModel for auth config discovery.</summary>
[ViewModelCommand("get-auth-config", Description = "Get public auth/OIDC configuration")]
internal sealed partial class AuthConfigViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<AuthConfigSnapshot> _command;

    public AuthConfigViewModel(Dispatcher dispatcher)
    {
        _command = new CqrsQueryCommand<AuthConfigSnapshot>(dispatcher, static () => new GetAuthConfigQuery());
    }

    public IAsyncRelayCommand GetCommand => _command;
    public IAsyncRelayCommand PrimaryCommand => GetCommand;
    public Result<AuthConfigSnapshot>? LastResult => _command.LastResult;
}

