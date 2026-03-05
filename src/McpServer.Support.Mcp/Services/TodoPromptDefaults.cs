namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Default Copilot prompt templates for TODO operations.
/// Templates support <c>{id}</c>, <c>{title}</c>, and <c>{baseUrl}</c> placeholders.
/// The TODO item context block is appended separately by <see cref="TodoPromptService"/>.
/// </summary>
public static class TodoPromptDefaults
{
    /// <summary>Default status prompt template.</summary>
    public const string StatusPrompt =
        """
        Get the current status of TODO {id}. Report the following in structured markdown:

        - Title, priority, section, done status
        - Description and technical details
        - Implementation task completion progress (count done vs total)
        - Any blockers or next steps
        - Dependencies and their current status
        """;

    /// <summary>Default implement prompt template.</summary>
    public const string ImplementPrompt =
        """
        Implement TODO {id}: {title}

        Follow this procedure:

        1. IMPLEMENT TASKS: Work through each implementation task that is not yet done.
           After completing each task, immediately update the TODO via:
           curl -X PUT {baseUrl}/mcpserver/todo/{id} \
             -H "Content-Type: application/json" \
             -d '{"implementationTasks": [ ...full array with updated done flags... ]}'
           This makes progress visible in real time.

        2. UPDATE DEPENDENTS: After all tasks are complete, query all TODOs:
           curl {baseUrl}/mcpserver/todo
           Find any TODO whose dependsOn array contains "{id}". For each dependent:
           - Update its technicalDetails or note to reflect that {id} is now complete.
           - If all of the dependent's own dependencies are satisfied, update its
             remaining estimate and note accordingly.

        3. MARK DONE: When all implementationTasks are done, mark the TODO itself done:
           curl -X PUT {baseUrl}/mcpserver/todo/{id} \
             -H "Content-Type: application/json" \
             -d '{"done": true}'

        4. Update the session log throughout. Run to completion, do not wait for user.
        """;

    /// <summary>Default plan prompt template.</summary>
    public const string PlanPrompt =
        """
        Create an implementation plan in excruciating detail as a new TODO that TODO {id} depends on.

        1. Analyze the TODO item below and break it into granular, actionable implementation tasks.
           Each task should be small enough to complete in a single focused session.
           Include file paths, class names, method signatures, test scenarios, and integration points.

        2. Create a new TODO via:
           POST {baseUrl}/mcpserver/todo
           with the detailed plan as the body (id, title, section, priority, description,
           technicalDetails, implementationTasks).

        3. Update {id} via PUT {baseUrl}/mcpserver/todo/{id}
           to add the new plan TODO as a dependency in its dependsOn array.

        Finally, update the todo with the plan.
        """;

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="value"/> matches the corresponding
    /// built-in default (ignoring leading/trailing whitespace), meaning it should not be persisted.
    /// </summary>
    public static bool IsDefault(string promptName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var trimmed = value.Trim();
        return promptName switch
        {
            nameof(StatusPrompt) => string.Equals(trimmed, StatusPrompt.Trim(), StringComparison.Ordinal),
            nameof(ImplementPrompt) => string.Equals(trimmed, ImplementPrompt.Trim(), StringComparison.Ordinal),
            nameof(PlanPrompt) => string.Equals(trimmed, PlanPrompt.Trim(), StringComparison.Ordinal),
            _ => false,
        };
    }
}
