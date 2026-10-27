// SPDX-License-Identifier: Apache-2.0
namespace QBrainAi.Repl.Core;

/// <summary>
/// Preserves the original JSON parameters at the existing REPL envelope boundary.
/// The serializer derives this value from input bytes, never from a params field.
/// </summary>
public interface ILosslessRequestPayload : IRequestPayload
{
    /// <summary>Gets exact JSON parameters when the input document was JSON.</summary>
    string? RawParamsJson { get; }
}

/// <summary>
/// Constructor-injected host adapter to the canonical PowerShell A8 lifecycle.
/// Implementations capture validated host options separately from tool arguments.
/// This boundary owns no turn map, latest-session selection or MCP transport.
/// </summary>
public interface IGovernedSessionDispatch
{
    /// <summary>Invokes a canonical governed method with exact untrusted content.</summary>
    /// <param name="method">The selected S6 or existing lifecycle method name.</param>
    /// <param name="paramsJson">Original parameters, including raw review lexemes.</param>
    /// <param name="cancellationToken">Cancellation propagated to the host adapter.</param>
    /// <returns>The canonical host's rooted persistence or refusal result.</returns>
    Task<object?> InvokeAsync(string method, string paramsJson, CancellationToken cancellationToken);
}
