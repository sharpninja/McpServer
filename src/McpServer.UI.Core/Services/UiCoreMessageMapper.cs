using System;
using System.Collections.Generic;
using System.Linq;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Models;
using ClientModels = McpServer.Client.Models;

namespace McpServer.UI.Core.Services;

internal static class UiCoreMessageMapper
{
    public static ListTodosResult ToListTodosResult(McpTodoQueryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var items = result.Items
            .Select(ToTodoListItem)
            .ToList();

        return new ListTodosResult(items, result.TotalCount);
    }

    public static TodoListItem ToTodoListItem(McpTodoFlatItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new TodoListItem(
            Id: item.Id,
            Title: item.Title,
            Section: item.Section,
            Priority: item.Priority,
            Done: item.Done,
            Estimate: item.Estimate);
    }

    public static TodoDetail ToTodoDetail(McpTodoFlatItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new TodoDetail(
            Id: item.Id,
            Title: item.Title,
            Section: item.Section,
            Priority: item.Priority,
            Done: item.Done,
            Estimate: item.Estimate,
            Note: item.Note,
            Description: item.Description?.ToList() ?? [],
            TechnicalDetails: item.TechnicalDetails?.ToList() ?? [],
            ImplementationTasks: item.ImplementationTasks?.Select(ToTodoTaskDetail).ToList() ?? [],
            CompletedDate: item.CompletedDate,
            DoneSummary: item.DoneSummary,
            Remaining: item.Remaining,
            PriorityNote: item.PriorityNote,
            Reference: item.Reference,
            DependsOn: item.DependsOn?.ToList() ?? [],
            FunctionalRequirements: item.FunctionalRequirements?.ToList() ?? [],
            TechnicalRequirements: item.TechnicalRequirements?.ToList() ?? []);
    }

    public static McpTodoFlatItem ToMcpTodoFlatItem(TodoDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        return new McpTodoFlatItem
        {
            Id = detail.Id,
            Title = detail.Title,
            Section = detail.Section,
            Priority = detail.Priority,
            Done = detail.Done,
            Estimate = detail.Estimate,
            Note = detail.Note,
            Description = detail.Description.ToList(),
            TechnicalDetails = detail.TechnicalDetails.ToList(),
            ImplementationTasks = detail.ImplementationTasks.Select(ToMcpTodoFlatTask).ToList(),
            CompletedDate = detail.CompletedDate,
            DoneSummary = detail.DoneSummary,
            Remaining = detail.Remaining,
            PriorityNote = detail.PriorityNote,
            Reference = detail.Reference,
            DependsOn = detail.DependsOn.ToList(),
            FunctionalRequirements = detail.FunctionalRequirements.ToList(),
            TechnicalRequirements = detail.TechnicalRequirements.ToList()
        };
    }

    public static TodoMutationOutcome ToTodoMutationOutcome(McpTodoMutationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TodoMutationOutcome(
            Success: result.Success,
            Error: result.Error,
            Item: result.Item is null ? null : ToTodoDetail(result.Item));
    }

    public static TodoRequirementsAnalysis ToTodoRequirementsAnalysis(McpRequirementsAnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TodoRequirementsAnalysis(
            Success: result.Success,
            FunctionalRequirements: result.FunctionalRequirements?.ToList() ?? [],
            TechnicalRequirements: result.TechnicalRequirements?.ToList() ?? [],
            Error: result.Error,
            CopilotResponse: result.CopilotResponse);
    }

    public static McpTodoCreateRequest ToTodoCreateRequest(CreateTodoCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new McpTodoCreateRequest
        {
            Id = command.Id,
            Title = command.Title,
            Section = command.Section,
            Priority = command.Priority,
            Estimate = command.Estimate,
            Note = command.Note,
            Remaining = command.Remaining,
            Description = command.Description?.ToList(),
            TechnicalDetails = command.TechnicalDetails?.ToList(),
            ImplementationTasks = command.ImplementationTasks?.Select(ToMcpTodoFlatTask).ToList(),
            DependsOn = command.DependsOn?.ToList(),
            FunctionalRequirements = command.FunctionalRequirements?.ToList(),
            TechnicalRequirements = command.TechnicalRequirements?.ToList()
        };
    }

    public static McpTodoUpdateRequest ToTodoUpdateRequest(UpdateTodoCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new McpTodoUpdateRequest
        {
            Title = command.Title,
            Section = command.Section,
            Priority = command.Priority,
            Done = command.Done,
            Estimate = command.Estimate,
            Note = command.Note,
            CompletedDate = command.CompletedDate,
            DoneSummary = command.DoneSummary,
            Remaining = command.Remaining,
            Description = command.Description?.ToList(),
            TechnicalDetails = command.TechnicalDetails?.ToList(),
            ImplementationTasks = command.ImplementationTasks?.Select(ToMcpTodoFlatTask).ToList(),
            DependsOn = command.DependsOn?.ToList(),
            FunctionalRequirements = command.FunctionalRequirements?.ToList(),
            TechnicalRequirements = command.TechnicalRequirements?.ToList()
        };
    }

    public static ListWorkspacesResult ToListWorkspacesResult(McpWorkspaceQueryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var items = result.Items
            .Where(static item => !string.IsNullOrWhiteSpace(item.WorkspacePath))
            .Select(ToWorkspaceSummary)
            .ToList();

        return new ListWorkspacesResult(items, result.TotalCount);
    }

    public static WorkspaceSummary ToWorkspaceSummary(McpWorkspaceItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new WorkspaceSummary(
            WorkspacePath: item.WorkspacePath ?? string.Empty,
            Name: item.Name ?? string.Empty,
            IsPrimary: item.IsPrimary ?? false,
            IsEnabled: item.IsEnabled ?? true);
    }

    public static WorkspaceDetail ToWorkspaceDetail(McpWorkspaceItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new WorkspaceDetail(
            WorkspacePath: item.WorkspacePath ?? string.Empty,
            Name: item.Name ?? string.Empty,
            TodoPath: item.TodoPath ?? string.Empty,
            DataDirectory: item.DataDirectory,
            TunnelProvider: item.TunnelProvider,
            IsPrimary: item.IsPrimary ?? false,
            IsEnabled: item.IsEnabled ?? true,
            RunAs: item.RunAs,
            PromptTemplate: item.PromptTemplate,
            StatusPrompt: item.StatusPrompt ?? string.Empty,
            ImplementPrompt: item.ImplementPrompt ?? string.Empty,
            PlanPrompt: item.PlanPrompt ?? string.Empty,
            DateTimeCreated: item.DateTimeCreated ?? DateTimeOffset.MinValue,
            DateTimeModified: item.DateTimeModified ?? item.DateTimeCreated ?? DateTimeOffset.MinValue,
            BannedLicenses: item.BannedLicenses?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList() ?? [],
            BannedCountriesOfOrigin: item.BannedCountriesOfOrigin?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList() ?? [],
            BannedOrganizations: item.BannedOrganizations?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList() ?? [],
            BannedIndividuals: item.BannedIndividuals?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList() ?? []);
    }

    public static WorkspaceMutationOutcome ToWorkspaceMutationOutcome(McpWorkspaceMutationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new WorkspaceMutationOutcome(
            Success: result.Success,
            Error: result.Error,
            Item: result.Workspace is null ? null : ToWorkspaceDetail(result.Workspace));
    }

    public static WorkspaceProcessState ToWorkspaceProcessState(McpWorkspaceProcessStatus result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new WorkspaceProcessState(
            IsRunning: result.IsRunning,
            Pid: result.Pid,
            Uptime: result.Uptime,
            Port: result.Port,
            Error: result.Error);
    }

    public static WorkspaceHealthState ToWorkspaceHealthState(McpWorkspaceHealthResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new WorkspaceHealthState(
            Success: result.Success,
            StatusCode: result.StatusCode,
            Url: result.Url,
            Body: result.Body,
            Error: result.Error);
    }

    public static WorkspaceGlobalPromptState ToWorkspaceGlobalPromptState(McpWorkspaceGlobalPromptResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new WorkspaceGlobalPromptState(
            Template: result.Template ?? string.Empty,
            IsDefault: result.IsDefault);
    }

    public static McpWorkspaceCreateRequest ToWorkspaceCreateRequest(CreateWorkspaceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new McpWorkspaceCreateRequest
        {
            WorkspacePath = command.WorkspacePath,
            Name = command.Name,
            TodoPath = command.TodoPath,
            DataDirectory = command.DataDirectory,
            TunnelProvider = command.TunnelProvider,
            RunAs = command.RunAs,
            IsPrimary = command.IsPrimary,
            IsEnabled = command.IsEnabled,
            PromptTemplate = command.PromptTemplate,
            StatusPrompt = command.StatusPrompt,
            ImplementPrompt = command.ImplementPrompt,
            PlanPrompt = command.PlanPrompt,
            BannedLicenses = command.BannedLicenses?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            BannedCountriesOfOrigin = command.BannedCountriesOfOrigin?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            BannedOrganizations = command.BannedOrganizations?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            BannedIndividuals = command.BannedIndividuals?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList()
        };
    }

    public static McpWorkspaceUpdateRequest ToWorkspaceUpdateRequest(UpdateWorkspaceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new McpWorkspaceUpdateRequest
        {
            Name = command.Name,
            TodoPath = command.TodoPath,
            DataDirectory = command.DataDirectory,
            TunnelProvider = command.TunnelProvider,
            RunAs = command.RunAs,
            IsPrimary = command.IsPrimary,
            IsEnabled = command.IsEnabled,
            PromptTemplate = command.PromptTemplate,
            StatusPrompt = command.StatusPrompt,
            ImplementPrompt = command.ImplementPrompt,
            PlanPrompt = command.PlanPrompt,
            BannedLicenses = command.BannedLicenses?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            BannedCountriesOfOrigin = command.BannedCountriesOfOrigin?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            BannedOrganizations = command.BannedOrganizations?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            BannedIndividuals = command.BannedIndividuals?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList()
        };
    }

    private static TodoTaskDetail ToTodoTaskDetail(McpTodoFlatTask task)
        => new(task.Task ?? string.Empty, task.Done);

    private static McpTodoFlatTask ToMcpTodoFlatTask(TodoTaskDetail task)
        => new() { Task = task.Task, Done = task.Done };

    // ── Voice mapping ───────────────────────────────────────────────────────

    public static McpVoiceSessionCreateRequest ToVoiceSessionCreateRequest(CreateVoiceSessionCommand command)
        => new()
        {
            Language = command.Language,
            DeviceId = command.DeviceId,
            ClientName = command.ClientName,
        };

    public static McpVoiceTurnRequest ToVoiceTurnRequest(SubmitVoiceTurnCommand command)
        => new()
        {
            UserTranscriptText = command.UserTranscriptText,
            Language = command.Language,
            ClientTimestampUtc = command.ClientTimestampUtc,
        };

    public static VoiceSessionInfo ToVoiceSessionInfo(McpVoiceSessionCreateResponse response)
        => new(
            response.SessionId,
            response.Status,
            response.Language,
            response.ModelRequested,
            response.ModelResolved);

    public static VoiceTurnInfo ToVoiceTurnInfo(McpVoiceTurnResponse response)
        => new(
            response.SessionId,
            response.TurnId,
            response.Status,
            response.AssistantDisplayText,
            response.AssistantSpeakText,
            response.ToolCalls?.Select(ToVoiceToolCallInfo).ToList() ?? [],
            response.Error,
            response.LatencyMs,
            response.ModelRequested,
            response.ModelResolved);

    public static VoiceToolCallInfo ToVoiceToolCallInfo(McpVoiceToolCallRecord record)
        => new(
            record.TurnId,
            record.ToolName,
            record.Step,
            record.ArgumentsJson,
            record.Status,
            record.IsMutation,
            record.ResultSummary,
            record.Error);

    public static VoiceInterruptInfo ToVoiceInterruptInfo(McpVoiceInterruptResponse response)
        => new(response.SessionId, response.Interrupted, response.Status);

    public static VoiceSessionStatusInfo ToVoiceSessionStatusInfo(McpVoiceSessionStatus status)
        => new(
            status.SessionId,
            status.Status,
            status.Language,
            status.CreatedUtc,
            status.LastUpdatedUtc,
            status.IsTurnActive,
            status.LastError,
            status.LastTurnId,
            status.TurnCounter,
            status.TranscriptCount);

    public static VoiceTranscriptInfo ToVoiceTranscriptInfo(McpVoiceTranscriptResponse response)
        => new(response.SessionId, response.Items.Select(ToVoiceTranscriptEntryInfo).ToList());

    public static VoiceTranscriptEntryInfo ToVoiceTranscriptEntryInfo(McpVoiceTranscriptEntry entry)
        => new(entry.TimestampUtc, entry.TurnId, entry.Role, entry.Category, entry.Text);

    public static McpVoiceToolCallRecord ToMcpVoiceToolCallRecord(VoiceToolCallInfo info)
        => new()
        {
            TurnId = info.TurnId,
            ToolName = info.ToolName,
            Step = info.Step,
            ArgumentsJson = info.ArgumentsJson,
            Status = info.Status,
            IsMutation = info.IsMutation,
            ResultSummary = info.ResultSummary,
            Error = info.Error,
        };

    public static McpVoiceTranscriptEntry ToMcpVoiceTranscriptEntry(VoiceTranscriptEntryInfo info)
        => new()
        {
            TimestampUtc = info.TimestampUtc,
            TurnId = info.TurnId,
            Role = info.Role,
            Category = info.Category,
            Text = info.Text,
        };

    // ── Session log mapping ─────────────────────────────────────────────────

    public static SessionLogSummary ToSessionLogSummary(Models.Json.UnifiedSessionLog log)
        => new(
            log.SessionId ?? string.Empty,
            log.SourceType ?? string.Empty,
            log.Title ?? string.Empty,
            log.Status ?? string.Empty,
            log.Model,
            log.Started?.ToString("o"),
            log.LastUpdated?.ToString("o"),
            log.EntryCount);

    public static SessionLogDetail ToSessionLogDetail(Models.Json.UnifiedSessionLog log)
        => new(
            log.SessionId ?? string.Empty,
            log.SourceType ?? string.Empty,
            log.Title ?? string.Empty,
            log.Status ?? string.Empty,
            log.Model,
            log.Started?.ToString("o"),
            log.LastUpdated?.ToString("o"),
            log.EntryCount,
            log.TotalTokens > 0 ? log.TotalTokens : null,
            log.CursorSessionLabel,
            log.Workspace is null ? null : new SessionLogWorkspaceInfo(
                log.Workspace.Project,
                log.Workspace.TargetFramework,
                log.Workspace.Repository,
                log.Workspace.Branch),
            log.CopilotStatistics is null ? null : new SessionLogCopilotStatistics(
                log.CopilotStatistics.AverageSuccessScore,
                (int?)log.CopilotStatistics.TotalNetTokens,
                (int?)log.CopilotStatistics.TotalNetPremiumRequests,
                log.CopilotStatistics.CompletedCount,
                log.CopilotStatistics.InProgressCount),
            log.Entries.Select(ToSessionLogEntryDetail).ToList());

    public static SessionLogEntryDetail ToSessionLogEntryDetail(Models.Json.UnifiedRequestEntry entry)
        => new(
            entry.RequestId ?? string.Empty,
            entry.Timestamp?.ToString("o"),
            entry.QueryTitle,
            entry.QueryText,
            entry.Response,
            entry.Interpretation,
            entry.Status,
            entry.Model,
            entry.ModelProvider,
            entry.TokenCount > 0 ? entry.TokenCount : null,
            string.IsNullOrWhiteSpace(entry.FailureNote) ? null : entry.FailureNote,
            entry.Score,
            entry.IsPremium,
            entry.Tags?.ToList() ?? [],
            entry.ContextList?.ToList() ?? [],
            [], // DesignDecisions — not in unified model
            [], // RequirementsDiscovered — not in unified model
            [], // FilesModified — not in unified model
            [], // Blockers — not in unified model
            entry.Actions?.Select(ToSessionLogActionDetail).ToList() ?? [],
            entry.ProcessingDialog?.Select(ToSessionLogDialogDetail).ToList() ?? [],
            []); // Commits — not in unified model

    public static SessionLogActionDetail ToSessionLogActionDetail(Models.Json.UnifiedAction action)
        => new(action.Order, action.Description, action.Type, action.Status, action.FilePath);

    public static SessionLogDialogDetail ToSessionLogDialogDetail(Models.Json.UnifiedProcessingDialogItem dialog)
        => new(dialog.Timestamp, dialog.Role, dialog.Category, dialog.Content);

    // ── Session log reverse mapping (UI.Core → client DTO for submit) ───────

    public static ClientModels.UnifiedSessionLogDto ToUnifiedSessionLogDto(SessionLogDetail detail)
        => new()
        {
            SourceType = detail.SourceType,
            SessionId = detail.SessionId,
            Title = detail.Title,
            Model = detail.Model,
            Started = detail.Started,
            LastUpdated = detail.LastUpdated,
            Status = detail.Status,
            EntryCount = detail.EntryCount,
            TotalTokens = detail.TotalTokens,
            CursorSessionLabel = detail.CursorSessionLabel,
            Workspace = detail.Workspace is null ? null : new ClientModels.WorkspaceInfoDto
            {
                Project = detail.Workspace.Project,
                TargetFramework = detail.Workspace.TargetFramework,
                Repository = detail.Workspace.Repository,
                Branch = detail.Workspace.Branch,
            },
            CopilotStatistics = detail.CopilotStatistics is null ? null : new ClientModels.CopilotStatisticsDto
            {
                AverageSuccessScore = detail.CopilotStatistics.AverageSuccessScore,
                TotalNetTokens = detail.CopilotStatistics.TotalNetTokens,
                TotalNetPremiumRequests = detail.CopilotStatistics.TotalNetPremiumRequests,
                CompletedCount = detail.CopilotStatistics.CompletedCount,
                InProgressCount = detail.CopilotStatistics.InProgressCount,
            },
            Entries = detail.Entries.Select(ToUnifiedRequestEntryDto).ToList(),
        };

    public static ClientModels.UnifiedRequestEntryDto ToUnifiedRequestEntryDto(SessionLogEntryDetail entry)
        => new()
        {
            RequestId = entry.RequestId,
            Timestamp = entry.Timestamp,
            QueryTitle = entry.QueryTitle,
            QueryText = entry.QueryText,
            Response = entry.Response,
            Interpretation = entry.Interpretation,
            Status = entry.Status,
            Model = entry.Model,
            ModelProvider = entry.ModelProvider,
            TokenCount = entry.TokenCount,
            FailureNote = entry.FailureNote,
            Score = entry.Score,
            IsPremium = entry.IsPremium,
            Tags = entry.Tags?.ToList(),
            ContextList = entry.ContextList?.ToList(),
            DesignDecisions = entry.DesignDecisions?.ToList(),
            RequirementsDiscovered = entry.RequirementsDiscovered?.ToList(),
            FilesModified = entry.FilesModified?.ToList(),
            Blockers = entry.Blockers?.ToList(),
            Actions = entry.Actions?.Select(ToActionDto).ToList(),
            ProcessingDialog = entry.ProcessingDialog?.Select(ToProcessingDialogItemDto).ToList(),
            Commits = entry.Commits?.Select(ToSessionLogCommitDto).ToList(),
        };

    public static ClientModels.UnifiedActionDto ToActionDto(SessionLogActionDetail action)
        => new()
        {
            Order = action.Order,
            Description = action.Description,
            Type = action.Type,
            Status = action.Status,
            FilePath = action.FilePath,
        };

    public static ClientModels.ProcessingDialogItemDto ToProcessingDialogItemDto(SessionLogDialogDetail dialog)
        => new()
        {
            Timestamp = dialog.Timestamp,
            Role = dialog.Role,
            Content = dialog.Content,
            Category = dialog.Category,
        };

    public static ClientModels.SessionLogCommitDto ToSessionLogCommitDto(SessionLogCommitDetail commit)
        => new()
        {
            Sha = commit.Sha,
            Branch = commit.Branch,
            Message = commit.Message,
            Author = commit.Author,
            Timestamp = commit.Timestamp,
            FilesChanged = commit.FilesChanged?.ToList(),
        };
}

