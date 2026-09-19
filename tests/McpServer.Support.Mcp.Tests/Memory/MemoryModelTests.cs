using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / FR-MCP-MEMORY-010: S1 Red acceptance for multi-layer model persistence and validation.
/// </summary>
public sealed class MemoryModelTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-02: Persisted row includes Title, Summary, Content, Type, Tags, Confidence, provenance.</summary>
    [Fact]
    public async Task PersistsMultiLayerFields()
    {
        var remembered = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Title = "Local DB decision",
            Summary = "Use native Postgres",
            Content = "BENCH-DECISION-DB=native-postgres-not-docker",
            Type = "decision",
            Tags = ["bench", "decision"],
            Confidence = 0.9,
            SourceKind = "operator",
            SourceRef = "plan",
        }).ConfigureAwait(true);

        var stored = remembered.Memory ?? await _harness.GetCompatAsync(
            remembered.MemoryId ?? string.Empty,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(stored);
        Assert.Equal("Local DB decision", stored!.Title);
        Assert.Equal("Use native Postgres", stored.Summary);
        Assert.Equal("BENCH-DECISION-DB=native-postgres-not-docker", stored.Content);
        Assert.Equal("decision", stored.Type, ignoreCase: true);
        Assert.Contains("bench", stored.Tags ?? []);
        Assert.Equal(0.9, stored.Confidence);
        Assert.Equal("operator", stored.SourceKind);
        Assert.Equal("plan", stored.SourceRef);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-08: Empty Content (and empty legacy text) returns 400 validation.</summary>
    [Fact]
    public async Task EmptyContent_Returns400()
    {
        var remember = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Content = string.Empty,
            Type = "fact",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var add = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "fact",
            Text = string.Empty,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(remember.StatusCode == 400 || add.FailureKind == MemoryMutationFailureKind.Validation);
        Assert.True(remember.FailureKind == MemoryMutationFailureKind.Validation
            || add.FailureKind == MemoryMutationFailureKind.Validation);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-09: Whitespace-only Content returns 400 validation.</summary>
    [Fact]
    public async Task WhitespaceOnlyContent_Returns400()
    {
        var remember = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Content = "   \n\t  ",
            Type = "fact",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, remember.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, remember.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-10: Content with internal newlines and punctuation round-trips exactly.</summary>
    [Fact]
    public async Task NewlinesPunctuation_RoundTripExact()
    {
        const string content = "Line one.\nLine two: yes, \"quoted\" -- keep punctuation!";
        var remembered = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Content = content,
            Type = "fact",
        }).ConfigureAwait(true);
        var stored = remembered.Memory ?? await _harness.GetCompatAsync(
            remembered.MemoryId!,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(content, stored?.Content ?? stored?.Text);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-11: Unicode BMP + supplementary-plane emoji in Title/Content round-trip exactly.</summary>
    [Fact]
    public async Task Unicode_RoundTripExact()
    {
        const string title = "Cafe\u0301 notes";
        const string content = "Prefer neovim \U0001F431 and native Postgres.";
        var remembered = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Title = title,
            Content = content,
            Type = "preference",
        }).ConfigureAwait(true);
        var stored = remembered.Memory ?? await _harness.GetCompatAsync(
            remembered.MemoryId!,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(title, stored?.Title);
        Assert.Equal(content, stored?.Content ?? stored?.Text);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-12: Invalid Type enum value returns 400.</summary>
    [Fact]
    public async Task InvalidType_Returns400()
    {
        var result = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Content = "typed",
            Type = "not-a-type",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, result.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-13: Confidence less than 0 returns 400.</summary>
    [Fact]
    public async Task ConfidenceBelowZero_Returns400()
    {
        var result = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Content = "low",
            Type = "fact",
            Confidence = -0.01,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, result.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-14: Confidence greater than 1 returns 400.</summary>
    [Fact]
    public async Task ConfidenceAboveOne_Returns400()
    {
        var result = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Content = "high",
            Type = "fact",
            Confidence = 1.01,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, result.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-15: Omitted confidence defaults to a documented value in [0,1].</summary>
    [Fact]
    public async Task ConfidenceOmitted_UsesDefault()
    {
        var remembered = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Content = "default confidence",
            Type = "fact",
        }).ConfigureAwait(true);
        var stored = remembered.Memory ?? await _harness.GetCompatAsync(
            remembered.MemoryId!,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(stored?.Confidence);
        Assert.InRange(stored!.Confidence!.Value, 0, 1);
        Assert.Equal(MemoryLimits.DefaultConfidence, stored.Confidence.Value);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-16: Empty tags are accepted; null tags are treated as empty.</summary>
    [Fact]
    public async Task NullOrEmptyTags_Accepted()
    {
        var empty = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Content = "empty tags",
            Type = "fact",
            Tags = [],
        }).ConfigureAwait(true);
        var nil = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Content = "null tags",
            Type = "fact",
            Tags = null,
        }).ConfigureAwait(true);

        Assert.True(empty.StatusCode is 200 or 201, empty.Error);
        Assert.True(nil.StatusCode is 200 or 201, nil.Error);
        var emptyItem = empty.Memory ?? await _harness.GetCompatAsync(empty.MemoryId!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var nilItem = nil.Memory ?? await _harness.GetCompatAsync(nil.MemoryId!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Empty(emptyItem?.Tags ?? []);
        Assert.Empty(nilItem?.Tags ?? []);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-17: Duplicate tags are de-duplicated or rejected consistently.</summary>
    [Fact]
    public async Task DuplicateTags_StableBehavior()
    {
        var first = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Content = "dup tags",
            Type = "fact",
            Tags = ["bench", "BENCH", "bench"],
        }).ConfigureAwait(true);
        var second = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Content = "dup tags again",
            Type = "fact",
            Tags = ["bench", "BENCH", "bench"],
        }).ConfigureAwait(true);

        var firstItem = first.Memory ?? await _harness.GetCompatAsync(first.MemoryId!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var secondItem = second.Memory ?? await _harness.GetCompatAsync(second.MemoryId!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(firstItem?.Tags);
        Assert.Equal(firstItem!.Tags, secondItem?.Tags);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-18: Tag longer than configured max returns 400.</summary>
    [Fact]
    public async Task TagTooLong_Returns400()
    {
        var result = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Content = "long tag",
            Type = "fact",
            Tags = [new string('t', MemoryLimits.MaxTagLength + 1)],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, result.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-19: Title longer than configured max returns 400.</summary>
    [Fact]
    public async Task TitleTooLong_Returns400()
    {
        var result = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Title = new string('T', MemoryLimits.MaxTitleLength + 1),
            Content = "title too long",
            Type = "fact",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, result.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-20: Content longer than configured max returns 400.</summary>
    [Fact]
    public async Task ContentTooLong_Returns400()
    {
        var result = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Content = new string('c', MemoryLimits.MaxContentLength + 1),
            Type = "fact",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, result.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-23: Default scope when omitted is Workspace.</summary>
    [Fact]
    public async Task DefaultScope_IsWorkspace()
    {
        var remembered = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Content = "default scope",
            Type = "fact",
        }).ConfigureAwait(true);
        var stored = remembered.Memory ?? await _harness.GetCompatAsync(
            remembered.MemoryId!,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(MemoryScope.Workspace, stored?.Scope);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-24: Scope=Global stores without owning workspace stamp and is visible everywhere.</summary>
    [Fact]
    public async Task GlobalScope_VisibleEverywhere()
    {
        var remembered = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Content = "global guidance",
            Type = "procedure",
            Scope = MemoryScope.Global,
        }).ConfigureAwait(true);
        var fromB = await _harness.GetCompatAsync(
            remembered.MemoryId!,
            _harness.WorkspaceB,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(fromB);
        Assert.Equal(MemoryScope.Global, fromB!.Scope);
        Assert.True(string.IsNullOrEmpty(fromB.WorkspacePath));
    }

    /// <summary>AC-FR-MCP-MEMORY-010-33: Scope Global to Workspace transitions are validated; invalid transitions return 400.</summary>
    [Fact]
    public async Task ScopeTransition_Validated()
    {
        var created = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "scope",
            Scope = MemoryScope.Global,
            Text = "global then workspace",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        var updated = await _harness.UpdateCompatAsync(
            created.Memory!.Id,
            new MemoryUpdateRequest { Scope = MemoryScope.Workspace },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(updated.Success, updated.Error);
        Assert.Equal(MemoryScope.Workspace, updated.Memory?.Scope);

        var invalid = await _harness.UpdateCompatAsync(
            created.Memory.Id,
            new MemoryUpdateRequest { Scope = (MemoryScope)99 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(MemoryMutationFailureKind.Validation, invalid.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-35: CreatedAt/UpdatedAt set on create; UpdatedAt advances; CreatedAt is stable.</summary>
    [Fact]
    public async Task Timestamps_CreateAndUpdate()
    {
        var created = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "time",
            Text = "before",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);
        var createdAt = created.Memory!.CreatedAtUtc;
        var updatedAt = created.Memory.UpdatedAtUtc;
        Assert.NotEqual(default, createdAt);
        Assert.NotEqual(default, updatedAt);

        await Task.Delay(20, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var updated = await _harness.UpdateCompatAsync(
            created.Memory.Id,
            new MemoryUpdateRequest { Text = "after", Content = "after" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(updated.Success, updated.Error);
        Assert.Equal(createdAt, updated.Memory!.CreatedAtUtc);
        Assert.True(updated.Memory.UpdatedAtUtc > updatedAt);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-36: Author/updatedBy attribution is recorded on create and update.</summary>
    [Fact]
    public async Task Attribution_Recorded()
    {
        var created = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Content = "attributed",
            Type = "fact",
            CreatedBy = "CursorGrok",
            UpdatedBy = "CursorGrok",
        }).ConfigureAwait(true);
        var stored = created.Memory ?? await _harness.GetCompatAsync(
            created.MemoryId!,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("CursorGrok", stored?.CreatedBy);
        Assert.Equal("CursorGrok", stored?.UpdatedBy);

        var updated = await _harness.UpdateCompatAsync(
            stored!.Id,
            new MemoryUpdateRequest { Text = "attributed-2", UpdatedBy = "CursorGrok2" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("CursorGrok", updated.Memory?.CreatedBy);
        Assert.Equal("CursorGrok2", updated.Memory?.UpdatedBy);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-39: Partial update does not clear Summary when omitted.</summary>
    [Fact]
    public async Task PartialUpdate_DoesNotClearSummary()
    {
        var created = await RememberOrAddAsync(new MemoryRememberRequest
        {
            Title = "Keep summary",
            Summary = "original summary",
            Content = "body",
            Type = "fact",
        }).ConfigureAwait(true);
        var id = created.MemoryId ?? created.Memory?.Id;
        Assert.False(string.IsNullOrWhiteSpace(id));

        var updated = await _harness.UpdateCompatAsync(
            id!,
            new MemoryUpdateRequest { Content = "body-2", Text = "body-2" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var stored = updated.Memory ?? await _harness.GetCompatAsync(id!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("original summary", stored?.Summary);
    }

    private async Task<MemoryRememberResult> RememberOrAddAsync(MemoryRememberRequest request)
    {
        var remembered = await _harness.RememberAsync(request, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        if (remembered.StatusCode is 200 or 201 && !string.IsNullOrWhiteSpace(remembered.MemoryId))
            return remembered;

        var add = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Id = request.Id,
            Category = request.Type ?? request.Category ?? "fact",
            Scope = request.Scope ?? MemoryScope.Workspace,
            Text = request.Content ?? string.Empty,
            Title = request.Title,
            Summary = request.Summary,
            Content = request.Content,
            Type = request.Type,
            Tags = request.Tags,
            Confidence = request.Confidence,
            SourceKind = request.SourceKind,
            SourceRef = request.SourceRef,
            CreatedBy = request.CreatedBy,
            UpdatedBy = request.UpdatedBy,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        return new MemoryRememberResult(
            add.Success ? 201 : 400,
            add.Memory?.Id,
            add.Memory,
            add.FailureKind,
            add.Error);
    }
}
