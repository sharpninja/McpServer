// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Command/response model shapes
// FR-MCP-REPL-003: Command Namespace Parity - Command argument and response structures
// TR-MCP-REPL-001: YAML Envelope Protocol - Command envelope data models
// TR-MCP-REPL-004: Command Registry and Dispatcher - Command shape definitions
// TEST-MCP-REPL-001: YAML command envelopes serialize/deserialize correctly

using System.Collections.Generic;

// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Client passthrough command shapes
// FR-MCP-REPL-003: Command Namespace Parity - Generic client operation contract models
// FR-MCP-REPL-005: Orchestration State Visibility - State query command shapes
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Client command namespace shapes
// TR-MCP-REPL-007: State Query Commands - Client passthrough models
// TEST-MCP-REPL-008: Context REPL operations match REST endpoints
// TEST-MCP-REPL-011: Generic client passthrough delegates to correct client methods

namespace McpServer.Repl.Core;

/// <summary>
/// Defines YAML command shapes for the <c>client.*</c> namespace, enabling dynamic invocation
/// of any <c>McpServerClient</c> sub-client method without compile-time knowledge of the specific client or method.
/// All commands follow the REPL protocol request envelope structure with runtime method binding.
/// </summary>
/// <remarks>
/// <para>
/// Command methods in this namespace follow a dynamic pattern:
/// <c>client.&lt;clientName&gt;.&lt;methodName&gt;</c>
/// </para>
/// <para><strong>Supported Clients:</strong></para>
/// <list type="bullet">
/// <item><c>client.context.*</c> — Context search and pack operations</item>
/// <item><c>client.github.*</c> — GitHub integration (issues, PRs, comments)</item>
/// <item><c>client.todo.*</c> — TODO management</item>
/// <item><c>client.sessionlog.*</c> — Session log operations</item>
/// <item><c>client.requirements.*</c> — Requirements management</item>
/// <item><c>client.voice.*</c> — Voice conversation endpoints</item>
/// <item><c>client.events.*</c> — Change-event SSE endpoints</item>
/// <item><c>client.repo.*</c> — Repository file operations</item>
/// <item><c>client.desktop.*</c> — Desktop process launch</item>
/// <item><c>client.tunnel.*</c> — Tunnel management</item>
/// <item><c>client.workspace.*</c> — Workspace lifecycle management</item>
/// <item><c>client.configuration.*</c> — Admin configuration</item>
/// <item><c>client.tools.*</c> — Tool registry operations</item>
/// <item><c>client.authconfig.*</c> — Public auth configuration</item>
/// <item><c>client.diagnostic.*</c> — Diagnostic endpoints</item>
/// <item><c>client.template.*</c> — Prompt template management</item>
/// <item><c>client.agentpool.*</c> — Agent-pool runtime</item>
/// <item><c>client.agent.*</c> — Agent management</item>
/// <item><c>client.health.*</c> — Server health checks</item>
/// <item><c>client.federation.*</c> — Federation management (status, targets, routes, push/pull)</item>
/// </list>
/// <para><strong>Request Envelope Structure:</strong></para>
/// <para>
/// All <c>client.*</c> commands follow this structure:
/// <code>
/// type: request
/// payload:
///   requestId: &lt;unique-request-id&gt;
///   method: client.&lt;clientName&gt;.&lt;methodName&gt;
///   params:
///     &lt;argument-name&gt;: &lt;argument-value&gt;
///     ...
/// </code>
/// </para>
/// <para>
/// The <c>params</c> dictionary contains argument names as keys and their values. Argument names
/// must match the parameter names of the target method (case-insensitive). The <c>CancellationToken</c>
/// parameter must not be included in <c>params</c>; it is automatically supplied by the invocation layer.
/// </para>
/// <para><strong>Response Envelope Structure:</strong></para>
/// <para>
/// Successful invocations return the method's result:
/// <code>
/// type: result
/// payload:
///   requestId: &lt;matching-request-id&gt;
///   result:
///     &lt;method-result-object&gt;
/// </code>
/// </para>
/// <para>
/// The structure of <c>result</c> depends on the invoked method's return type. For example,
/// <c>client.context.SearchAsync</c> returns a <c>ContextSearchResult</c>, while <c>client.todo.QueryAsync</c>
/// returns a collection of TODO items.
/// </para>
/// <para><strong>Error Envelope Structure:</strong></para>
/// <para>
/// Errors follow the standard REPL protocol error envelope:
/// <code>
/// type: error
/// payload:
///   requestId: &lt;matching-request-id&gt;
///   code: &lt;error-code&gt;
///   message: &lt;human-readable-message&gt;
///   details:
///     &lt;optional-context-specific-details&gt;
/// </code>
/// </para>
/// <para><strong>Standard Error Codes:</strong></para>
/// <list type="bullet">
/// <item><c>unknown_client</c> — The <c>clientName</c> does not match any sub-client property on <c>McpServerClient</c>.</item>
/// <item><c>unknown_method</c> — The <c>methodName</c> does not match any public <c>Task&lt;T&gt;</c>-returning method on the resolved client.</item>
/// <item><c>missing_required_parameter</c> — A required method parameter is not present in the <c>params</c> dictionary.</item>
/// <item><c>type_conversion_error</c> — Argument coercion failed (e.g., passing a string for an integer parameter).</item>
/// <item><c>invalid_enum_value</c> — An enum parameter received an invalid string value.</item>
/// <item><c>json_deserialization_error</c> — Complex object deserialization failed.</item>
/// <item><c>collection_conversion_error</c> — Collection deserialization failed.</item>
/// <item><c>null_for_nonnullable_parameter</c> — A non-nullable parameter received a <c>null</c> value.</item>
/// <item><c>method_invocation_error</c> — The underlying method invocation failed (network error, validation failure, etc.).</item>
/// </list>
/// </remarks>
/// <example>
/// <para><strong>Example: Context Search</strong></para>
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T120000Z-search-001
///   method: client.context.SearchAsync
///   params:
///     query: authentication flow
///     limit: 10
/// </code>
/// <para>Response:</para>
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T120000Z-search-001
///   result:
///     results:
///       - key: docs/auth.md
///         content: "Authentication flow overview..."
///         score: 0.95
///       - key: src/AuthService.cs
///         content: "public class AuthService..."
///         score: 0.87
///     totalResults: 2
/// </code>
/// <para><strong>Example: GitHub Issue Listing</strong></para>
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T120000Z-issues-001
///   method: client.github.ListIssuesAsync
///   params:
///     state: open
///     labels:
///       - bug
///       - priority-high
///     assignee: johndoe
/// </code>
/// <para>Response:</para>
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T120000Z-issues-001
///   result:
///     issues:
///       - number: 42
///         title: Authentication timeout on slow networks
///         state: open
///         labels: [bug, priority-high]
///         assignee: johndoe
///       - number: 38
///         title: Token refresh race condition
///         state: open
///         labels: [bug, priority-high]
///         assignee: johndoe
///     totalCount: 2
/// </code>
/// <para><strong>Example: TODO Query</strong></para>
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T120000Z-todo-001
///   method: client.todo.QueryAsync
///   params:
///     status: pending
///     limit: 20
/// </code>
/// <para>Response:</para>
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T120000Z-todo-001
///   result:
///     items:
///       - id: TODO-001
///         title: Implement JWT validation
///         status: pending
///         priority: high
///       - id: TODO-002
///         title: Add rate limiting
///         status: pending
///         priority: medium
///     totalCount: 2
/// </code>
/// <para><strong>Example: Error - Unknown Client</strong></para>
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T120000Z-invalid-001
///   method: client.invalidclient.SomeMethodAsync
///   params:
///     arg1: value1
/// </code>
/// <para>Error Response:</para>
/// <code>
/// type: error
/// payload:
///   requestId: req-20260304T120000Z-invalid-001
///   code: unknown_client
///   message: Client 'invalidclient' not found. Valid clients: context, github, todo, sessionlog, requirements, voice, events, repo, desktop, tunnel, workspace, configuration, tools, authconfig, diagnostic, template, agentpool, agent, health, federation.
///   details:
///     requestedClient: invalidclient
///     validClients:
///       - context
///       - github
///       - todo
///       - sessionlog
///       - requirements
///       - voice
///       - events
///       - repo
///       - desktop
///       - tunnel
///       - workspace
///       - configuration
///       - tools
///       - authconfig
///       - diagnostic
///       - template
///       - agentpool
///       - agent
///       - health
///       - federation
/// </code>
/// <para><strong>Example: Error - Unknown Method</strong></para>
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T120000Z-invalid-002
///   method: client.context.InvalidMethodAsync
///   params:
///     query: test
/// </code>
/// <para>Error Response:</para>
/// <code>
/// type: error
/// payload:
///   requestId: req-20260304T120000Z-invalid-002
///   code: unknown_method
///   message: Method 'InvalidMethodAsync' not found on client 'context'. Valid methods: SearchAsync, RebuildIndexAsync, PackAsync, ListSourcesAsync, IngestWebsiteAsync, GraphRagStatusAsync, GraphRagIndexAsync, GraphRagQueryAsync.
///   details:
///     requestedMethod: InvalidMethodAsync
///     clientName: context
///     validMethods:
///       - SearchAsync
///       - RebuildIndexAsync
///       - PackAsync
///       - ListSourcesAsync
///       - IngestWebsiteAsync
///       - GraphRagStatusAsync
///       - GraphRagIndexAsync
///       - GraphRagQueryAsync
/// </code>
/// <para><strong>Example: Error - Type Conversion</strong></para>
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T120000Z-invalid-003
///   method: client.context.SearchAsync
///   params:
///     query: authentication
///     limit: not-a-number
/// </code>
/// <para>Error Response:</para>
/// <code>
/// type: error
/// payload:
///   requestId: req-20260304T120000Z-invalid-003
///   code: type_conversion_error
///   message: Failed to convert argument 'limit' from value 'not-a-number' to type 'System.Int32'.
///   details:
///     argumentName: limit
///     providedValue: not-a-number
///     targetType: System.Int32
///     innerException: Input string was not in a correct format.
/// </code>
/// </example>
public static class ClientCommandShapes
{
    /// <summary>
    /// The namespace prefix for all generic client passthrough commands.
    /// </summary>
    public const string MethodNamespace = "client";

    /// <summary>
    /// Represents the parameters for any <c>client.*.*</c> command.
    /// The parameters are dynamic and depend on the target method's signature.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>params</c> dictionary in a YAML command maps to this interface. Each key-value pair corresponds
    /// to a method parameter name and its value. Parameter names are case-insensitive; values are coerced to
    /// the target parameter type using the rules described in <see cref="IGenericClientPassthrough"/>.
    /// </para>
    /// <para><strong>Example: Context Search Parameters</strong></para>
    /// <code>
    /// type: request
    /// payload:
    ///   requestId: req-20260304T120000Z-search-001
    ///   method: client.context.SearchAsync
    ///   params:
    ///     query: authentication flow
    ///     sourceType: markdown
    ///     limit: 10
    /// </code>
    /// <para>
    /// In this example, the <c>params</c> dictionary contains:
    /// <list type="bullet">
    /// <item><c>"query"</c> → <c>"authentication flow"</c> (string)</item>
    /// <item><c>"sourceType"</c> → <c>"markdown"</c> (string, optional)</item>
    /// <item><c>"limit"</c> → <c>10</c> (integer)</item>
    /// </list>
    /// </para>
    /// <para><strong>Example: GitHub Issues with Complex Parameters</strong></para>
    /// <code>
    /// type: request
    /// payload:
    ///   requestId: req-20260304T120000Z-issues-001
    ///   method: client.github.ListIssuesAsync
    ///   params:
    ///     state: open
    ///     labels:
    ///       - bug
    ///       - priority-high
    ///     assignee: johndoe
    ///     sort: created
    ///     direction: desc
    /// </code>
    /// <para>
    /// In this example, the <c>params</c> dictionary contains:
    /// <list type="bullet">
    /// <item><c>"state"</c> → <c>"open"</c> (string)</item>
    /// <item><c>"labels"</c> → <c>["bug", "priority-high"]</c> (array of strings)</item>
    /// <item><c>"assignee"</c> → <c>"johndoe"</c> (string)</item>
    /// <item><c>"sort"</c> → <c>"created"</c> (string)</item>
    /// <item><c>"direction"</c> → <c>"desc"</c> (string)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IClientInvokeParams
    {
        /// <summary>
        /// Gets the name of the sub-client to invoke (case-insensitive).
        /// Examples: <c>"context"</c>, <c>"github"</c>, <c>"todo"</c>.
        /// Valid client names match the property names on <c>McpServerClient</c>: <c>Context</c>, <c>GitHub</c>, <c>Todo</c>,
        /// <c>SessionLog</c>, <c>Requirements</c>, <c>Voice</c>, <c>Events</c>, <c>Repo</c>, <c>Desktop</c>, <c>Tunnel</c>,
        /// <c>Workspace</c>, <c>Configuration</c>, <c>Tools</c>, <c>AuthConfig</c>, <c>Diagnostic</c>, <c>Template</c>,
        /// <c>AgentPool</c>, <c>Agent</c>, <c>Health</c>, <c>Federation</c>.
        /// </summary>
        string ClientName { get; }

        /// <summary>
        /// Gets the name of the method to invoke on the resolved client (case-sensitive, with case-insensitive fallback).
        /// The method must be public, return <c>Task&lt;T&gt;</c>, and accept a <c>CancellationToken</c> as the last parameter.
        /// Examples: <c>"SearchAsync"</c>, <c>"QueryAsync"</c>, <c>"CreateAsync"</c>, <c>"ListIssuesAsync"</c>.
        /// </summary>
        string MethodName { get; }

        /// <summary>
        /// Gets a dictionary of argument names (case-insensitive) to argument values.
        /// Each key corresponds to a method parameter name. Values are coerced to the target parameter type
        /// using the rules described in <see cref="IGenericClientPassthrough"/>.
        /// Optional parameters may be omitted; required parameters must be provided or an exception is thrown.
        /// The <c>CancellationToken</c> parameter must not be included in this dictionary.
        /// </summary>
        Dictionary<string, object?> Arguments { get; }
    }

    /// <summary>
    /// Represents the result for any <c>client.*.*</c> command.
    /// The result structure depends on the target method's return type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>result</c> field in a YAML response maps to this interface. The structure of the result
    /// depends on the invoked method's return type. For example:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Context Search</term>
    /// <description><c>ContextSearchResult</c> with <c>results</c> array and <c>totalResults</c> count.</description>
    /// </item>
    /// <item>
    /// <term>GitHub Issues</term>
    /// <description><c>GitHubIssuesResult</c> with <c>issues</c> array and <c>totalCount</c>.</description>
    /// </item>
    /// <item>
    /// <term>TODO Query</term>
    /// <description>Collection of TODO items with metadata.</description>
    /// </item>
    /// </list>
    /// <para><strong>Example: Context Search Result</strong></para>
    /// <code>
    /// type: result
    /// payload:
    ///   requestId: req-20260304T120000Z-search-001
    ///   result:
    ///     results:
    ///       - key: docs/auth.md
    ///         content: "Authentication flow overview..."
    ///         score: 0.95
    ///       - key: src/AuthService.cs
    ///         content: "public class AuthService..."
    ///         score: 0.87
    ///     totalResults: 2
    /// </code>
    /// <para><strong>Example: TODO Query Result</strong></para>
    /// <code>
    /// type: result
    /// payload:
    ///   requestId: req-20260304T120000Z-todo-001
    ///   result:
    ///     items:
    ///       - id: TODO-001
    ///         title: Implement JWT validation
    ///         status: pending
    ///         priority: high
    ///       - id: TODO-002
    ///         title: Add rate limiting
    ///         status: pending
    ///         priority: medium
    ///     totalCount: 2
    /// </code>
    /// </remarks>
    public interface IClientInvokeResult
    {
        /// <summary>
        /// Gets the result of the invoked method.
        /// The structure depends on the method's return type. May be <c>null</c> if the method returns <c>Task</c>
        /// (completion without a result) or if the method explicitly returns <c>null</c>.
        /// </summary>
        object? Result { get; }
    }

    /// <summary>
    /// Defines structured error envelopes for generic client passthrough operations.
    /// All errors follow the REPL protocol error envelope structure with standardized codes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Error envelope structure:
    /// <code>
    /// type: error
    /// payload:
    ///   requestId: &lt;matching-request-id&gt;
    ///   code: &lt;error-code&gt;
    ///   message: &lt;human-readable-message&gt;
    ///   details:
    ///     &lt;optional-context-specific-details&gt;
    /// </code>
    /// </para>
    /// <para>
    /// Standard error codes for client passthrough operations:
    /// <list type="bullet">
    /// <item><c>unknown_client</c> — The <c>clientName</c> does not match any sub-client property on <c>McpServerClient</c>.</item>
    /// <item><c>unknown_method</c> — The <c>methodName</c> does not match any public <c>Task&lt;T&gt;</c>-returning method on the resolved client.</item>
    /// <item><c>missing_required_parameter</c> — A required method parameter is not present in the <c>params</c> dictionary.</item>
    /// <item><c>type_conversion_error</c> — Argument coercion failed (e.g., passing a string for an integer parameter).</item>
    /// <item><c>invalid_enum_value</c> — An enum parameter received an invalid string value.</item>
    /// <item><c>json_deserialization_error</c> — Complex object deserialization failed.</item>
    /// <item><c>collection_conversion_error</c> — Collection deserialization failed.</item>
    /// <item><c>null_for_nonnullable_parameter</c> — A non-nullable parameter received a <c>null</c> value.</item>
    /// <item><c>method_invocation_error</c> — The underlying method invocation failed (network error, validation failure, etc.).</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IClientPassthroughError
    {
        /// <summary>
        /// Gets the request ID that this error corresponds to.
        /// Must match the request ID from the failed command.
        /// </summary>
        string RequestId { get; }

        /// <summary>
        /// Gets the error code indicating the failure category.
        /// See remarks for standard error codes.
        /// </summary>
        string Code { get; }

        /// <summary>
        /// Gets the human-readable error message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets optional additional error details or context.
        /// Structure depends on the error code and operation.
        /// For example, <c>unknown_client</c> errors include <c>requestedClient</c> and <c>validClients</c> fields.
        /// </summary>
        IReadOnlyDictionary<string, object?>? Details { get; }
    }

    /// <summary>
    /// Provides standard error code constants for generic client passthrough operations.
    /// </summary>
    public static class ClientPassthroughErrorCodes
    {
        /// <summary>
        /// The <c>clientName</c> does not match any sub-client property on <c>McpServerClient</c>.
        /// Error details include <c>requestedClient</c> (string) and <c>validClients</c> (array of strings).
        /// </summary>
        public const string UnknownClient = "unknown_client";

        /// <summary>
        /// The <c>methodName</c> does not match any public <c>Task&lt;T&gt;</c>-returning method on the resolved client.
        /// Error details include <c>requestedMethod</c> (string), <c>clientName</c> (string), and <c>validMethods</c> (array of strings).
        /// </summary>
        public const string UnknownMethod = "unknown_method";

        /// <summary>
        /// A required method parameter is not present in the <c>params</c> dictionary.
        /// Error details include <c>parameterName</c> (string) and <c>parameterType</c> (string).
        /// </summary>
        public const string MissingRequiredParameter = "missing_required_parameter";

        /// <summary>
        /// Argument coercion failed (e.g., passing a string for an integer parameter).
        /// Error details include <c>argumentName</c> (string), <c>providedValue</c> (object), <c>targetType</c> (string),
        /// and <c>innerException</c> (string).
        /// </summary>
        public const string TypeConversionError = "type_conversion_error";

        /// <summary>
        /// An enum parameter received an invalid string value.
        /// Error details include <c>argumentName</c> (string), <c>providedValue</c> (string), <c>enumType</c> (string),
        /// and <c>validValues</c> (array of strings).
        /// </summary>
        public const string InvalidEnumValue = "invalid_enum_value";

        /// <summary>
        /// Complex object deserialization failed.
        /// Error details include <c>argumentName</c> (string), <c>targetType</c> (string), and <c>innerException</c> (string).
        /// </summary>
        public const string JsonDeserializationError = "json_deserialization_error";

        /// <summary>
        /// Collection deserialization failed.
        /// Error details include <c>argumentName</c> (string), <c>targetCollectionType</c> (string), and <c>innerException</c> (string).
        /// </summary>
        public const string CollectionConversionError = "collection_conversion_error";

        /// <summary>
        /// A non-nullable parameter received a <c>null</c> value.
        /// Error details include <c>parameterName</c> (string) and <c>parameterType</c> (string).
        /// </summary>
        public const string NullForNonNullableParameter = "null_for_nonnullable_parameter";

        /// <summary>
        /// The underlying method invocation failed (network error, validation failure, etc.).
        /// Error details include <c>clientName</c> (string), <c>methodName</c> (string), and <c>innerException</c> (string).
        /// </summary>
        public const string MethodInvocationError = "method_invocation_error";
    }
}
