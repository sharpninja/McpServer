using System;
using System.Collections.Generic;
using McpServer.Client.Models;

// FR-MCP-REPL-003: Command Namespace Parity - Requirements command structures
// TR-MCP-REPL-001: YAML Envelope Protocol - Requirements command data models
// TEST-MCP-REPL-009: Requirements REPL commands match REST endpoint semantics

// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Requirements workflow data models
// FR-MCP-REPL-003: Command Namespace Parity - Requirements operation data models
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Requirements models
// TEST-MCP-REPL-009: Requirements management operations validate requirement identifier rules

namespace McpServer.Repl.Core;

/// <inheritdoc />
public sealed class FrCreateRequestModel : IFrCreateRequest
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Title { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Priority { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Area { get; set; } = string.Empty;
    /// <inheritdoc />
    public string? Notes { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <inheritdoc />
public sealed class FrUpdateRequestModel : IFrUpdateRequest
{
    /// <inheritdoc />
    public string? Id { get; set; }
    /// <inheritdoc />
    public string? Title { get; set; }
    /// <inheritdoc />
    public string? Description { get; set; }
    /// <inheritdoc />
    public string? Status { get; set; }
    /// <inheritdoc />
    public string? Priority { get; set; }
    /// <inheritdoc />
    public string? Notes { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <inheritdoc />
public sealed class TrCreateRequestModel : ITrCreateRequest
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Title { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Priority { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Area { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Subarea { get; set; } = string.Empty;
    /// <inheritdoc />
    public string? Notes { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <inheritdoc />
public sealed class TrUpdateRequestModel : ITrUpdateRequest
{
    /// <inheritdoc />
    public string? Id { get; set; }
    /// <inheritdoc />
    public string? Title { get; set; }
    /// <inheritdoc />
    public string? Description { get; set; }
    /// <inheritdoc />
    public string? Status { get; set; }
    /// <inheritdoc />
    public string? Priority { get; set; }
    /// <inheritdoc />
    public string? Notes { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <inheritdoc />
public sealed class TestCreateRequestModel : ITestCreateRequest
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Title { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Priority { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Area { get; set; } = string.Empty;
    /// <inheritdoc />
    public string TestType { get; set; } = string.Empty;
    /// <inheritdoc />
    public string? Notes { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <inheritdoc />
public sealed class TestUpdateRequestModel : ITestUpdateRequest
{
    /// <inheritdoc />
    public string? Id { get; set; }
    /// <inheritdoc />
    public string? Title { get; set; }
    /// <inheritdoc />
    public string? Description { get; set; }
    /// <inheritdoc />
    public string? Status { get; set; }
    /// <inheritdoc />
    public string? Priority { get; set; }
    /// <inheritdoc />
    public string? Notes { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <inheritdoc />
public sealed class MappingCreateRequestModel : IMappingCreateRequest
{
    /// <inheritdoc />
    public string? FrId { get; set; }
    /// <inheritdoc />
    public string? TrId { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<string>? TrIds { get; set; }
    /// <inheritdoc />
    public string? TestId { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<string>? TestIds { get; set; }
    /// <inheritdoc />
    public string? Notes { get; set; }
}

/// <inheritdoc />
public sealed class ListFrParamsModel : IListFrParams
{
    /// <inheritdoc />
    public string? Area { get; set; }
    /// <inheritdoc />
    public string? Status { get; set; }
}

/// <inheritdoc />
public sealed class GetFrParamsModel : IGetFrParams
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class CreateFrParamsModel : ICreateFrParams
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Title { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Priority { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Area { get; set; } = string.Empty;
    /// <inheritdoc />
    public string? Notes { get; set; }
}

/// <inheritdoc />
public sealed class UpdateFrParamsModel : IUpdateFrParams
{
    /// <inheritdoc />
    public string? Id { get; set; }
    /// <inheritdoc />
    public string? Title { get; set; }
    /// <inheritdoc />
    public string? Description { get; set; }
    /// <inheritdoc />
    public string? Status { get; set; }
    /// <inheritdoc />
    public string? Priority { get; set; }
    /// <inheritdoc />
    public string? Notes { get; set; }
}

/// <inheritdoc />
public sealed class DeleteFrParamsModel : IDeleteFrParams
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class DeleteFrResultModel : IDeleteFrResult
{
    /// <inheritdoc />
    public bool Deleted { get; set; }
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class ListTrParamsModel : IListTrParams
{
    /// <inheritdoc />
    public string? Area { get; set; }
    /// <inheritdoc />
    public string? Subarea { get; set; }
    /// <inheritdoc />
    public string? Status { get; set; }
}

/// <inheritdoc />
public sealed class GetTrParamsModel : IGetTrParams
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class CreateTrParamsModel : ICreateTrParams
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Title { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Priority { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Area { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Subarea { get; set; } = string.Empty;
    /// <inheritdoc />
    public string? Notes { get; set; }
}

/// <inheritdoc />
public sealed class UpdateTrParamsModel : IUpdateTrParams
{
    /// <inheritdoc />
    public string? Id { get; set; }
    /// <inheritdoc />
    public string? Title { get; set; }
    /// <inheritdoc />
    public string? Description { get; set; }
    /// <inheritdoc />
    public string? Status { get; set; }
    /// <inheritdoc />
    public string? Priority { get; set; }
    /// <inheritdoc />
    public string? Notes { get; set; }
}

/// <inheritdoc />
public sealed class DeleteTrParamsModel : IDeleteTrParams
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class DeleteTrResultModel : IDeleteTrResult
{
    /// <inheritdoc />
    public bool Deleted { get; set; }
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class ListTestParamsModel : IListTestParams
{
    /// <inheritdoc />
    public string? Area { get; set; }
    /// <inheritdoc />
    public string? Status { get; set; }
}

/// <inheritdoc />
public sealed class GetTestParamsModel : IGetTestParams
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class CreateTestParamsModel : ICreateTestParams
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Title { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Priority { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Area { get; set; } = string.Empty;
    /// <inheritdoc />
    public string TestType { get; set; } = string.Empty;
    /// <inheritdoc />
    public string? Notes { get; set; }
}

/// <inheritdoc />
public sealed class UpdateTestParamsModel : IUpdateTestParams
{
    /// <inheritdoc />
    public string? Id { get; set; }
    /// <inheritdoc />
    public string? Title { get; set; }
    /// <inheritdoc />
    public string? Description { get; set; }
    /// <inheritdoc />
    public string? Status { get; set; }
    /// <inheritdoc />
    public string? Priority { get; set; }
    /// <inheritdoc />
    public string? Notes { get; set; }
}

/// <inheritdoc />
public sealed class DeleteTestParamsModel : IDeleteTestParams
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class DeleteTestResultModel : IDeleteTestResult
{
    /// <inheritdoc />
    public bool Deleted { get; set; }
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class ListMappingsParamsModel : IListMappingsParams
{
    /// <inheritdoc />
    public string? FrId { get; set; }
    /// <inheritdoc />
    public string? TrId { get; set; }
    /// <inheritdoc />
    public string? TestId { get; set; }
}

/// <inheritdoc />
public sealed class CreateMappingParamsModel : ICreateMappingParams
{
    /// <inheritdoc />
    public string? FrId { get; set; }
    /// <inheritdoc />
    public string? TrId { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<string>? TrIds { get; set; }
    /// <inheritdoc />
    public string? TestId { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<string>? TestIds { get; set; }
    /// <inheritdoc />
    public string? Notes { get; set; }
}

/// <inheritdoc />
public sealed class DeleteMappingParamsModel : IDeleteMappingParams
{
    /// <inheritdoc />
    public string? FrId { get; set; }
    /// <inheritdoc />
    public string? TrId { get; set; }
    /// <inheritdoc />
    public string? TestId { get; set; }
}

/// <inheritdoc />
public sealed class DeleteMappingResultModel : IDeleteMappingResult
{
    /// <inheritdoc />
    public bool Deleted { get; set; }
    /// <inheritdoc />
    public string? FrId { get; set; }
    /// <inheritdoc />
    public string? TrId { get; set; }
    /// <inheritdoc />
    public string? TestId { get; set; }
}

/// <inheritdoc />
public sealed class GenerateDocumentParamsModel : IGenerateDocumentParams
{
    /// <inheritdoc />
    public string Format { get; set; } = string.Empty;
    /// <inheritdoc />
    public string DocType { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class GenerateDocumentResultModel : IGenerateDocumentResult
{
    /// <inheritdoc />
    public bool Success { get; set; }
    /// <inheritdoc />
    public string Content { get; set; } = string.Empty;
    /// <inheritdoc />
    public string? ContentBase64 { get; set; }
    /// <inheritdoc />
    public string? ContentType { get; set; }
    /// <inheritdoc />
    public string? FileName { get; set; }
    /// <inheritdoc />
    public string? OutputRoot { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<RequirementsDocumentExportFile> Files { get; set; } = [];
    /// <inheritdoc />
    public string Format { get; set; } = string.Empty;
    /// <inheritdoc />
    public string DocType { get; set; } = string.Empty;
    /// <inheritdoc />
    public DateTimeOffset GeneratedAt { get; set; }
}

/// <inheritdoc />
public sealed class IngestDocumentParamsModel : IIngestDocumentParams
{
    /// <inheritdoc />
    public string Content { get; set; } = string.Empty;
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Documents { get; set; }
    /// <inheritdoc />
    public string Format { get; set; } = string.Empty;
    /// <inheritdoc />
    public string? SourceFormat { get; set; }
    /// <inheritdoc />
    public string? PreferredWikiFormat { get; set; }
    /// <inheritdoc />
    public string MergeStrategy { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class IngestDocumentResultModel : IIngestDocumentResult
{
    /// <inheritdoc />
    public bool Success { get; set; }
    /// <inheritdoc />
    public int FrCreated { get; set; }
    /// <inheritdoc />
    public int FrUpdated { get; set; }
    /// <inheritdoc />
    public int TrCreated { get; set; }
    /// <inheritdoc />
    public int TrUpdated { get; set; }
    /// <inheritdoc />
    public int TestCreated { get; set; }
    /// <inheritdoc />
    public int TestUpdated { get; set; }
    /// <inheritdoc />
    public int MappingsCreated { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<IIngestionConflict> Conflicts { get; set; } = new List<IIngestionConflict>();
    /// <inheritdoc />
    public DateTimeOffset IngestedAt { get; set; }
}

/// <inheritdoc />
public sealed class IngestionConflictModel : IIngestionConflict
{
    /// <inheritdoc />
    public string RequirementId { get; set; } = string.Empty;
    /// <inheritdoc />
    public string ConflictType { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Resolution { get; set; } = string.Empty;
}

/// <inheritdoc />
public sealed class RequirementsCurrentSelectionParamsModel : IRequirementsCurrentSelectionParams
{
}

/// <inheritdoc />
public sealed class RequirementsCurrentSelectionResultModel : IRequirementsCurrentSelectionResult
{
    /// <inheritdoc />
    public string? FrId { get; set; }
    /// <inheritdoc />
    public string? TrId { get; set; }
    /// <inheritdoc />
    public string? TestId { get; set; }
    /// <inheritdoc />
    public DateTimeOffset? SelectedAt { get; set; }
}

/// <inheritdoc />
public sealed class RequirementsErrorModel : IRequirementsError
{
    /// <inheritdoc />
    public string RequestId { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Code { get; set; } = string.Empty;
    /// <inheritdoc />
    public string Message { get; set; } = string.Empty;
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Details { get; set; }
}

/// <inheritdoc />
public sealed class ListFrResultModel : IListFrResult
{
    /// <inheritdoc />
    public IReadOnlyList<IFrItem> Items { get; set; } = new List<IFrItem>();
    /// <inheritdoc />
    public int TotalCount { get; set; }
}

/// <inheritdoc />
public sealed class GetFrResultModel : IGetFrResult
{
    /// <inheritdoc />
    public IFrItem Item { get; set; } = null!;
}

/// <inheritdoc />
public sealed class CreateFrResultModel : ICreateFrResult
{
    /// <inheritdoc />
    public bool Success { get; set; }
    /// <inheritdoc />
    public IFrItem Item { get; set; } = null!;
}

/// <inheritdoc />
public sealed class UpdateFrResultModel : IUpdateFrResult
{
    /// <inheritdoc />
    public bool Success { get; set; }
    /// <inheritdoc />
    public IFrItem Item { get; set; } = null!;
}

/// <inheritdoc />
public sealed class ListTrResultModel : IListTrResult
{
    /// <inheritdoc />
    public IReadOnlyList<ITrItem> Items { get; set; } = new List<ITrItem>();
    /// <inheritdoc />
    public int TotalCount { get; set; }
}

/// <inheritdoc />
public sealed class GetTrResultModel : IGetTrResult
{
    /// <inheritdoc />
    public ITrItem Item { get; set; } = null!;
}

/// <inheritdoc />
public sealed class CreateTrResultModel : ICreateTrResult
{
    /// <inheritdoc />
    public bool Success { get; set; }
    /// <inheritdoc />
    public ITrItem Item { get; set; } = null!;
}

/// <inheritdoc />
public sealed class UpdateTrResultModel : IUpdateTrResult
{
    /// <inheritdoc />
    public bool Success { get; set; }
    /// <inheritdoc />
    public ITrItem Item { get; set; } = null!;
}

/// <inheritdoc />
public sealed class ListTestResultModel : IListTestResult
{
    /// <inheritdoc />
    public IReadOnlyList<ITestItem> Items { get; set; } = new List<ITestItem>();
    /// <inheritdoc />
    public int TotalCount { get; set; }
}

/// <inheritdoc />
public sealed class GetTestResultModel : IGetTestResult
{
    /// <inheritdoc />
    public ITestItem Item { get; set; } = null!;
}

/// <inheritdoc />
public sealed class CreateTestResultModel : ICreateTestResult
{
    /// <inheritdoc />
    public bool Success { get; set; }
    /// <inheritdoc />
    public ITestItem Item { get; set; } = null!;
}

/// <inheritdoc />
public sealed class UpdateTestResultModel : IUpdateTestResult
{
    /// <inheritdoc />
    public bool Success { get; set; }
    /// <inheritdoc />
    public ITestItem Item { get; set; } = null!;
}

/// <inheritdoc />
public sealed class ListMappingsResultModel : IListMappingsResult
{
    /// <inheritdoc />
    public IReadOnlyList<IMappingItem> Items { get; set; } = new List<IMappingItem>();
    /// <inheritdoc />
    public int TotalCount { get; set; }
}

/// <inheritdoc />
public sealed class CreateMappingResultModel : ICreateMappingResult
{
    /// <inheritdoc />
    public bool Success { get; set; }
    /// <inheritdoc />
    public IMappingItem Item { get; set; } = null!;
}
