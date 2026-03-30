using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Repl.Core;

/// <summary>
/// Defines a generic passthrough interface for dynamically invoking methods on any <c>McpServerClient</c> sub-client
/// without compile-time knowledge of the specific client or method signature.
/// This enables YAML-driven command dispatch to arbitrary client operations at runtime.
/// </summary>
/// <remarks>
/// <para><strong>Overview:</strong></para>
/// <para>
/// This interface provides dynamic binding for the <c>client.*</c> YAML command namespace, allowing callers to
/// invoke any public method on any sub-client exposed by <c>McpServerClient</c> (e.g., <c>client.context.SearchAsync</c>,
/// <c>client.github.ListIssuesAsync</c>) by specifying the client name, method name, and a dictionary of arguments.
/// </para>
/// <para><strong>YAML Command Shape:</strong></para>
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
/// <para><strong>Example Commands:</strong></para>
/// <list type="bullet">
/// <item>
/// <term>Context search</term>
/// <description>
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T120000Z-search-001
///   method: client.context.SearchAsync
///   params:
///     query: authentication flow
///     limit: 10
/// </code>
/// </description>
/// </item>
/// <item>
/// <term>GitHub issues</term>
/// <description>
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T120000Z-issues-001
///   method: client.github.ListIssuesAsync
///   params:
///     state: open
///     labels: bug,priority-high
/// </code>
/// </description>
/// </item>
/// <item>
/// <term>TODO query</term>
/// <description>
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T120000Z-todo-001
///   method: client.todo.QueryAsync
///   params:
///     status: pending
///     limit: 20
/// </code>
/// </description>
/// </item>
/// </list>
/// <para><strong>Method Resolution Strategy:</strong></para>
/// <list type="number">
/// <item>
/// <term>Client Name Lookup</term>
/// <description>
/// The <c>clientName</c> parameter (case-insensitive) is matched against the property names
/// of <c>McpServerClient</c>. Examples: <c>"context"</c> → <c>Context</c>, <c>"github"</c> → <c>GitHub</c>,
/// <c>"sessionlog"</c> → <c>SessionLog</c>.
/// If no matching client is found, an <see cref="System.InvalidOperationException"/> is thrown with error code
/// <c>"unknown_client"</c>.
/// </description>
/// </item>
/// <item>
/// <term>Method Name Lookup</term>
/// <description>
/// The <c>methodName</c> parameter (case-sensitive by default, case-insensitive fallback) is matched
/// against the public methods of the resolved client. The method must have a return type of <c>Task&lt;T&gt;</c>
/// (async methods returning results) and accept a <see cref="CancellationToken"/> as the last parameter.
/// If no matching method is found, an <see cref="System.InvalidOperationException"/> is thrown with error code
/// <c>"unknown_method"</c>.
/// </description>
/// </item>
/// <item>
/// <term>Parameter Binding</term>
/// <description>
/// The <c>arguments</c> dictionary keys are matched against the method's parameter names (case-insensitive).
/// Each argument value is coerced to the target parameter type using the rules described below. Missing optional
/// parameters use their default values. Missing required parameters throw an <see cref="System.ArgumentException"/>
/// with error code <c>"missing_required_parameter"</c>.
/// </description>
/// </item>
/// </list>
/// <para><strong>Argument Coercion Rules:</strong></para>
/// <list type="bullet">
/// <item>
/// <term>Primitive Types</term>
/// <description>
/// <c>string</c>, <c>int</c>, <c>long</c>, <c>bool</c>, <c>double</c>, <c>decimal</c> are converted using
/// <c>System.Convert.ChangeType</c>. If conversion fails, an <see cref="System.ArgumentException"/> is thrown
/// with error code <c>"type_conversion_error"</c>.
/// </description>
/// </item>
/// <item>
/// <term>Enum Types</term>
/// <description>
/// String values are parsed using <c>Enum.Parse</c> (case-insensitive). Numeric values are cast directly.
/// If parsing fails, an <see cref="System.ArgumentException"/> is thrown with error code <c>"invalid_enum_value"</c>.
/// </description>
/// </item>
/// <item>
/// <term>Complex Objects</term>
/// <description>
/// Dictionary values are serialized to JSON and deserialized into the target type using <c>System.Text.Json</c>.
/// This supports request model types (e.g., <c>ContextSearchRequest</c>, <c>CreateTodoRequest</c>).
/// If deserialization fails, an <see cref="System.ArgumentException"/> is thrown with error code
/// <c>"json_deserialization_error"</c>.
/// </description>
/// </item>
/// <item>
/// <term>Collections</term>
/// <description>
/// Array or list values in the dictionary are deserialized to <c>IReadOnlyList&lt;T&gt;</c>, <c>List&lt;T&gt;</c>,
/// or <c>T[]</c> using <c>System.Text.Json</c>. Element types follow the same coercion rules.
/// If deserialization fails, an <see cref="System.ArgumentException"/> is thrown with error code
/// <c>"collection_conversion_error"</c>.
/// </description>
/// </item>
/// <item>
/// <term>Nullable Types</term>
/// <description>
/// If the dictionary value is <c>null</c> and the target parameter is nullable, <c>null</c> is passed.
/// If the target parameter is non-nullable and the value is <c>null</c>, an <see cref="System.ArgumentException"/>
/// is thrown with error code <c>"null_for_nonnullable_parameter"</c>.
/// </description>
/// </item>
/// <item>
/// <term>CancellationToken</term>
/// <description>
/// The <c>cancellationToken</c> parameter is always passed as the last argument to the method,
/// even if not present in the <c>arguments</c> dictionary.
/// </description>
/// </item>
/// </list>
/// <para><strong>Error Handling:</strong></para>
/// <list type="bullet">
/// <item>
/// <term>unknown_client</term>
/// <description>
/// Thrown when <c>clientName</c> does not match any sub-client property on <c>McpServerClient</c>.
/// The exception message includes the invalid client name and a list of valid client names.
/// </description>
/// </item>
/// <item>
/// <term>unknown_method</term>
/// <description>
/// Thrown when <c>methodName</c> does not match any public <c>Task&lt;T&gt;</c>-returning method
/// on the resolved client. The exception message includes the invalid method name and a list of valid method names.
/// </description>
/// </item>
/// <item>
/// <term>missing_required_parameter</term>
/// <description>
/// Thrown when a required method parameter is not present in the <c>arguments</c> dictionary.
/// The exception message includes the missing parameter name and type.
/// </description>
/// </item>
/// <item>
/// <term>type_conversion_error</term>
/// <description>
/// Thrown when argument coercion fails (e.g., passing <c>"abc"</c> for an <c>int</c> parameter).
/// The exception message includes the argument name, provided value, target type, and inner exception details.
/// </description>
/// </item>
/// <item>
/// <term>invalid_enum_value</term>
/// <description>
/// Thrown when an enum parameter receives an invalid string value.
/// The exception message includes the invalid value and the list of valid enum members.
/// </description>
/// </item>
/// <item>
/// <term>json_deserialization_error</term>
/// <description>
/// Thrown when complex object deserialization fails.
/// The exception message includes the argument name, target type, and JSON serialization exception details.
/// </description>
/// </item>
/// <item>
/// <term>collection_conversion_error</term>
/// <description>
/// Thrown when collection deserialization fails.
/// The exception message includes the argument name, target collection type, and inner exception details.
/// </description>
/// </item>
/// <item>
/// <term>null_for_nonnullable_parameter</term>
/// <description>
/// Thrown when a non-nullable parameter receives a <c>null</c> value.
/// The exception message includes the parameter name and type.
/// </description>
/// </item>
/// <item>
/// <term>method_invocation_error</term>
/// <description>
/// Thrown when the underlying method invocation fails (e.g., network error, validation failure).
/// The exception message includes the client name, method name, and the inner exception from the invoked method.
/// </description>
/// </item>
/// </list>
/// <para><strong>Return Value:</strong></para>
/// <para>
/// The return value is the deserialized result of the invoked method. The caller is responsible for interpreting
/// the result based on the method's return type. If the method returns <c>Task&lt;void&gt;</c> (completion without
/// a result), the implementation returns an empty object or <c>null</c>.
/// </para>
/// <para><strong>Thread Safety:</strong></para>
/// <para>
/// Implementations must be thread-safe. Multiple callers may invoke <see cref="InvokeAsync"/> concurrently
/// with different client/method combinations.
/// </para>
/// </remarks>
/// <example>
/// <para><strong>Example: Context Search</strong></para>
/// <code>
/// var passthrough = serviceProvider.GetRequiredService&lt;IGenericClientPassthrough&gt;();
/// var args = new Dictionary&lt;string, object&gt;
/// {
///     ["query"] = "authentication flow",
///     ["limit"] = 10
/// };
/// var result = await passthrough.InvokeAsync("context", "SearchAsync", args);
/// // result is a ContextSearchResult object
/// </code>
/// <para><strong>Example: GitHub Issues with Complex Filter</strong></para>
/// <code>
/// var args = new Dictionary&lt;string, object&gt;
/// {
///     ["state"] = "open",
///     ["labels"] = new[] { "bug", "priority-high" },
///     ["assignee"] = "johndoe"
/// };
/// var result = await passthrough.InvokeAsync("github", "ListIssuesAsync", args);
/// </code>
/// </example>
public interface IGenericClientPassthrough
{
    /// <summary>
    /// Dynamically invokes a method on the specified <c>McpServerClient</c> sub-client with the given arguments.
    /// </summary>
    /// <param name="clientName">
    /// The name of the sub-client to invoke (case-insensitive). Examples: <c>"context"</c>, <c>"github"</c>, <c>"todo"</c>.
    /// Valid client names match the property names on <c>McpServerClient</c>: <c>Context</c>, <c>GitHub</c>, <c>Todo</c>,
    /// <c>SessionLog</c>, <c>Requirements</c>, <c>Voice</c>, <c>Events</c>, <c>Repo</c>, <c>Desktop</c>, <c>Tunnel</c>,
    /// <c>Workspace</c>, <c>Configuration</c>, <c>Tools</c>, <c>AuthConfig</c>, <c>Diagnostic</c>, <c>Template</c>,
    /// <c>AgentPool</c>, <c>Agent</c>, <c>Health</c>.
    /// </param>
    /// <param name="methodName">
    /// The name of the method to invoke on the resolved client (case-sensitive, with case-insensitive fallback).
    /// The method must be public, return <c>Task&lt;T&gt;</c>, and accept a <see cref="CancellationToken"/> as the last parameter.
    /// Examples: <c>"SearchAsync"</c>, <c>"QueryAsync"</c>, <c>"CreateAsync"</c>, <c>"ListIssuesAsync"</c>.
    /// </param>
    /// <param name="arguments">
    /// A dictionary of argument names (case-insensitive) to argument values. Each key corresponds to a method parameter name.
    /// Values are coerced to the target parameter type using the rules described in the interface remarks.
    /// Optional parameters may be omitted; required parameters must be provided or an exception is thrown.
    /// The <see cref="CancellationToken"/> parameter must not be included in this dictionary.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation. This is automatically passed to the invoked method.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous invocation. The result is the return value of the invoked method,
    /// deserialized to its declared return type. If the method returns <c>Task</c> (no result), the implementation
    /// may return <c>null</c> or an empty object.
    /// </returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when <paramref name="clientName"/> does not match any sub-client property on <c>McpServerClient</c>
    /// (error code <c>"unknown_client"</c>) or when <paramref name="methodName"/> does not match any valid method
    /// on the resolved client (error code <c>"unknown_method"</c>).
    /// </exception>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="clientName"/>, <paramref name="methodName"/>, or <paramref name="arguments"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when a required parameter is missing (error code <c>"missing_required_parameter"</c>), when type coercion
    /// fails (error codes <c>"type_conversion_error"</c>, <c>"invalid_enum_value"</c>, <c>"json_deserialization_error"</c>,
    /// <c>"collection_conversion_error"</c>), or when a non-nullable parameter receives <c>null</c>
    /// (error code <c>"null_for_nonnullable_parameter"</c>).
    /// </exception>
    /// <exception cref="System.Reflection.TargetInvocationException">
    /// Thrown when the underlying method invocation fails. The inner exception contains the actual error from the invoked method.
    /// This is wrapped with error code <c>"method_invocation_error"</c> for consistent error handling.
    /// </exception>
    Task<object?> InvokeAsync(
        string clientName,
        string methodName,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);
}
