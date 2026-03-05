using McpServer.UI.Core;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Cqrs.Tests;

public sealed class TodoViewModelTests
{
    [Fact]
    public async Task TodoDetailViewModel_CreateAsync_PreservesExtendedFields()
    {
        var apiClient = Substitute.For<ITodoApiClient>();
        apiClient.CreateTodoAsync(Arg.Any<CreateTodoCommand>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var command = call.Arg<CreateTodoCommand>()!;
                return new TodoMutationOutcome(
                    true,
                    null,
                    new TodoDetail(
                        command.Id,
                        command.Title,
                        command.Section,
                        command.Priority,
                        false,
                        command.Estimate,
                        command.Note,
                        command.Description?.ToList() ?? [],
                        command.TechnicalDetails?.ToList() ?? [],
                        command.ImplementationTasks?.ToList() ?? [],
                        null,
                        null,
                        command.Remaining,
                        null,
                        null,
                        command.DependsOn?.ToList() ?? [],
                        command.FunctionalRequirements?.ToList() ?? [],
                        command.TechnicalRequirements?.ToList() ?? []));
            });

        using var sp = BuildProvider(apiClient);
        var vm = sp.GetRequiredService<TodoDetailViewModel>();
        vm.BeginNewDraft();
        vm.EditorId = "TODO-123";
        vm.EditorTitle = "Title";
        vm.EditorSection = "general";
        vm.EditorPriority = "high";
        vm.EditorEstimate = "2h";
        vm.EditorNote = "Note";
        vm.EditorRemaining = "Remaining";
        vm.EditorDescriptionText = "Line one\nLine two";
        vm.EditorTechnicalDetailsText = "Detail one";
        vm.EditorImplementationTasksText = "[x] Finished task";
        vm.EditorDependsOnText = "TODO-001";
        vm.EditorFunctionalRequirementsText = "FR-001";
        vm.EditorTechnicalRequirementsText = "TR-001";

        await vm.CreateAsync();

        await apiClient.Received(1).CreateTodoAsync(
            Arg.Is<CreateTodoCommand>(cmd =>
                cmd != null &&
                cmd.Id == "TODO-123" &&
                cmd.Note == "Note" &&
                cmd.Remaining == "Remaining" &&
                cmd.DependsOn!.SequenceEqual(new[] { "TODO-001" }) &&
                cmd.FunctionalRequirements!.SequenceEqual(new[] { "FR-001" }) &&
                cmd.TechnicalRequirements!.SequenceEqual(new[] { "TR-001" })),
            Arg.Any<CancellationToken>());

        Assert.Null(vm.ErrorMessage);
        Assert.Equal("TODO-123", vm.Detail?.Id);
    }

    [Fact]
    public async Task TodoDetailViewModel_SaveAsync_PreservesExtendedFields()
    {
        var apiClient = Substitute.For<ITodoApiClient>();
        apiClient.UpdateTodoAsync(Arg.Any<UpdateTodoCommand>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var command = call.Arg<UpdateTodoCommand>()!;
                return new TodoMutationOutcome(
                    true,
                    null,
                    new TodoDetail(
                        command.TodoId,
                        command.Title ?? "Title",
                        command.Section ?? "general",
                        command.Priority ?? "medium",
                        command.Done ?? false,
                        command.Estimate,
                        command.Note,
                        command.Description?.ToList() ?? [],
                        command.TechnicalDetails?.ToList() ?? [],
                        command.ImplementationTasks?.ToList() ?? [],
                        command.CompletedDate,
                        command.DoneSummary,
                        command.Remaining,
                        null,
                        null,
                        command.DependsOn?.ToList() ?? [],
                        command.FunctionalRequirements?.ToList() ?? [],
                        command.TechnicalRequirements?.ToList() ?? []));
            });

        using var sp = BuildProvider(apiClient);
        var vm = sp.GetRequiredService<TodoDetailViewModel>();
        vm.IsNewDraft = false;
        vm.EditorId = "TODO-123";
        vm.EditorTitle = "Title";
        vm.EditorSection = "general";
        vm.EditorPriority = "high";
        vm.EditorDone = true;
        vm.EditorEstimate = "2h";
        vm.EditorNote = "Note";
        vm.EditorCompletedDate = "2026-03-03";
        vm.EditorDoneSummary = "Done";
        vm.EditorRemaining = "Remaining";
        vm.EditorDescriptionText = "Line one\nLine two";
        vm.EditorTechnicalDetailsText = "Detail one";
        vm.EditorImplementationTasksText = "[x] Finished task";
        vm.EditorDependsOnText = "TODO-001";
        vm.EditorFunctionalRequirementsText = "FR-001";
        vm.EditorTechnicalRequirementsText = "TR-001";

        await vm.SaveAsync();

        await apiClient.Received(1).UpdateTodoAsync(
            Arg.Is<UpdateTodoCommand>(cmd =>
                cmd != null &&
                cmd.TodoId == "TODO-123" &&
                cmd.CompletedDate == "2026-03-03" &&
                cmd.DoneSummary == "Done" &&
                cmd.Remaining == "Remaining" &&
                cmd.DependsOn!.SequenceEqual(new[] { "TODO-001" }) &&
                cmd.FunctionalRequirements!.SequenceEqual(new[] { "FR-001" }) &&
                cmd.TechnicalRequirements!.SequenceEqual(new[] { "TR-001" })),
            Arg.Any<CancellationToken>());

        Assert.Null(vm.ErrorMessage);
        Assert.Equal("TODO-123", vm.Detail?.Id);
        Assert.Equal("2026-03-03", vm.EditorCompletedDate);
    }

    private static ServiceProvider BuildProvider(ITodoApiClient apiClient)
    {
        var auth = Substitute.For<IAuthorizationPolicyService>();
        auth.CanExecuteAction(Arg.Any<string>()).Returns(true);

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(apiClient);
        services.AddSingleton(auth);
        services.AddCqrs(typeof(TodoViewModelTests).Assembly);
        services.AddUiCore();
        return services.BuildServiceProvider();
    }
}
