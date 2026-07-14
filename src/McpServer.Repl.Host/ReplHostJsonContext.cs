using System.Text.Json;
using System.Text.Json.Serialization;
using McpServer.Client.Models;

namespace McpServer.Repl.Host;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(LoginHandler.DeviceAuthResponse))]
[JsonSerializable(typeof(LoginHandler.DeviceTokenResponse))]
[JsonSerializable(typeof(TransactionalTodoWorkflow.TodoCreateTransactionPayload))]
[JsonSerializable(typeof(TransactionalTodoWorkflow.TodoDeleteTransactionPayload))]
[JsonSerializable(typeof(TransactionalTodoWorkflow.TodoUpdateTransactionPayload))]
[JsonSerializable(typeof(TransactionalTodoWorkflow.TodoDeleteResultPayload))]
[JsonSerializable(typeof(TransactionalTodoWorkflow.TodoMutationStatusPayload))]
[JsonSerializable(typeof(TransactionalTodoWorkflow.TodoMutationSuccessPayload))]
internal sealed partial class ReplHostJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, PropertyNameCaseInsensitive = true, WriteIndented = true)]
[JsonSerializable(typeof(LoginHandler.CachedToken))]
internal sealed partial class ReplHostCacheJsonContext : JsonSerializerContext;