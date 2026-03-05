using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Abstraction over requirements endpoints used by UI.Core CQRS handlers.
/// </summary>
public interface IRequirementsApiClient
{
    /// <summary>Lists functional requirements.</summary>
    Task<FunctionalRequirementListResult> ListFunctionalRequirementsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a functional requirement by ID.</summary>
    Task<FunctionalRequirementItem?> GetFunctionalRequirementAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Creates a functional requirement.</summary>
    Task<FunctionalRequirementItem> CreateFunctionalRequirementAsync(CreateFunctionalRequirementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Updates a functional requirement.</summary>
    Task<FunctionalRequirementItem> UpdateFunctionalRequirementAsync(UpdateFunctionalRequirementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Deletes a functional requirement.</summary>
    Task<RequirementsMutationOutcome> DeleteFunctionalRequirementAsync(DeleteFunctionalRequirementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Lists technical requirements.</summary>
    Task<TechnicalRequirementListResult> ListTechnicalRequirementsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a technical requirement by ID.</summary>
    Task<TechnicalRequirementItem?> GetTechnicalRequirementAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Creates a technical requirement.</summary>
    Task<TechnicalRequirementItem> CreateTechnicalRequirementAsync(CreateTechnicalRequirementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Updates a technical requirement.</summary>
    Task<TechnicalRequirementItem> UpdateTechnicalRequirementAsync(UpdateTechnicalRequirementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Deletes a technical requirement.</summary>
    Task<RequirementsMutationOutcome> DeleteTechnicalRequirementAsync(DeleteTechnicalRequirementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Lists testing requirements.</summary>
    Task<TestingRequirementListResult> ListTestingRequirementsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a testing requirement by ID.</summary>
    Task<TestingRequirementItem?> GetTestingRequirementAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Creates a testing requirement.</summary>
    Task<TestingRequirementItem> CreateTestingRequirementAsync(CreateTestingRequirementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Updates a testing requirement.</summary>
    Task<TestingRequirementItem> UpdateTestingRequirementAsync(UpdateTestingRequirementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Deletes a testing requirement.</summary>
    Task<RequirementsMutationOutcome> DeleteTestingRequirementAsync(DeleteTestingRequirementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Lists FR-to-TR mapping rows.</summary>
    Task<RequirementMappingListResult> ListMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets an FR-to-TR mapping row by FR ID.</summary>
    Task<RequirementMappingItem?> GetMappingAsync(string frId, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates an FR-to-TR mapping row.</summary>
    Task<RequirementMappingItem> UpsertMappingAsync(UpsertRequirementMappingCommand command, CancellationToken cancellationToken = default);

    /// <summary>Deletes an FR-to-TR mapping row.</summary>
    Task<RequirementsMutationOutcome> DeleteMappingAsync(DeleteRequirementMappingCommand command, CancellationToken cancellationToken = default);

    /// <summary>Generates requirements output as markdown or zip.</summary>
    Task<GeneratedRequirementsDocument> GenerateAsync(GenerateRequirementsDocumentQuery query, CancellationToken cancellationToken = default);
}
