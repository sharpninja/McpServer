using McpServer.Support.Mcp.Storage.Entities;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Built-in agent type defaults for well-known AI coding agents.
/// These are seeded into the database on first run and cannot be deleted.
/// </summary>
public static class AgentDefaults
{
    /// <summary>Returns the list of built-in agent definitions.</summary>
    public static IReadOnlyList<AgentDefinitionEntity> GetBuiltInDefaults()
    {
        var now = DateTime.UtcNow;
        return
        [
            new AgentDefinitionEntity
            {
                Id = "copilot",
                DisplayName = "GitHub Copilot",
                DefaultLaunchCommand = "code --wait .",
                DefaultInstructionFile = ".github/copilot-instructions.md",
                DefaultModelsJson = "[\"gpt-4o\",\"claude-sonnet-4-20250514\"]",
                DefaultBranchStrategy = "feature/copilot/{task}",
                DefaultSeedPrompt = "You are GitHub Copilot working on this project. Follow the coding conventions and patterns established in the codebase.",
                IsBuiltIn = true,
                CreatedAt = now,
                ModifiedAt = now
            },
            new AgentDefinitionEntity
            {
                Id = "cline",
                DisplayName = "Cline",
                DefaultLaunchCommand = "cline",
                DefaultInstructionFile = ".clinerules",
                DefaultModelsJson = "[\"claude-sonnet-4-20250514\"]",
                DefaultBranchStrategy = "feature/cline/{task}",
                DefaultSeedPrompt = "You are Cline working on this project. Always read AGENTS-README-FIRST.yaml at the start of each session and log all interactions to the session log API.",
                IsBuiltIn = true,
                CreatedAt = now,
                ModifiedAt = now
            },
            new AgentDefinitionEntity
            {
                Id = "cursor",
                DisplayName = "Cursor",
                DefaultLaunchCommand = "cursor .",
                DefaultInstructionFile = ".cursorrules",
                DefaultModelsJson = "[\"claude-sonnet-4-20250514\"]",
                DefaultBranchStrategy = "feature/cursor/{task}",
                DefaultSeedPrompt = "You are Cursor working on this project. Follow the project's coding standards and conventions.",
                IsBuiltIn = true,
                CreatedAt = now,
                ModifiedAt = now
            },
            new AgentDefinitionEntity
            {
                Id = "windsurf",
                DisplayName = "Windsurf",
                DefaultLaunchCommand = "windsurf .",
                DefaultInstructionFile = ".windsurfrules",
                DefaultModelsJson = "[\"claude-sonnet-4-20250514\"]",
                DefaultBranchStrategy = "feature/windsurf/{task}",
                DefaultSeedPrompt = "You are Windsurf working on this project. Follow the project's coding standards and conventions.",
                IsBuiltIn = true,
                CreatedAt = now,
                ModifiedAt = now
            },
            new AgentDefinitionEntity
            {
                Id = "claude-code",
                DisplayName = "Claude Code",
                DefaultLaunchCommand = "claude",
                DefaultInstructionFile = "CLAUDE.md",
                DefaultModelsJson = "[\"claude-sonnet-4-20250514\"]",
                DefaultBranchStrategy = "feature/claude/{task}",
                DefaultSeedPrompt = "You are Claude Code working on this project. Follow the project's coding standards and conventions.",
                IsBuiltIn = true,
                CreatedAt = now,
                ModifiedAt = now
            },
            new AgentDefinitionEntity
            {
                Id = "aider",
                DisplayName = "Aider",
                DefaultLaunchCommand = "aider",
                DefaultInstructionFile = ".aider.conf.yml",
                DefaultModelsJson = "[\"gpt-4o\",\"claude-sonnet-4-20250514\"]",
                DefaultBranchStrategy = "feature/aider/{task}",
                DefaultSeedPrompt = "You are Aider working on this project.",
                IsBuiltIn = true,
                CreatedAt = now,
                ModifiedAt = now
            },
            new AgentDefinitionEntity
            {
                Id = "continue",
                DisplayName = "Continue",
                DefaultLaunchCommand = "code --wait .",
                DefaultInstructionFile = ".continuerules",
                DefaultModelsJson = "[\"claude-sonnet-4-20250514\"]",
                DefaultBranchStrategy = "feature/continue/{task}",
                DefaultSeedPrompt = "You are Continue working on this project. Follow the project's coding standards and conventions.",
                IsBuiltIn = true,
                CreatedAt = now,
                ModifiedAt = now
            }
        ];
    }
}
