---
title: McpServer Use Case Extension – Full Design & Implementation Package
version: 1.0
date: 2026-07-10
author: Grok (team collaboration with realsharpninja)
status: Ready for implementation
---

# McpServer Use Case Extension – Full Design & Implementation Package

## 1. Executive Summary (Your Preferences Applied)
- **Default LinkType** when creating from FR: **Realizes**  
- **Diagram format**: **Mermaid only**  
- **Design surface priority**: **Pure API + external editor support** (no Blazor in Phase 1)  
- **Naming conventions**: **Match existing** (`UseCase*`, `/mcpserver/usecases`, etc.)  
- **FR derived from Use Case (reverse flow)**: **Yes** (fully supported)

This extension adds structured Use Case modeling with live Mermaid diagrams, 4NF storage, and bidirectional traceability to your existing FR surface — zero breaking changes.

## 2. Data Model (4NF)

### SQL Schema (copy-paste ready)
```sql
CREATE TABLE UseCases (
    UseCaseId BIGINT PRIMARY KEY IDENTITY,
    WorkspaceId NVARCHAR(50) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    BriefDescription NVARCHAR(MAX),
    Precondition NVARCHAR(MAX),
    Postcondition NVARCHAR(MAX),
    Scope NVARCHAR(50),
    Priority INT,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE Actors (
    ActorId BIGINT PRIMARY KEY IDENTITY,
    WorkspaceId NVARCHAR(50) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    Type NVARCHAR(20) CHECK (Type IN ('Primary','Secondary','System','External'))
);

CREATE TABLE UseCaseActors (
    UseCaseId BIGINT NOT NULL,
    ActorId BIGINT NOT NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,
    PRIMARY KEY (UseCaseId, ActorId)
);

CREATE TABLE UseCaseFlows (
    FlowId BIGINT PRIMARY KEY IDENTITY,
    UseCaseId BIGINT NOT NULL,
    FlowType NVARCHAR(20) CHECK (FlowType IN ('Basic','Alternative','Exception')),
    Name NVARCHAR(100),
    SequenceNumber INT NOT NULL
);

CREATE TABLE UseCaseSteps (
    StepId BIGINT PRIMARY KEY IDENTITY,
    FlowId BIGINT NOT NULL,
    StepNumber INT NOT NULL,
    ActorId BIGINT NULL,
    Action NVARCHAR(MAX) NOT NULL,
    SystemResponse NVARCHAR(MAX),
    DataEntities NVARCHAR(MAX)
);

CREATE TABLE UseCaseSpecialRequirements (
    SpecialReqId BIGINT PRIMARY KEY IDENTITY,
    UseCaseId BIGINT NOT NULL,
    Category NVARCHAR(50),
    RequirementText NVARCHAR(MAX),
    Priority INT
);

CREATE TABLE UseCaseExtensionPoints (
    ExtensionPointId BIGINT PRIMARY KEY IDENTITY,
    UseCaseId BIGINT NOT NULL,
    Name NVARCHAR(100),
    Description NVARCHAR(MAX)
);

-- FR Association (the key integration point)
CREATE TABLE UseCaseFrLinks (
    LinkId BIGINT PRIMARY KEY IDENTITY,
    UseCaseId BIGINT NOT NULL,
    FrId BIGINT NOT NULL,
    LinkType NVARCHAR(20) NOT NULL DEFAULT 'Realizes',
    LinkOrder INT NOT NULL DEFAULT 0,
    Notes NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_UCFL UNIQUE (UseCaseId, FrId)
);
```

### C# EF Core Entities (drop-in ready)
```csharp
public class UseCase
{
    public long UseCaseId { get; set; }
    public string WorkspaceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? BriefDescription { get; set; }
    public string? Precondition { get; set; }
    public string? Postcondition { get; set; }
    public string? Scope { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UseCaseActor> UseCaseActors { get; set; } = new List<UseCaseActor>();
    public ICollection<UseCaseFlow> Flows { get; set; } = new List<UseCaseFlow>();
    public ICollection<UseCaseSpecialRequirement> SpecialRequirements { get; set; } = new List<UseCaseSpecialRequirement>();
    public ICollection<UseCaseExtensionPoint> ExtensionPoints { get; set; } = new List<UseCaseExtensionPoint>();
    public ICollection<UseCaseFrLink> FrLinks { get; set; } = new List<UseCaseFrLink>();
}

public class UseCaseFrLink
{
    public long LinkId { get; set; }
    public long UseCaseId { get; set; }
    public long FrId { get; set; }
    public string LinkType { get; set; } = "Realizes";   // your default
    public int LinkOrder { get; set; }
    public string? Notes { get; set; }

    public UseCase UseCase { get; set; } = null!;
    public FunctionalRequirement Fr { get; set; } = null!;
}
```

**Migration**  
`Add-Migration AddUseCaseSupport` → `Update-Database`

## 3. API & CQRS (Pure API First)
- `POST /mcpserver/usecases` — create Use Case (+ optional FR link, default "Realizes")
- `GET /mcpserver/usecases/{id}/diagram?format=mermaid`
- Extended FR endpoint includes `linkedUseCases` array
- Symmetric reverse linking fully supported

## 4. Diagram Generation Service
```csharp
public class UseCaseDiagramService
{
    public string GenerateMermaid(long useCaseId)
    {
        // Query 4NF data and build Mermaid string
        return "sequenceDiagram\n    participant Actor\n    ...";
    }
}
```

## 5. Validation Extension
Extend `ValidateTraceability` to check bidirectional coverage using `UseCaseFrLinks` (default "Realizes").

## 6. Agent & External Editor Support
New agent tools:
- `CreateUseCaseFromFr(frId)`
- `RenderMermaidDiagram(useCaseId)`
- `LinkUseCaseToFr(useCaseId, frId)`

External editors (Mermaid Live, VS Code, Obsidian, etc.) consume the pure REST API.

## 7. Phased Rollout
- Phase 0: Data model + migration (1 day)  
- Phase 1: Junction + FR surface + validation (2 days)  
- Phase 2: Mermaid generation + API (2 days)  
- Phase 3: Agents + GraphRAG (1 day)

**Total estimated effort**: ~6 days for v1.

**End of Document**
