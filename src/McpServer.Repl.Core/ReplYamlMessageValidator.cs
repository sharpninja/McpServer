// TR-MCP-REPL-001: YAML Envelope Protocol - schema-backed request validation
// TR-MCP-REPL-004: Command Registry and Dispatcher - validate before dispatch

using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace McpServer.Repl.Core;

internal sealed record ReplYamlMessageValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ReplYamlMessageValidationResult Success { get; } = new(true, Array.Empty<string>());
}

internal static class ReplYamlMessageValidator
{
    private const string MemoryIdPattern = "^MEMORY-[A-Z0-9]+(?:-[A-Z0-9]+)*-[0-9]{3,}$";

    private static readonly IReadOnlyDictionary<string, Action<IReadOnlyDictionary<string, object?>, List<string>>> MethodValidators = CreateValidators();

    public static ReplYamlMessageValidationResult ValidateRequest(IRequestPayload request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            errors.Add("payload.requestId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Method))
        {
            errors.Add("payload.method is required.");
        }

        var args = request.Params is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);

        if (errors.Count > 0)
        {
            return new ReplYamlMessageValidationResult(false, errors);
        }

        var method = request.Method ?? string.Empty;
        if (MethodValidators.TryGetValue(method, out var validate))
        {
            validate(args, errors);
        }
        else if (method.StartsWith("client.", StringComparison.Ordinal))
        {
            ValidateClientMethod(method, errors);
        }
        else if (method.StartsWith(SessionLogCommandShapes.MethodNamespace + ".", StringComparison.Ordinal) ||
                 method.StartsWith(TodoCommandShapes.MethodNamespace + ".", StringComparison.Ordinal) ||
                 method.StartsWith(RequirementsCommandShapes.MethodNamespace + ".", StringComparison.Ordinal) ||
                 method.StartsWith(MemoryCommandShapes.MethodNamespace + ".", StringComparison.Ordinal))
        {
            errors.Add($"No YAML schema is registered for method '{method}'.");
        }

        return errors.Count == 0
            ? ReplYamlMessageValidationResult.Success
            : new ReplYamlMessageValidationResult(false, errors);
    }

    private static IReadOnlyDictionary<string, Action<IReadOnlyDictionary<string, object?>, List<string>>> CreateValidators()
    {
        var validators = new Dictionary<string, Action<IReadOnlyDictionary<string, object?>, List<string>>>(StringComparer.Ordinal)
        {
            [SessionLogCommandShapes.BootstrapMethod] = NoParameters,
            [SessionLogCommandShapes.CurrentSessionMethod] = NoParameters,
            [SessionLogCommandShapes.OpenSessionMethod] = static (args, errors) =>
            {
                RequireText(args, "sessionId", errors);
                RequireText(args, "title", errors);
                OptionalText(args, "agent", errors);
                OptionalText(args, "sourceType", errors);
                OptionalText(args, "model", errors);
            },
            [SessionLogCommandShapes.BeginTurnMethod] = static (args, errors) =>
            {
                RequireText(args, "requestId", errors);
                RequireText(args, "queryTitle", errors);
                RequireText(args, "queryText", errors);
            },
            [SessionLogCommandShapes.UpdateTurnMethod] = static (args, errors) =>
            {
                OptionalText(args, "response", errors);
                OptionalText(args, "interpretation", errors);
                OptionalInteger(args, "tokenCount", errors);
                OptionalStringList(args, "tags", errors);
                OptionalStringList(args, "contextList", errors);
            },
            [SessionLogCommandShapes.CompleteTurnMethod] = static (args, errors) => RequireText(args, "response", errors),
            [SessionLogCommandShapes.FailTurnMethod] = static (args, errors) =>
            {
                RequireText(args, "errorMessage", errors);
                OptionalText(args, "errorCode", errors);
            },
            [SessionLogCommandShapes.AppendDialogMethod] = static (args, errors) =>
                RequireObjectArray(args, "dialogItems", errors, static (item, index, itemErrors) =>
                {
                    OptionalText(item, "timestamp", itemErrors, $"dialogItems[{index}].timestamp");
                    RequireText(item, "role", itemErrors, $"dialogItems[{index}].role");
                    RequireText(item, "content", itemErrors, $"dialogItems[{index}].content");
                    RequireText(item, "category", itemErrors, $"dialogItems[{index}].category");
                }),
            [SessionLogCommandShapes.AppendActionsMethod] = static (args, errors) =>
                RequireObjectArray(args, "actions", errors, static (item, index, itemErrors) =>
                {
                    OptionalInteger(item, "order", itemErrors, $"actions[{index}].order");
                    RequireText(item, "description", itemErrors, $"actions[{index}].description");
                    RequireText(item, "type", itemErrors, $"actions[{index}].type");
                    RequireText(item, "status", itemErrors, $"actions[{index}].status");
                    OptionalText(item, "filePath", itemErrors, $"actions[{index}].filePath");
                }),
            [SessionLogCommandShapes.QueryHistoryMethod] = static (args, errors) =>
            {
                OptionalText(args, "agent", errors);
                OptionalText(args, "sourceType", errors);
                OptionalText(args, "model", errors);
                OptionalText(args, "text", errors);
                OptionalText(args, "from", errors);
                OptionalText(args, "to", errors);
                OptionalInteger(args, "limit", errors);
                OptionalInteger(args, "offset", errors);
            },
            [SessionLogCommandShapes.ImportRecoveryMethod] = static (args, errors) =>
            {
                if (args.TryGetValue("sessionLog", out var value) && value is not null)
                {
                    if (ToDictionary(value) is null)
                    {
                        errors.Add("payload.params.sessionLog must be an object.");
                    }

                    return;
                }

                RequireOneText(args, errors, "payload.params", "sourceType", "agent");
                RequireText(args, "sessionId", errors);
                RequireText(args, "title", errors);
                OptionalText(args, "model", errors);
                OptionalText(args, "started", errors);
                OptionalText(args, "lastUpdated", errors);
                OptionalText(args, "status", errors);
                if (args.TryGetValue("turns", out var turnsValue) && turnsValue is not null)
                {
                    if (!TryGetArray(turnsValue, out var turns))
                    {
                        errors.Add("payload.params.turns must be an array.");
                        return;
                    }

                    for (var i = 0; i < turns.Count; i++)
                    {
                        var turn = ToDictionary(turns[i]);
                        if (turn is null)
                        {
                            errors.Add($"turns[{i}] must be an object.");
                            continue;
                        }

                        OptionalText(turn, "requestId", errors, $"turns[{i}].requestId");
                        OptionalText(turn, "timestamp", errors, $"turns[{i}].timestamp");
                        OptionalText(turn, "queryTitle", errors, $"turns[{i}].queryTitle");
                        OptionalText(turn, "queryText", errors, $"turns[{i}].queryText");
                        OptionalText(turn, "status", errors, $"turns[{i}].status");
                    }
                }
            },

            [TodoCommandShapes.QueryMethod] = static (args, errors) =>
            {
                OptionalText(args, "keyword", errors);
                OptionalText(args, "priority", errors);
                OptionalText(args, "section", errors);
                OptionalText(args, "id", errors);
                OptionalBoolean(args, "done", errors);
            },
            [TodoCommandShapes.GetMethod] = static (args, errors) => RequireText(args, "id", errors),
            [TodoCommandShapes.SelectMethod] = static (args, errors) => RequireText(args, "id", errors),
            [TodoCommandShapes.DeleteMethod] = static (args, errors) => RequireText(args, "id", errors),
            [TodoCommandShapes.AnalyzeRequirementsMethod] = static (args, errors) => RequireText(args, "id", errors),
            [TodoCommandShapes.StreamStatusMethod] = static (args, errors) => RequireText(args, "id", errors),
            [TodoCommandShapes.StreamPlanMethod] = static (args, errors) => RequireText(args, "id", errors),
            [TodoCommandShapes.StreamImplementMethod] = static (args, errors) => RequireText(args, "id", errors),
            [TodoCommandShapes.GetProjectionStatusMethod] = static (args, errors) => RequireText(args, "id", errors),
            [TodoCommandShapes.RepairProjectionMethod] = static (args, errors) => RequireText(args, "id", errors),
            [TodoCommandShapes.DeleteSelectedMethod] = NoParameters,
            [TodoCommandShapes.CurrentSelectionMethod] = NoParameters,
            [TodoCommandShapes.CreateMethod] = static (args, errors) =>
            {
                var source = UnwrapRequest(args);
                RequireText(source, "id", errors);
                RequireText(source, "title", errors);
                RequireText(source, "section", errors);
                RequireText(source, "priority", errors);
                ValidateTodoOptionalFields(source, errors);
            },
            [TodoCommandShapes.UpdateMethod] = static (args, errors) =>
            {
                RequireText(args, "id", errors);
                ValidateTodoOptionalFields(UnwrapRequest(args), errors);
            },
            [TodoCommandShapes.UpdateSelectedMethod] = static (args, errors) => ValidateTodoOptionalFields(UnwrapRequest(args), errors),

            [MemoryCommandShapes.ListMethod] = static (args, errors) =>
            {
                OptionalMemoryListScope(args, "scope", errors);
                OptionalText(args, "category", errors);
                OptionalText(args, "keyword", errors);
            },
            [MemoryCommandShapes.GetMethod] = static (args, errors) => RequireMemoryId(args, "id", errors),
            [MemoryCommandShapes.RemoveMethod] = static (args, errors) => RequireMemoryId(args, "id", errors),
            [MemoryCommandShapes.AddMethod] = static (args, errors) =>
            {
                var source = UnwrapRequest(args);
                OptionalMemoryId(source, "id", errors);
                RequireText(source, "category", errors);
                OptionalMemoryScope(source, "scope", errors);
                RequireText(source, "text", errors);
                OptionalText(source, "updatedBy", errors);
            },
            [MemoryCommandShapes.UpdateMethod] = static (args, errors) =>
            {
                RequireMemoryId(args, "id", errors);
                var source = UnwrapRequest(args);
                OptionalText(source, "category", errors);
                OptionalMemoryScope(source, "scope", errors);
                OptionalText(source, "text", errors);
                OptionalText(source, "updatedBy", errors);
            },

            [RequirementsCommandShapes.ListFrMethod] = static (args, errors) =>
            {
                OptionalText(args, "area", errors);
                OptionalText(args, "status", errors);
            },
            [RequirementsCommandShapes.GetFrMethod] = static (args, errors) => RequireText(args, "id", errors),
            [RequirementsCommandShapes.DeleteFrMethod] = static (args, errors) => RequireText(args, "id", errors),
            [RequirementsCommandShapes.CreateFrMethod] = static (args, errors) =>
            {
                RequireText(args, "id", errors);
                RequireText(args, "title", errors);
                RequireText(args, "description", errors);
                RequireText(args, "priority", errors);
                RequireText(args, "area", errors);
                OptionalText(args, "notes", errors);
            },
            [RequirementsCommandShapes.UpdateFrMethod] = static (args, errors) => ValidateRequirementPatch(args, errors),
            [RequirementsCommandShapes.CreateFrBatchMethod] = static (args, errors) =>
                RequireRecords(args, errors, static (record, index, itemErrors) =>
                {
                    RequireText(record, "id", itemErrors, $"records[{index}].id");
                    RequireText(record, "title", itemErrors, $"records[{index}].title");
                    RequireOneText(record, itemErrors, $"records[{index}]", "body", "description");
                    ValidateRequirementPatch(record, itemErrors, $"records[{index}].");
                }),
            [RequirementsCommandShapes.UpdateFrBatchMethod] = static (args, errors) =>
                RequireRecords(args, errors, static (record, index, itemErrors) =>
                {
                    RequireText(record, "id", itemErrors, $"records[{index}].id");
                    ValidateRequirementPatch(record, itemErrors, $"records[{index}].");
                }),

            [RequirementsCommandShapes.ListTrMethod] = static (args, errors) =>
            {
                OptionalText(args, "area", errors);
                OptionalText(args, "subarea", errors);
                OptionalText(args, "status", errors);
            },
            [RequirementsCommandShapes.GetTrMethod] = static (args, errors) => RequireText(args, "id", errors),
            [RequirementsCommandShapes.DeleteTrMethod] = static (args, errors) => RequireText(args, "id", errors),
            [RequirementsCommandShapes.CreateTrMethod] = static (args, errors) =>
            {
                RequireText(args, "id", errors);
                RequireText(args, "title", errors);
                RequireText(args, "description", errors);
                RequireText(args, "priority", errors);
                RequireText(args, "area", errors);
                RequireText(args, "subarea", errors);
                OptionalText(args, "notes", errors);
            },
            [RequirementsCommandShapes.UpdateTrMethod] = static (args, errors) => ValidateRequirementPatch(args, errors),
            [RequirementsCommandShapes.CreateTrBatchMethod] = static (args, errors) =>
                RequireRecords(args, errors, static (record, index, itemErrors) =>
                {
                    RequireText(record, "id", itemErrors, $"records[{index}].id");
                    RequireOneText(record, itemErrors, $"records[{index}]", "body", "description");
                    ValidateRequirementPatch(record, itemErrors, $"records[{index}].");
                }),
            [RequirementsCommandShapes.UpdateTrBatchMethod] = static (args, errors) =>
                RequireRecords(args, errors, static (record, index, itemErrors) =>
                {
                    RequireText(record, "id", itemErrors, $"records[{index}].id");
                    ValidateRequirementPatch(record, itemErrors, $"records[{index}].");
                }),

            [RequirementsCommandShapes.ListTestMethod] = static (args, errors) =>
            {
                OptionalText(args, "area", errors);
                OptionalText(args, "status", errors);
            },
            [RequirementsCommandShapes.GetTestMethod] = static (args, errors) => RequireText(args, "id", errors),
            [RequirementsCommandShapes.DeleteTestMethod] = static (args, errors) => RequireText(args, "id", errors),
            [RequirementsCommandShapes.CreateTestMethod] = static (args, errors) =>
            {
                RequireText(args, "id", errors);
                RequireText(args, "title", errors);
                RequireText(args, "description", errors);
                RequireText(args, "priority", errors);
                RequireText(args, "area", errors);
                OptionalText(args, "testType", errors);
                OptionalText(args, "notes", errors);
            },
            [RequirementsCommandShapes.UpdateTestMethod] = static (args, errors) => ValidateRequirementPatch(args, errors),
            [RequirementsCommandShapes.CreateTestBatchMethod] = static (args, errors) =>
                RequireRecords(args, errors, static (record, index, itemErrors) =>
                {
                    RequireText(record, "id", itemErrors, $"records[{index}].id");
                    RequireOneText(record, itemErrors, $"records[{index}]", "condition", "description", "body");
                    ValidateRequirementPatch(record, itemErrors, $"records[{index}].");
                }),
            [RequirementsCommandShapes.UpdateTestBatchMethod] = static (args, errors) =>
                RequireRecords(args, errors, static (record, index, itemErrors) =>
                {
                    RequireText(record, "id", itemErrors, $"records[{index}].id");
                    ValidateRequirementPatch(record, itemErrors, $"records[{index}].");
                }),
            [RequirementsCommandShapes.CreateBatchMethod] = static (args, errors) =>
                RequireRecords(args, errors, static (record, index, itemErrors) =>
                {
                    var kind = RequireText(record, "kind", itemErrors, $"records[{index}].kind")?.Trim().ToLowerInvariant();
                    RequireText(record, "id", itemErrors, $"records[{index}].id");
                    switch (kind)
                    {
                        case "fr":
                            RequireText(record, "title", itemErrors, $"records[{index}].title");
                            RequireOneText(record, itemErrors, $"records[{index}]", "body", "description");
                            break;
                        case "tr":
                            RequireOneText(record, itemErrors, $"records[{index}]", "body", "description");
                            break;
                        case "test":
                            RequireOneText(record, itemErrors, $"records[{index}]", "condition", "body", "description");
                            break;
                        case not null:
                            itemErrors.Add($"records[{index}].kind must be one of: fr, tr, test.");
                            break;
                    }

                    ValidateRequirementPatch(record, itemErrors, $"records[{index}].");
                }),
            [RequirementsCommandShapes.UpdateBatchMethod] = static (args, errors) =>
                RequireRecords(args, errors, static (record, index, itemErrors) =>
                {
                    var kind = RequireText(record, "kind", itemErrors, $"records[{index}].kind")?.Trim().ToLowerInvariant();
                    RequireText(record, "id", itemErrors, $"records[{index}].id");
                    if (kind is not ("fr" or "tr" or "test") && kind is not null)
                    {
                        itemErrors.Add($"records[{index}].kind must be one of: fr, tr, test.");
                    }

                    ValidateRequirementPatch(record, itemErrors, $"records[{index}].");
                }),
            [RequirementsCommandShapes.ListMappingsMethod] = static (args, errors) =>
            {
                OptionalText(args, "frId", errors);
                OptionalText(args, "trId", errors);
                OptionalText(args, "testId", errors);
            },
            [RequirementsCommandShapes.CreateMappingMethod] = static (args, errors) =>
            {
                RequireText(args, "frId", errors);
                if (!HasNonEmptyText(args, "trId") &&
                    !HasNonEmptyText(args, "testId") &&
                    !HasNonEmptyStringList(args, "trIds") &&
                    !HasNonEmptyStringList(args, "testIds"))
                {
                    errors.Add("payload.params must include at least one of trId, trIds, testId, or testIds.");
                }
            },
            [RequirementsCommandShapes.DeleteMappingMethod] = static (args, errors) =>
            {
                if (!HasNonEmptyText(args, "frId") &&
                    !HasNonEmptyText(args, "trId") &&
                    !HasNonEmptyText(args, "testId"))
                {
                    errors.Add("payload.params must include at least one of frId, trId, or testId.");
                }
            },
            [RequirementsCommandShapes.GenerateDocumentMethod] = static (args, errors) =>
            {
                OptionalEnum(args, "format", errors, "markdown", "yaml", "wiki");
                OptionalEnum(args, "docType", errors, "fr", "tr", "test", "matrix", "all", "functional", "technical", "testing", "mapping");
            },
            [RequirementsCommandShapes.IngestDocumentMethod] = static (args, errors) =>
            {
                OptionalText(args, "format", errors);
                OptionalText(args, "mergeStrategy", errors);
                OptionalText(args, "sourceFormat", errors);
                OptionalText(args, "preferredWikiFormat", errors);
                if (!HasNonEmptyText(args, "content") && !HasObject(args, "documents"))
                {
                    errors.Add("payload.params must include either content or documents.");
                }
            },
            [RequirementsCommandShapes.CurrentSelectionMethod] = NoParameters,
        };

        return validators;
    }

    private static void ValidateClientMethod(string method, ICollection<string> errors)
    {
        var parts = method.Split('.', 3);
        if (parts.Length != 3 || parts[0] != "client" || string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
        {
            errors.Add("payload.method must match client.<clientName>.<methodName>.");
        }
    }

    private static void NoParameters(IReadOnlyDictionary<string, object?> args, List<string> errors)
    {
        _ = args;
        _ = errors;
    }

    private static IReadOnlyDictionary<string, object?> UnwrapRequest(IReadOnlyDictionary<string, object?> args)
    {
        if (args.TryGetValue("request", out var request) && ToDictionary(request) is { } requestFields)
        {
            return requestFields;
        }

        return args;
    }

    private static void ValidateTodoOptionalFields(IReadOnlyDictionary<string, object?> args, List<string> errors)
    {
        OptionalText(args, "title", errors);
        OptionalText(args, "priority", errors);
        OptionalText(args, "section", errors);
        OptionalText(args, "estimate", errors);
        OptionalText(args, "note", errors);
        OptionalText(args, "completedDate", errors);
        OptionalText(args, "doneSummary", errors);
        OptionalText(args, "remaining", errors);
        OptionalText(args, "reference", errors);
        OptionalText(args, "phase", errors);
        OptionalBoolean(args, "done", errors);
        OptionalStringList(args, "description", errors);
        OptionalStringList(args, "technicalDetails", errors);
        OptionalStringList(args, "dependsOn", errors);
        OptionalStringList(args, "functionalRequirements", errors);
        OptionalStringList(args, "technicalRequirements", errors);
        OptionalTodoSubtasks(args, "implementationTasks", errors);
    }

    private static void ValidateRequirementPatch(IReadOnlyDictionary<string, object?> args, List<string> errors, string prefix = "")
    {
        OptionalText(args, "id", errors, $"{prefix}id");
        OptionalText(args, "kind", errors, $"{prefix}kind");
        OptionalText(args, "title", errors, $"{prefix}title");
        OptionalText(args, "body", errors, $"{prefix}body");
        OptionalText(args, "description", errors, $"{prefix}description");
        OptionalText(args, "condition", errors, $"{prefix}condition");
        OptionalText(args, "area", errors, $"{prefix}area");
        OptionalText(args, "subarea", errors, $"{prefix}subarea");
        OptionalText(args, "testType", errors, $"{prefix}testType");
        OptionalEnum(args, "priority", errors, $"{prefix}priority", "critical", "high", "medium", "low");
        OptionalEnum(args, "status", errors, $"{prefix}status", "pending", "in_progress", "completed", "deferred");
        OptionalText(args, "notes", errors, $"{prefix}notes");
    }

    private static void OptionalTodoSubtasks(IReadOnlyDictionary<string, object?> args, string key, List<string> errors)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return;
        }

        if (TryGetArray(value, out var values))
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i] is string)
                {
                    continue;
                }

                var item = ToDictionary(values[i]);
                if (item is null)
                {
                    errors.Add($"payload.params.{key}[{i}] must be a string or object.");
                    continue;
                }

                RequireText(item, "task", errors, $"{key}[{i}].task");
                OptionalBoolean(item, "done", errors, $"{key}[{i}].done");
            }
            return;
        }

        if (value is not string)
        {
            errors.Add($"payload.params.{key} must be a string or array.");
        }
    }

    private static void RequireRecords(
        IReadOnlyDictionary<string, object?> args,
        List<string> errors,
        Action<IReadOnlyDictionary<string, object?>, int, List<string>> validateRecord)
    {
        if (!args.TryGetValue("records", out var value) || value is null)
        {
            errors.Add("payload.params.records is required.");
            return;
        }

        if (!TryGetArray(value, out var records))
        {
            errors.Add("payload.params.records must be an array.");
            return;
        }

        if (records.Count == 0)
        {
            errors.Add("payload.params.records must contain at least one record.");
            return;
        }

        for (var i = 0; i < records.Count; i++)
        {
            var record = ToDictionary(records[i]);
            if (record is null)
            {
                errors.Add($"records[{i}] must be an object.");
                continue;
            }

            validateRecord(record, i, errors);
        }
    }

    private static void RequireObjectArray(
        IReadOnlyDictionary<string, object?> args,
        string key,
        List<string> errors,
        Action<IReadOnlyDictionary<string, object?>, int, List<string>> validateItem)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            errors.Add($"payload.params.{key} is required.");
            return;
        }

        if (!TryGetArray(value, out var values) || values.Count == 0)
        {
            errors.Add($"payload.params.{key} must be a non-empty array.");
            return;
        }

        for (var i = 0; i < values.Count; i++)
        {
            var item = ToDictionary(values[i]);
            if (item is null)
            {
                errors.Add($"{key}[{i}] must be an object.");
                continue;
            }

            validateItem(item, i, errors);
        }
    }

    private static string? RequireText(IReadOnlyDictionary<string, object?> args, string key, List<string> errors, string? path = null)
    {
        path ??= $"payload.params.{key}";
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            errors.Add($"{path} is required.");
            return null;
        }

        if (!TryGetText(value, out var text) || string.IsNullOrWhiteSpace(text))
        {
            errors.Add($"{path} must be a non-empty string.");
            return null;
        }

        return text;
    }

    private static void OptionalText(IReadOnlyDictionary<string, object?> args, string key, List<string> errors, string? path = null)
    {
        path ??= $"payload.params.{key}";
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return;
        }

        if (!TryGetText(value, out _))
        {
            errors.Add($"{path} must be a string.");
        }
    }

    private static void RequireOneText(IReadOnlyDictionary<string, object?> args, List<string> errors, string path, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!args.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            if (!TryGetText(value, out var text))
            {
                errors.Add($"{path}.{key} must be a string.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                return;
            }
        }

        errors.Add($"{path} must include one non-empty string field: {string.Join(", ", keys)}.");
    }

    private static void OptionalEnum(IReadOnlyDictionary<string, object?> args, string key, List<string> errors, params string[] values)
        => OptionalEnum(args, key, errors, $"payload.params.{key}", values);

    private static void OptionalEnum(IReadOnlyDictionary<string, object?> args, string key, List<string> errors, string path, params string[] values)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return;
        }

        if (!TryGetText(value, out var text) || string.IsNullOrWhiteSpace(text))
        {
            errors.Add($"{path} must be one of: {string.Join(", ", values)}.");
            return;
        }

        if (!values.Contains(text.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"{path} must be one of: {string.Join(", ", values)}.");
        }
    }

    private static void OptionalMemoryScope(IReadOnlyDictionary<string, object?> args, string key, List<string> errors, string? path = null)
        => OptionalEnum(args, key, errors, path ?? $"payload.params.{key}", "Global", "Workspace");

    private static void OptionalMemoryListScope(IReadOnlyDictionary<string, object?> args, string key, List<string> errors, string? path = null)
        => OptionalEnum(args, key, errors, path ?? $"payload.params.{key}", "Effective", "Global", "Workspace");

    private static void RequireMemoryId(IReadOnlyDictionary<string, object?> args, string key, List<string> errors, string? path = null)
    {
        path ??= $"payload.params.{key}";
        var value = RequireText(args, key, errors, path);
        if (value is not null && !IsValidMemoryId(value))
        {
            errors.Add($"{path} must match MEMORY-{{CATEGORY}}-{{NNN}}.");
        }
    }

    private static void OptionalMemoryId(IReadOnlyDictionary<string, object?> args, string key, List<string> errors, string? path = null)
    {
        path ??= $"payload.params.{key}";
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return;
        }

        if (!TryGetText(value, out var text) || string.IsNullOrWhiteSpace(text))
        {
            errors.Add($"{path} must match MEMORY-{{CATEGORY}}-{{NNN}}.");
            return;
        }

        if (!IsValidMemoryId(text))
        {
            errors.Add($"{path} must match MEMORY-{{CATEGORY}}-{{NNN}}.");
        }
    }

    private static bool IsValidMemoryId(string value)
        => Regex.IsMatch(value, MemoryIdPattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    private static void OptionalInteger(IReadOnlyDictionary<string, object?> args, string key, List<string> errors, string? path = null)
    {
        path ??= $"payload.params.{key}";
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return;
        }

        if (!TryGetInteger(value, out _))
        {
            errors.Add($"{path} must be an integer.");
        }
    }

    private static void OptionalBoolean(IReadOnlyDictionary<string, object?> args, string key, List<string> errors, string? path = null)
    {
        path ??= $"payload.params.{key}";
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return;
        }

        if (!TryGetBoolean(value, out _))
        {
            errors.Add($"{path} must be a boolean.");
        }
    }

    private static void OptionalStringList(IReadOnlyDictionary<string, object?> args, string key, List<string> errors)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return;
        }

        if (TryGetText(value, out _))
        {
            return;
        }

        if (!TryGetArray(value, out var values))
        {
            errors.Add($"payload.params.{key} must be a string or array of strings.");
            return;
        }

        for (var i = 0; i < values.Count; i++)
        {
            if (!TryGetText(values[i], out _))
            {
                errors.Add($"payload.params.{key}[{i}] must be a string.");
            }
        }
    }

    private static bool HasObject(IReadOnlyDictionary<string, object?> args, string key)
        => args.TryGetValue(key, out var value) && ToDictionary(value) is not null;

    private static bool HasNonEmptyText(IReadOnlyDictionary<string, object?> args, string key)
        => args.TryGetValue(key, out var value) && TryGetText(value, out var text) && !string.IsNullOrWhiteSpace(text);

    private static bool HasNonEmptyStringList(IReadOnlyDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        if (TryGetText(value, out var text))
        {
            return !string.IsNullOrWhiteSpace(text);
        }

        return TryGetArray(value, out var values) &&
               values.Any(item => TryGetText(item, out var itemText) && !string.IsNullOrWhiteSpace(itemText));
    }

    private static bool TryGetText(object? value, out string? text)
    {
        text = null;
        value = NormalizeJsonElement(value);
        if (value is string raw)
        {
            text = raw;
            return true;
        }

        return false;
    }

    private static bool TryGetInteger(object? value, out int number)
    {
        number = 0;
        value = NormalizeJsonElement(value);
        return value switch
        {
            int typed => TryAssign(typed, out number),
            long typed when typed is >= int.MinValue and <= int.MaxValue => TryAssign((int)typed, out number),
            string text => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number),
            _ => false,
        };
    }

    private static bool TryGetBoolean(object? value, out bool boolean)
    {
        boolean = false;
        value = NormalizeJsonElement(value);
        return value switch
        {
            bool typed => TryAssign(typed, out boolean),
            string text => bool.TryParse(text, out boolean),
            _ => false,
        };
    }

    private static bool TryGetArray(object? value, out IReadOnlyList<object?> values)
    {
        value = NormalizeJsonElement(value);
        if (value is IReadOnlyList<object?> readOnly)
        {
            values = readOnly;
            return true;
        }

        if (value is IEnumerable enumerable and not string)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(item);
            }

            values = list;
            return true;
        }

        values = Array.Empty<object?>();
        return false;
    }

    private static IReadOnlyDictionary<string, object?>? ToDictionary(object? value)
    {
        value = NormalizeJsonElement(value);
        if (value is IReadOnlyDictionary<string, object?> readOnly)
        {
            return new Dictionary<string, object?>(readOnly, StringComparer.OrdinalIgnoreCase);
        }

        if (value is IDictionary dictionary)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    normalized[key] = entry.Value;
                }
            }

            return normalized;
        }

        return null;
    }

    private static object? NormalizeJsonElement(object? value)
    {
        if (value is not JsonElement element)
        {
            return value;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(property => property.Name, property => (object?)NormalizeJsonElement(property.Value), StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => element.EnumerateArray().Select(item => NormalizeJsonElement(item)).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString(),
        };
    }

    private static bool TryAssign<T>(T value, out T target)
    {
        target = value;
        return true;
    }
}
