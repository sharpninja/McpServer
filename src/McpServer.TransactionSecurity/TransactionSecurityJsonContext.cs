using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;

namespace McpServer.TransactionSecurity;

/// <summary>
/// Provides source-generated JSON metadata for transaction-security request and response DTOs.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PartyRegistrationRequest))]
[JsonSerializable(typeof(PartyRegistrationResponse))]
[JsonSerializable(typeof(PartyKeyDescriptor))]
[JsonSerializable(typeof(List<PartyKeyDescriptor>))]
[JsonSerializable(typeof(TransactionManifestSignRequest))]
[JsonSerializable(typeof(TransactionManifestAlgorithms))]
[JsonSerializable(typeof(TransactionManifestSignResponse))]
[JsonSerializable(typeof(TransactionManifestVerifyRequest))]
[JsonSerializable(typeof(TransactionManifestVerifyResponse))]
[JsonSerializable(typeof(TransactionManifestTraceRecord))]
[JsonSerializable(typeof(List<TransactionManifestTraceRecord>))]
[JsonSerializable(typeof(TransactionManifestTraceReportRequest))]
[JsonSerializable(typeof(TransactionManifestTraceReport))]
[JsonSerializable(typeof(TransactionManifestDto))]
[JsonSerializable(typeof(TransactionManifestSignatureDto))]
[JsonSerializable(typeof(DiffgramCommitRequest))]
[JsonSerializable(typeof(DiffgramCommitResponse))]
[JsonSerializable(typeof(TransactionStatusResponse))]
[JsonSerializable(typeof(TransactionAbortRequest))]
[JsonSerializable(typeof(TransactionAbortResponse))]
[JsonSerializable(typeof(TransactionPubSubEnvelope))]
[JsonSerializable(typeof(TransactionPubSubAcknowledgement))]
[JsonSerializable(typeof(TurnTransactionRequest))]
[JsonSerializable(typeof(TurnMutationResult))]
[JsonSerializable(typeof(TurnTransactionResult))]
[JsonSerializable(typeof(TurnTransactionStatusResponse))]
[JsonSerializable(typeof(TransactionFailureReason))]
[JsonSerializable(typeof(TransactionDiffgramProtector.ProtectedDiffgramEnvelope))]
[JsonSerializable(typeof(SubscriberMessageLogPayload[]))]
[JsonSerializable(typeof(TurnTransactionDiffgramBody))]
internal sealed partial class TransactionSecurityJsonContext : JsonSerializerContext;
