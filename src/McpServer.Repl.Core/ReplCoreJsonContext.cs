using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Provides source-generated JSON metadata for REPL-owned DTO serialization paths.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(List<object?>))]
[JsonSerializable(typeof(List<UnifiedRequestEntryDto>))]
[JsonSerializable(typeof(UnifiedSessionLogDto))]
[JsonSerializable(typeof(UnifiedRequestEntryDto))]
[JsonSerializable(typeof(List<ReplCommandDispatcher.DialogItemAdapter>))]
[JsonSerializable(typeof(List<ReplCommandDispatcher.SessionActionAdapter>))]
[JsonSerializable(typeof(IReadOnlyList<AcceptanceCriterion>))]
internal sealed partial class ReplCoreJsonContext : JsonSerializerContext;
