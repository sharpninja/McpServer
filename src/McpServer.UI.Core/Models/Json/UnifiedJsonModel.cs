using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Models.Json
{
    public class UnifiedSessionLog
    {
        public string SourceType { get; set; } = "";
        public string SessionId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Model { get; set; } = "";
        public DateTime? Started { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string Status { get; set; } = "";

        // Unified Stats (converter allows number/string from JSON, e.g. totalTokens as double)
        [JsonConverter(typeof(FlexibleInt32Converter))]
        public int EntryCount { get; set; }
        [JsonConverter(typeof(FlexibleInt32Converter))]
        public int TotalTokens { get; set; }

        [JsonConverter(typeof(WorkspaceInfoConverter))]
        public WorkspaceInfo? Workspace { get; set; }

        public List<UnifiedRequestEntry> Entries { get; set; } = new();

        // Specifics retained for fidelity
        public StatisticsInfo? CopilotStatistics { get; set; }
        public string? CursorSessionLabel { get; set; }
    }

    public class UnifiedRequestEntry
    {
        public string RequestId { get; set; } = "";
        public DateTime? Timestamp { get; set; }

        /// <summary>Timestamp in local time for display (short date/time).</summary>
        public string TimestampLocalDisplay => Timestamp.HasValue ? ToLocalTime(Timestamp.Value).ToString("g") : "";

        private static DateTime ToLocalTime(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Local) return dt;
            if (dt.Kind == DateTimeKind.Utc) return dt.ToLocalTime();
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
        }

        public string Model { get; set; } = "";
        public string ModelProvider { get; set; } = "";
        public string Agent { get; set; } = "";

        // Unified Query/Response
        public string QueryText { get; set; } = "";
        public string QueryTitle { get; set; } = "";
        public string Response { get; set; } = "";

        // Unified Context
        public List<string> ContextList { get; set; } = new();
        public object? RawContext { get; set; }

        // Unified Metrics
        public int TokenCount { get; set; }
        public bool IsPremium { get; set; }
        public double? Score { get; set; }
        public string Status { get; set; } = "";

        // Metadata / Tags
        public List<string> Tags { get; set; } = new();
        public string FailureNote { get; set; } = "";
        public string Interpretation { get; set; } = "";

        // Structured Actions (for Grid)
        public ObservableCollection<UnifiedAction> Actions { get; set; } = new ObservableCollection<UnifiedAction>();

        public bool HasActions => Actions?.Count > 0;

        // Processing dialog captured during execution (reasoning/tool trace).
        public ObservableCollection<UnifiedProcessingDialogItem> ProcessingDialog { get; set; } = new ObservableCollection<UnifiedProcessingDialogItem>();
        public bool HasProcessingDialog => ProcessingDialog?.Count > 0;

        /// <summary>Original source object for the "Original JSON" viewer. Not serialized to avoid object cycle when building the JSON tree.</summary>
        [JsonIgnore]
        public object? OriginalEntry { get; set; }
    }

    public class UnifiedAction
    {
        public int Order { get; set; }
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public string FilePath { get; set; } = "";

        /// <summary>Single line for list display: Order | Type | Description | Status | FilePath.</summary>
        public string PipeDelimitedLine => $"{Order} | {Type} | {Description} | {Status} | {FilePath}";
    }

    public class UnifiedProcessingDialogItem
    {
        public string Timestamp { get; set; } = "";
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public string Category { get; set; } = "";

        /// <summary>Timestamp formatted as short date/time in local time.</summary>
        public string TimestampDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Timestamp)) return "";
                if (DateTimeOffset.TryParse(Timestamp, out var dto))
                    return dto.LocalDateTime.ToString("g");
                if (DateTime.TryParse(Timestamp, out var dt))
                    return dt.ToString("g");
                return Timestamp;
            }
        }
    }

    /// <summary>Export path and content for the unified model JSON schema.</summary>
    public static class UnifiedSchemaExport
    {
        /// <summary>Filename of the schema when copied to output (UnifiedModel.schema.json).</summary>
        public const string SchemaFileName = "UnifiedModel.schema.json";

        /// <summary>Gets the path to the schema file in the application directory (when deployed).</summary>
        public static string GetSchemaPath()
        {
            return Path.Combine(AppContext.BaseDirectory, SchemaFileName);
        }

        /// <summary>Gets the JSON schema content for the unified model, or null if the file is not found.</summary>
        public static string? GetSchemaContent()
        {
            var path = GetSchemaPath();
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }
    }

    public static class UnifiedLogFactory
    {
        private static readonly ILogger _logger = AppLogService.Instance.CreateLogger("UnifiedJson");

        /// <summary>Ensures entries have OriginalEntry set and Agent set from log SourceType when empty (e.g. when loaded from unified-format file).</summary>
        public static void EnsureOriginalEntriesSet(UnifiedSessionLog? log)
        {
            if (log?.Entries == null) return;
            var agentFromLog = string.IsNullOrWhiteSpace(log.SourceType) ? "Unknown" : log.SourceType.Trim();
            foreach (var entry in log.Entries)
            {
                if (entry == null) continue;
                if (entry.OriginalEntry == null)
                    entry.OriginalEntry = entry;
                if (string.IsNullOrWhiteSpace(entry.Agent))
                    entry.Agent = agentFromLog;
            }
        }

        public static UnifiedSessionLog Create(CopilotSessionLog log)
        {
            var u = new UnifiedSessionLog
            {
                SourceType = "Copilot",
                SessionId = log.SessionId,
                Started = log.Started,
                LastUpdated = log.LastUpdated ?? log.Completed,
                Status = log.Status,
                EntryCount = log.Requests?.Count ?? 0,

                Title = log.Workspace?.Project ?? log.Workspace?.Repository ?? "Copilot Session",
                Model = log.Model, // Copilot has session-level model
                Workspace = log.Workspace,
                CopilotStatistics = log.Statistics,
                TotalTokens = (int)(log.Statistics?.TotalNetTokens ?? 0)
            };

            if (log.Requests != null)
            {
                foreach (var r in log.Requests)
                {
                    int tokens = ParseTokens(r.Cost?.Tokens);

                    var entry = new UnifiedRequestEntry
                    {
                        RequestId = r.RequestId,
                        Timestamp = r.Timestamp,
                        Model = r.Model,
                        ModelProvider = r.ModelProvider,
                        Agent = "Copilot",

                        QueryText = r.UserRequest,
                        QueryTitle = !string.IsNullOrEmpty(r.Title) ? r.Title : r.Slug,
                        Response = ParseCopilotResponse(r.Response, r.Interpretation, r.Actions),
                        Status = "Completed",

                        ContextList = ParseContextList(r.Context),
                        RawContext = r.Context,

                        Interpretation = ExtractInterpretation(r.Interpretation),
                        Actions = new ObservableCollection<UnifiedAction>(ParseActionsForCopilot(r.Actions, r.RequestId)),

                        TokenCount = tokens,
                        IsPremium = ParseBool(r.Cost?.PremiumRequests),

                        OriginalEntry = r
                    };

                    if (r.RequestNumber.HasValue) entry.Tags.Add($"Request #{r.RequestNumber}");
                    if (!string.IsNullOrEmpty(r.Slug) && r.Slug != r.Title) entry.Tags.Add($"Slug: {r.Slug}");

                    u.Entries.Add(entry);
                }
            }
            return u;
        }

        public static UnifiedSessionLog Create(CursorRequestLog log)
        {
            var u = new UnifiedSessionLog
            {
                SourceType = "Cursor",
                SessionId = log.Session,
                Status = "Unknown",
                Title = log.Description ?? log.SessionLabel ?? "Cursor Session",
                CursorSessionLabel = log.SessionLabel
            };

            if (log.Entries != null && log.Entries.Count > 0)
            {
                // Cursor doesn't have session-level model, try to get from first entry
                u.Model = log.Entries[0].Model;
            }

            if (DateTime.TryParse(log.Updated, out var dt))
            {
                u.LastUpdated = dt;
            }

            if (log.Entries != null)
            {
                u.EntryCount = log.Entries.Count;
                int totalTokens = 0;

                foreach (var r in log.Entries)
                {
                    DateTime? ts = ParseCursorTimestamp(r.Timestamp);
                    int tokens = ParseTokens(r.Cost?.Tokens);
                    totalTokens += tokens;

                    var entry = new UnifiedRequestEntry
                    {
                        RequestId = r.RequestId,
                        Timestamp = ts,
                        Model = r.Model,
                        Agent = "Cursor",

                        QueryText = r.ExactRequest,
                        QueryTitle = r.ExactRequestNote,
                        Response = ParseCursorResponse(r.Response),
                        Status = r.Successfulness?.Score != null ? "Scored" : "Unknown",

                        ContextList = r.ContextApplied ?? new List<string>(),

                        Interpretation = r.Interpretation != null ? string.Join("\n", r.Interpretation) : "",
                        Actions = new ObservableCollection<UnifiedAction>(GetCursorEntryActions(r.ActionsTaken, r.Actions)),

                        TokenCount = tokens,
                        IsPremium = ParseBool(r.Cost?.PremiumRequests),
                        Score = r.Successfulness?.Score,
                        FailureNote = r.PriorFailureNote,

                        OriginalEntry = r
                    };

                    if (r.ActionsTaken != null) entry.Tags.AddRange(r.ActionsTaken);
                    if (r.Successfulness?.Notes != null) entry.Tags.AddRange(r.Successfulness.Notes);

                    u.Entries.Add(entry);
                }
                u.TotalTokens = totalTokens;
            }
            return u;
        }

        /// <summary>
        /// Parses actions for a Copilot request with detailed logging.
        /// </summary>
        private static List<UnifiedAction> ParseActionsForCopilot(object? actionsObj, string? requestId)
        {
            if (actionsObj == null)
                return new List<UnifiedAction>();

            if (actionsObj is JsonElement element)
            {
                var list = new List<UnifiedAction>();
                ParseActionsFromElement(element, list);
                return list.OrderBy(x => x.Order).ToList();
            }

            if (actionsObj is string jsonString)
            {
                var list = new List<UnifiedAction>();
                try
                {
                    using var doc = JsonDocument.Parse(jsonString ?? "[]");
                    ParseActionsFromElement(doc.RootElement, list);
                }
                catch (Exception ex)
                {
                _logger.LogWarning("Copilot actions parse failed RequestId={RequestId}: {Message}", requestId, ex.Message);
                }
                return list.OrderBy(a => a.Order).ToList();
            }

            return new List<UnifiedAction>();
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Length <= maxLength) return value;
            return value.Substring(0, maxLength) + "...";
        }

        private static List<UnifiedAction> ParseActions(object? actionsObj)
        {
            var list = new List<UnifiedAction>();
            if (actionsObj == null) return list;

            if (actionsObj is System.Text.Json.JsonElement element)
            {
                ParseActionsFromElement(element, list);
            }
            else if (actionsObj is string jsonString)
            {
                 try {
                     using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
                     ParseActionsFromElement(doc.RootElement, list);
                 } catch (Exception ex) {
                     _logger.LogWarning("Failed to parse actions JSON: {Message}", ex.Message);
                 }
            }
            return list.OrderBy(a => a.Order).ToList();
        }

        /// <summary>
        /// Gets actions for a Cursor entry: primary source is actionsTaken (list of strings), fallback is structured actions.
        /// </summary>
        private static List<UnifiedAction> GetCursorEntryActions(List<string>? actionsTaken, object? actions)
        {
            var fromTaken = ActionsFromActionsTaken(actionsTaken);
            if (fromTaken != null && fromTaken.Count > 0) return fromTaken;
            return ParseActions(actions);
        }

        /// <summary>
        /// Converts Cursor's actionsTaken (list of strings) to unified actions. Returns null if list is null/empty.
        /// </summary>
        private static List<UnifiedAction>? ActionsFromActionsTaken(List<string>? actionsTaken)
        {
            if (actionsTaken == null || actionsTaken.Count == 0) return null;
            var list = new List<UnifiedAction>();
            for (int i = 0; i < actionsTaken.Count; i++)
                list.Add(new UnifiedAction { Order = i + 1, Description = actionsTaken[i] ?? "" });
            return list;
        }

        /// <summary>
        /// Parses actions from a raw JSON entry element (e.g. one item from entries[]).
        /// Cursor logs use "actionsTaken" or "actions_taken" (array of strings); also looks for "actions"/"Actions" (structured).
        /// </summary>
        public static List<UnifiedAction> ParseActionsFromEntryElement(System.Text.Json.JsonElement entryElement)
        {
            var list = new List<UnifiedAction>();
            if (entryElement.ValueKind != System.Text.Json.JsonValueKind.Object) return list;
            // Cursor: actionsTaken / actions_taken is the primary node (array of strings)
            if (TryGetActionsTakenArray(entryElement, out var actionsTakenProp))
            {
                int idx = 1;
                foreach (var item in actionsTakenProp.EnumerateArray())
                    list.Add(new UnifiedAction { Order = idx++, Description = GetElementString(item) });
                return list.OrderBy(a => a.Order).ToList();
            }
            // Fallback: structured "actions" or "Actions"
            if (TryGetPropertyCI(entryElement, "actions", out var actionsProp))
                ParseActionsFromElement(actionsProp, list);
            if (list.Count > 0) return list.OrderBy(a => a.Order).ToList();
            // Last resort: scan all properties for any array that looks like actions (array of strings or array of objects)
            foreach (var prop in entryElement.EnumerateObject())
            {
                if (prop.Value.ValueKind != System.Text.Json.JsonValueKind.Array) continue;
                var arr = prop.Value;
                if (arr.GetArrayLength() == 0) continue;
                using var enumerator = arr.EnumerateArray();
                if (!enumerator.MoveNext()) continue;
                var first = enumerator.Current;
                if (first.ValueKind == System.Text.Json.JsonValueKind.String || first.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    int idx = 1;
                    foreach (var item in arr.EnumerateArray())
                        list.Add(new UnifiedAction { Order = idx++, Description = GetElementString(item) });
                    return list.OrderBy(a => a.Order).ToList();
                }
                if (first.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    ParseActionsFromElement(arr, list);
                    if (list.Count > 0) return list.OrderBy(a => a.Order).ToList();
                }
            }
            return list.OrderBy(a => a.Order).ToList();
        }

        /// <summary>Tries to get the actions-taken array (cursor: "actionsTaken" or "actions_taken").</summary>
        private static bool TryGetActionsTakenArray(System.Text.Json.JsonElement entryElement, out System.Text.Json.JsonElement arrayElement)
        {
            arrayElement = default;
            foreach (var name in new[] { "actionsTaken", "actions_taken" })
            {
                if (TryGetPropertyCI(entryElement, name, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    arrayElement = prop;
                    return true;
                }
            }
            return false;
        }

        private static void ParseActionsFromElement(System.Text.Json.JsonElement arrElement, List<UnifiedAction> list)
        {
            if (arrElement.ValueKind != System.Text.Json.JsonValueKind.Array) return;
            int idx = 1;
            foreach (var item in arrElement.EnumerateArray())
            {
                var action = new UnifiedAction();
                // Array of strings/primitives: use value as description
                if (item.ValueKind == System.Text.Json.JsonValueKind.String || item.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    action.Order = idx++;
                    action.Description = GetElementString(item);
                    list.Add(action);
                    continue;
                }
                if (TryGetPropertyCI(item, "order", out var order) && order.ValueKind == System.Text.Json.JsonValueKind.Number)
                    action.Order = order.GetInt32();
                else
                    action.Order = idx++;
                if (TryGetPropertyCI(item, "description", out var desc)) action.Description = GetElementString(desc);
                if (TryGetPropertyCI(item, "type", out var type)) action.Type = GetElementString(type);
                if (TryGetPropertyCI(item, "status", out var status)) action.Status = GetElementString(status);
                if (TryGetPropertyCI(item, "filePath", out var fp)) action.FilePath = GetElementString(fp);
                else if (TryGetPropertyCI(item, "file_path", out var fp2)) action.FilePath = GetElementString(fp2);
                else if (TryGetPropertyCI(item, "filePaths", out var fps)) action.FilePath = "Multiple files";
                list.Add(action);
            }
        }

        private static string GetElementString(System.Text.Json.JsonElement element)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var s = element.GetString();
                return s ?? "";
            }
            return element.ToString().Trim('"');
        }

        private static bool TryGetPropertyCI(System.Text.Json.JsonElement element, string propertyName, out System.Text.Json.JsonElement value)
        {
            value = default;
            if (element.ValueKind != System.Text.Json.JsonValueKind.Object) return false;

            if (element.TryGetProperty(propertyName, out value)) return true;

            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
            return false;
        }

        private static int ParseTokens(object? tokenObj)
        {
            if (tokenObj == null) return 0;
            if (tokenObj is int i) return i;
            if (tokenObj is long l) return (int)l;
            if (tokenObj is double d) return (int)d;
            if (tokenObj is System.Text.Json.JsonElement e)
            {
                if (e.ValueKind == System.Text.Json.JsonValueKind.Number && e.TryGetInt32(out var val)) return val;
                if (e.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(e.GetString(), out var val2)) return val2;
            }
            if (tokenObj is string s && int.TryParse(s, out var val3)) return val3;
            return 0;
        }

        private static bool ParseBool(object? obj)
        {
            if (obj == null) return false;
            if (obj is bool b) return b;
            if (obj is string s) return s.Equals("true", StringComparison.OrdinalIgnoreCase);
             if (obj is System.Text.Json.JsonElement e)
             {
                 if (e.ValueKind == System.Text.Json.JsonValueKind.True) return true;
             }
            return false;
        }

        private static DateTime? ParseCursorTimestamp(string ts)
        {
            if (string.IsNullOrEmpty(ts)) return null;
            if (DateTime.TryParse(ts, out var t)) return t;
            if (long.TryParse(ts, out var l))
            {
                 if (l > 1000000000000) return DateTimeOffset.FromUnixTimeMilliseconds(l).UtcDateTime;
                 return DateTimeOffset.FromUnixTimeSeconds(l).UtcDateTime;
            }
            return null;
        }

        private static string ExtractInterpretation(object? interpretationObj)
        {
             if (interpretationObj == null) return "";

             if (interpretationObj is System.Text.Json.JsonElement interp && interp.ValueKind == System.Text.Json.JsonValueKind.Object)
             {
                 var sb = new System.Text.StringBuilder();

                 // Summary
                 if (interp.TryGetProperty("summary", out var summary))
                 {
                     sb.AppendLine(summary.GetString());
                     sb.AppendLine();
                 }

                 // Requirements
                 if (interp.TryGetProperty("requirements", out var reqs) && reqs.ValueKind == System.Text.Json.JsonValueKind.Array)
                 {
                     sb.AppendLine("Requirements:");
                     foreach (var req in reqs.EnumerateArray())
                     {
                         sb.AppendLine($"- {req}");
                     }
                     sb.AppendLine();
                 }

                 // Purpose
                 if (interp.TryGetProperty("purpose", out var purp) && purp.ValueKind == System.Text.Json.JsonValueKind.Array)
                 {
                     sb.AppendLine("Purpose:");
                     foreach (var p in purp.EnumerateArray())
                     {
                         sb.AppendLine($"- {p}");
                     }
                     sb.AppendLine();
                 }

                 // Key Decisions (from req-002)
                 if (interp.TryGetProperty("keyDecisions", out var decisions) && decisions.ValueKind == System.Text.Json.JsonValueKind.Array)
                 {
                     sb.AppendLine("Key Decisions:");
                     foreach (var d in decisions.EnumerateArray())
                     {
                         sb.AppendLine($"- {d}");
                     }
                     sb.AppendLine();
                 }

                 return sb.ToString().Trim();
             }
             return interpretationObj.ToString() ?? "";
        }

        private static string ParseCopilotResponse(object? responseObj, object? interpretationObj = null, object? actionsObj = null)
        {
            // If explicit response exists, use it
            if (responseObj != null)
            {
                string resp = "";
                if (responseObj is string s) resp = s;
                else if (responseObj is System.Text.Json.JsonElement e)
                {
                     if (e.ValueKind == System.Text.Json.JsonValueKind.Array)
                     {
                        var sb = new System.Text.StringBuilder();
                        foreach (var item in e.EnumerateArray()) sb.Append(item.ToString());
                        resp = sb.ToString();
                     }
                     else resp = e.ToString();
                }
                else resp = responseObj.ToString() ?? "";

                if (!string.IsNullOrWhiteSpace(resp)) return resp;
            }

            // Fallback: Synthesize response from Interpretation and Actions
            if (interpretationObj != null || actionsObj != null)
            {
                var sb = new System.Text.StringBuilder();

                if (interpretationObj is System.Text.Json.JsonElement interp && interp.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (interp.TryGetProperty("summary", out var summary))
                    {
                        sb.AppendLine("### Summary");
                        sb.AppendLine(summary.GetString());
                        sb.AppendLine();
                    }

                    if (interp.TryGetProperty("requirements", out var reqs) && reqs.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        sb.AppendLine("### Requirements");
                        foreach (var req in reqs.EnumerateArray())
                        {
                            sb.AppendLine($"- {req}");
                        }
                        sb.AppendLine();
                    }
                }

                if (actionsObj is System.Text.Json.JsonElement actions && actions.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    sb.AppendLine("### Actions Taken");
                    foreach (var action in actions.EnumerateArray())
                    {
                        string desc = "";
                        if (action.TryGetProperty("description", out var d)) desc = d.GetString() ?? "";
                        else desc = action.ToString();

                        if (action.TryGetProperty("order", out var order))
                            sb.AppendLine($"{order}. {desc}");
                        else
                            sb.AppendLine($"- {desc}");
                    }
                }

                return sb.ToString();
            }

            return "";
        }

        private static string ParseCursorResponse(object? responseObj)
        {
            if (responseObj == null) return "";
            if (responseObj is string s) return s;
            if (responseObj is System.Text.Json.JsonElement e)
            {
                if (e.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var item in e.EnumerateArray())
                    {
                        sb.Append(item.ToString());
                    }
                    return sb.ToString();
                }
                return e.ToString();
            }
            if (responseObj is System.Collections.IEnumerable list && !(responseObj is string))
            {
                var sb = new System.Text.StringBuilder();
                foreach (var item in list) sb.Append(item?.ToString());
                return sb.ToString();
            }
            return responseObj.ToString() ?? "";
        }

        private static List<string> ParseContextList(object? contextObj)
        {
            var list = new List<string>();
            if (contextObj == null) return list;

            if (contextObj is System.Text.Json.JsonElement e)
            {
                if (e.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in e.EnumerateArray())
                    {
                        list.Add(item.ToString());
                    }
                }
                else
                {
                    list.Add(e.ToString());
                }
            }
            else if (contextObj is System.Collections.IEnumerable enumerable && !(contextObj is string))
            {
                foreach (var item in enumerable)
                {
                    list.Add(item?.ToString() ?? "");
                }
            }
            else
            {
                list.Add(contextObj.ToString() ?? "");
            }
            return list;
        }
    }
}


