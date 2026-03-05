using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query to list functional requirements.</summary>
public sealed record ListFunctionalRequirementsQuery : IQuery<FunctionalRequirementListResult>;

/// <summary>Query to get a functional requirement by ID.</summary>
public sealed record GetFunctionalRequirementQuery(string Id) : IQuery<FunctionalRequirementItem?>;

/// <summary>Command to create a functional requirement.</summary>
public sealed record CreateFunctionalRequirementCommand(string Id, string Title, string Body) : ICommand<FunctionalRequirementItem>;

/// <summary>Command to update a functional requirement.</summary>
public sealed record UpdateFunctionalRequirementCommand(string Id, string Title, string Body) : ICommand<FunctionalRequirementItem>;

/// <summary>Command to delete a functional requirement.</summary>
public sealed record DeleteFunctionalRequirementCommand(string Id) : ICommand<RequirementsMutationOutcome>;

/// <summary>Query to list technical requirements.</summary>
public sealed record ListTechnicalRequirementsQuery : IQuery<TechnicalRequirementListResult>;

/// <summary>Query to get a technical requirement by ID.</summary>
public sealed record GetTechnicalRequirementQuery(string Id) : IQuery<TechnicalRequirementItem?>;

/// <summary>Command to create a technical requirement.</summary>
public sealed record CreateTechnicalRequirementCommand(string Id, string? Title, string Body) : ICommand<TechnicalRequirementItem>;

/// <summary>Command to update a technical requirement.</summary>
public sealed record UpdateTechnicalRequirementCommand(string Id, string? Title, string Body) : ICommand<TechnicalRequirementItem>;

/// <summary>Command to delete a technical requirement.</summary>
public sealed record DeleteTechnicalRequirementCommand(string Id) : ICommand<RequirementsMutationOutcome>;

/// <summary>Query to list testing requirements.</summary>
public sealed record ListTestingRequirementsQuery : IQuery<TestingRequirementListResult>;

/// <summary>Query to get a testing requirement by ID.</summary>
public sealed record GetTestingRequirementQuery(string Id) : IQuery<TestingRequirementItem?>;

/// <summary>Command to create a testing requirement.</summary>
public sealed record CreateTestingRequirementCommand(string Id, string Condition) : ICommand<TestingRequirementItem>;

/// <summary>Command to update a testing requirement.</summary>
public sealed record UpdateTestingRequirementCommand(string Id, string Condition) : ICommand<TestingRequirementItem>;

/// <summary>Command to delete a testing requirement.</summary>
public sealed record DeleteTestingRequirementCommand(string Id) : ICommand<RequirementsMutationOutcome>;

/// <summary>Query to list FR-to-TR mapping rows.</summary>
public sealed record ListRequirementMappingsQuery : IQuery<RequirementMappingListResult>;

/// <summary>Query to get a mapping row by FR ID.</summary>
public sealed record GetRequirementMappingQuery(string FrId) : IQuery<RequirementMappingItem?>;

/// <summary>Command to upsert a mapping row by FR ID.</summary>
public sealed record UpsertRequirementMappingCommand(string FrId, IReadOnlyList<string> TrIds) : ICommand<RequirementMappingItem>;

/// <summary>Command to delete a mapping row by FR ID.</summary>
public sealed record DeleteRequirementMappingCommand(string FrId) : ICommand<RequirementsMutationOutcome>;

/// <summary>Query to generate requirements output for a specific doc selector.</summary>
public sealed record GenerateRequirementsDocumentQuery(string Doc = "all") : IQuery<GeneratedRequirementsDocument>;

/// <summary>List result for functional requirements.</summary>
public sealed record FunctionalRequirementListResult(IReadOnlyList<FunctionalRequirementItem> Items);

/// <summary>Functional requirement item.</summary>
public sealed record FunctionalRequirementItem(string Id, string Title, string Body);

/// <summary>List result for technical requirements.</summary>
public sealed record TechnicalRequirementListResult(IReadOnlyList<TechnicalRequirementItem> Items);

/// <summary>Technical requirement item.</summary>
public sealed record TechnicalRequirementItem(string Id, string Title, string Body);

/// <summary>List result for testing requirements.</summary>
public sealed record TestingRequirementListResult(IReadOnlyList<TestingRequirementItem> Items);

/// <summary>Testing requirement item.</summary>
public sealed record TestingRequirementItem(string Id, string Condition);

/// <summary>List result for requirement mappings.</summary>
public sealed record RequirementMappingListResult(IReadOnlyList<RequirementMappingItem> Items);

/// <summary>FR-to-TR mapping row.</summary>
public sealed record RequirementMappingItem(string FrId, IReadOnlyList<string> TrIds);

/// <summary>Mutation result for requirements endpoints.</summary>
public sealed record RequirementsMutationOutcome(bool Success, string? Error);

/// <summary>Generated requirements binary output.</summary>
public sealed record GeneratedRequirementsDocument(byte[] Content, string? ContentType);
