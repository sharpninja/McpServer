using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.UI.Core.Models.Json
{
    // Copilot Session Log
    public class CopilotSessionLog
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = "";

        [JsonPropertyName("started")]
        public DateTime? Started { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTime? LastUpdated { get; set; }

        [JsonPropertyName("completed")]
        public DateTime? Completed { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("totalRequests")]
        public int? TotalRequests { get; set; }

        [JsonPropertyName("completedRequests")]
        public int? CompletedRequests { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("modelProvider")]
        public string ModelProvider { get; set; } = "";

        [JsonPropertyName("workspace")]
        [JsonConverter(typeof(WorkspaceInfoConverter))]
        public WorkspaceInfo? Workspace { get; set; }

        [JsonPropertyName("statistics")]
        public StatisticsInfo? Statistics { get; set; }

        [JsonPropertyName("requests")]
        public List<CopilotRequestEntry> Requests { get; set; } = new();
    }

    public class CopilotRequestEntry
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = "";

        [JsonPropertyName("requestNumber")]
        public int? RequestNumber { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("modelProvider")]
        public string ModelProvider { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("userRequest")]
        public string UserRequest { get; set; } = "";

        [JsonPropertyName("context")]
        public object? Context { get; set; }

        [JsonPropertyName("response")]
        public object? Response { get; set; }

        [JsonPropertyName("interpretation")]
        public object? Interpretation { get; set; }

        [JsonPropertyName("actions")]
        public object? Actions { get; set; }

        [JsonPropertyName("cost")]
        public CostInfo? Cost { get; set; }
    }

    public class WorkspaceInfo
    {
        [JsonPropertyName("project")]
        public string Project { get; set; } = "";

        [JsonPropertyName("targetFramework")]
        public string TargetFramework { get; set; } = "";

        [JsonPropertyName("repository")]
        public string Repository { get; set; } = "";

        [JsonPropertyName("branch")]
        public string Branch { get; set; } = "";
    }

    /// <summary>Allows workspace to be either an object, a string (e.g. path), or other value in Copilot session JSON. Tolerates object properties with non-string values (e.g. numbers).</summary>
    public sealed class WorkspaceInfoConverter : JsonConverter<WorkspaceInfo?>
    {
        public override WorkspaceInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    reader.Read();
                    return null;
                case JsonTokenType.String:
                    var path = reader.GetString();
                    return string.IsNullOrEmpty(path) ? null : new WorkspaceInfo { Project = path };
                case JsonTokenType.StartObject:
                    using (var doc = JsonDocument.ParseValue(ref reader))
                    {
                        var root = doc.RootElement;
                        return new WorkspaceInfo
                        {
                            Project = GetStringFromElement(root, "project"),
                            TargetFramework = GetStringFromElement(root, "targetFramework"),
                            Repository = GetStringFromElement(root, "repository"),
                            Branch = GetStringFromElement(root, "branch")
                        };
                    }
                case JsonTokenType.Number:
                    try { _ = reader.GetDouble(); } catch { /* consume */ }
                    return null;
                case JsonTokenType.True:
                case JsonTokenType.False:
                    _ = reader.GetBoolean();
                    return null;
                case JsonTokenType.StartArray:
                    reader.Skip();
                    return null;
                default:
                    reader.Read();
                    return null;
            }
        }

        private static string GetStringFromElement(JsonElement element, string propertyName)
        {
            if (!TryGetPropertyIgnoreCase(element, propertyName, out var prop))
                return "";
            return prop.ValueKind switch
            {
                JsonValueKind.String => prop.GetString() ?? "",
                JsonValueKind.Number => prop.TryGetInt64(out var i) ? i.ToString() : prop.GetDouble().ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "",
                JsonValueKind.Object or JsonValueKind.Array => "", // ignore nested structures
                _ => prop.GetRawText().Trim('"')
            };
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (var p in element.EnumerateObject())
            {
                if (string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        public override void Write(Utf8JsonWriter writer, WorkspaceInfo? value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else
                JsonSerializer.Serialize(writer, value, options);
        }
    }

    public class StatisticsInfo
    {
        [JsonPropertyName("averageSuccessScore")]
        public double? AverageSuccessScore { get; set; }

        [JsonPropertyName("totalNetTokens")]
        public double? TotalNetTokens { get; set; }

        [JsonPropertyName("totalNetPremiumRequests")]
        public double? TotalNetPremiumRequests { get; set; }

        [JsonPropertyName("completedCount")]
        public int? CompletedCount { get; set; }

        [JsonPropertyName("inProgressCount")]
        public int? InProgressCount { get; set; }

        [JsonPropertyName("failedCount")]
        public int? FailedCount { get; set; }
    }

    // Cursor Request Log
    public class CursorRequestLog
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("session")]
        public string Session { get; set; } = "";

        [JsonPropertyName("sessionLabel")]
        public string SessionLabel { get; set; } = "";

        [JsonPropertyName("updated")]
        public string Updated { get; set; } = "";

        [JsonPropertyName("entries")]
        public List<CursorRequestEntry> Entries { get; set; } = new();
    }

    public class CursorRequestEntry
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = "";

        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "";

        [JsonPropertyName("exactRequest")]
        public string ExactRequest { get; set; } = "";

        [JsonPropertyName("exactRequestNote")]
        public string ExactRequestNote { get; set; } = "";

        [JsonPropertyName("contextApplied")]
        public List<string> ContextApplied { get; set; } = new();

        [JsonPropertyName("interpretation")]
        public List<string> Interpretation { get; set; } = new();

        [JsonPropertyName("response")]
        public object? Response { get; set; }

        [JsonPropertyName("actionsTaken")]
        public List<string> ActionsTaken { get; set; } = new();

        /// <summary>Structured actions array (optional; "actions" or "Actions" in JSON).</summary>
        [JsonPropertyName("actions")]
        public object? Actions { get; set; }

        [JsonPropertyName("successfulness")]
        public SuccessfulnessInfo? Successfulness { get; set; }

        [JsonPropertyName("priorFailureNote")]
        public string PriorFailureNote { get; set; } = "";

        [JsonPropertyName("cost")]
        public CostInfo? Cost { get; set; }
    }

    public class SuccessfulnessInfo
    {
        [JsonPropertyName("score")]
        public int? Score { get; set; }

        [JsonPropertyName("maxScore")]
        public int? MaxScore { get; set; }

        [JsonPropertyName("notes")]
        public List<string> Notes { get; set; } = new();
    }

    public class CostInfo
    {
        [JsonPropertyName("tokens")]
        public object? Tokens { get; set; }

        [JsonPropertyName("premiumRequests")]
        public object? PremiumRequests { get; set; }

        [JsonPropertyName("costOfLogEntryExcluded")]
        public string CostOfLogEntryExcluded { get; set; } = "";
    }
}

