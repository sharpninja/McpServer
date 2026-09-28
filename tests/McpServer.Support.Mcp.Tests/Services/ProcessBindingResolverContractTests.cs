using Xunit;

#if PROCESS_MOCK
using TargetBinding = McpServer.Support.Mcp.Tests.Services.MockProcessBinding;
using TargetResolver = McpServer.Support.Mcp.Tests.Services.MockProcessBindingResolver;
#else
using TargetBinding = McpServer.Support.Mcp.Services.ProcessBindingRecord;
using TargetResolver = McpServer.Support.Mcp.Services.ProcessBindingResolver;
#endif

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-PROCESS-001 increment A: the same cases first exercise a test double,
/// then the server resolver. PROCESS_MOCK is test-only and never gates production.
/// </summary>
public sealed class ProcessBindingResolverContractTests
{
    private const string Workspace = @"F:\Fixture";
    private const string Todo = "PLAN-FIXTURE-001";

    [Fact]
    public void TodoBindingPrecedesWorkspaceAndGlobal()
    {
        var rows = new List<TargetBinding>
        {
            Binding("global", "*", "global-1", "global-flow", 1),
            Binding("workspace", Workspace, "workspace-1", "workspace-flow", 2),
            Binding("todo", Todo, "todo-1", "BDPv4", 3),
        };
        var actual = new TargetResolver(rows).Resolve(Workspace, Todo, "slice-1");
        AssertAssigned(actual, "todo-1", "BDPv4", 3);
    }

    [Fact]
    public void WorkspaceBindingPrecedesGlobalWhenTodoBindingAbsent()
    {
        var rows = new List<TargetBinding>
        {
            Binding("global", "*", "global-1", "global-flow", 1),
            Binding("workspace", Workspace, "workspace-1", "review-flow", 2),
        };
        AssertAssigned(new TargetResolver(rows).Resolve(Workspace, Todo, "slice-1"),
            "workspace-1", "review-flow", 2);
    }

    [Fact]
    public void ApprovedGlobalDefaultAppliesWhenNarrowerBindingAbsent()
    {
        var rows = new List<TargetBinding>
        {
            Binding("global", "*", "global-1", "global-flow", 1),
        };
        AssertAssigned(new TargetResolver(rows).Resolve(Workspace, Todo, "slice-1"),
            "global-1", "global-flow", 1);
    }

    [Fact]
    public void MissingBindingIsUnavailableNotUnmanaged()
    {
        var actual = new TargetResolver(new List<TargetBinding>())
            .Resolve(Workspace, Todo, "slice-1");
        Assert.Equal("PROCESS_CONTEXT_UNAVAILABLE", actual.Status);
        Assert.Equal("Unavailable", actual.AssignmentState);
    }

    [Fact]
    public void DuplicateWinningScopeIsAmbiguousAndUnavailable()
    {
        var rows = new List<TargetBinding>
        {
            Binding("global", "*", "global-1", "global-flow", 1),
            Binding("todo", Todo, "todo-1", "BDPv4", 3),
            Binding("todo", Todo, "todo-2", "BDPv4", 4),
        };
        var actual = new TargetResolver(rows).Resolve(Workspace, Todo, "slice-1");
        Assert.Equal("PROCESS_CONTEXT_UNAVAILABLE", actual.Status);
        Assert.Equal("Unavailable", actual.AssignmentState);
    }

    [Fact]
    public void UnsignedBindingCannotCreateAnAssignment()
    {
        var rows = new List<TargetBinding>
        {
            Binding("todo", Todo, "todo-1", "BDPv4", 3, signed: false),
        };
        var actual = new TargetResolver(rows).Resolve(Workspace, Todo, "slice-1");
        Assert.Equal("PROCESS_CONTEXT_UNAVAILABLE", actual.Status);
        Assert.Equal("Unavailable", actual.AssignmentState);
    }

    [Fact]
    public void ExplicitUnmanagedRequiresAnApprovedRecord()
    {
        var unsigned = Binding("todo", Todo, "todo-1", "none", 3,
            signed: false, state: "ExplicitlyUnmanaged");
        var signed = Binding("todo", Todo, "todo-1", "none", 3,
            signed: true, state: "ExplicitlyUnmanaged");
        Assert.Equal("Unavailable",
            new TargetResolver(new List<TargetBinding> { unsigned })
                .Resolve(Workspace, Todo, "slice-1").AssignmentState);
        var actual = new TargetResolver(new List<TargetBinding> { signed })
            .Resolve(Workspace, Todo, "slice-1");
        Assert.Equal("ok", actual.Status);
        Assert.Equal("ExplicitlyUnmanaged", actual.AssignmentState);
        Assert.Equal("todo-1", actual.BindingId);
    }

    [Fact]
    public void ActiveSlicePinsRevisionWhileNewSliceSeesNextRevision()
    {
        var rows = new List<TargetBinding>
        {
            Binding("todo", Todo, "todo-1", "BDPv4", 7),
        };
        var resolver = new TargetResolver(rows);
        AssertAssigned(resolver.Resolve(Workspace, Todo, "slice-1"), "todo-1", "BDPv4", 7);
        rows.Clear();
        rows.Add(Binding("todo", Todo, "todo-2", "review-flow", 8));
        AssertAssigned(resolver.Resolve(Workspace, Todo, "slice-1"), "todo-1", "BDPv4", 7);
        AssertAssigned(resolver.Resolve(Workspace, Todo, "slice-2"), "todo-2", "review-flow", 8);
    }

    [Fact]
    public void WorkspaceSwitchCannotReuseAnActiveSlicePin()
    {
        var other = @"F:\Other";
        var rows = new List<TargetBinding>
        {
            Binding("workspace", Workspace, "workspace-1", "BDPv4", 7),
            Binding("workspace", other, "workspace-2", "review-flow", 8),
        };
        var resolver = new TargetResolver(rows);
        AssertAssigned(resolver.Resolve(Workspace, Todo, "same-slice"),
            "workspace-1", "BDPv4", 7);
        AssertAssigned(resolver.Resolve(other, Todo, "same-slice"),
            "workspace-2", "review-flow", 8);
    }

    private static TargetBinding Binding(string scope, string scopeId, string id,
        string methodology, int revision, bool signed = true, string state = "Assigned") =>
        new()
        {
            Scope = scope,
            ScopeId = scopeId,
            BindingId = id,
            MethodologyId = methodology,
            Revision = revision,
            SignedApproval = signed,
            AssignmentState = state,
        };

    private static void AssertAssigned(dynamic actual, string bindingId,
        string methodology, int revision)
    {
        Assert.Equal("ok", (string)actual.Status);
        Assert.Equal("Assigned", (string)actual.AssignmentState);
        Assert.Equal(bindingId, (string)actual.BindingId);
        Assert.Equal(methodology, (string)actual.MethodologyId);
        Assert.Equal(revision, (int)actual.BindingRevision);
    }
}

#if PROCESS_MOCK
/// <summary>Test-only binding row for the mock validation phase.</summary>
public sealed class MockProcessBinding
{
    public string Scope { get; init; } = "";
    public string ScopeId { get; init; } = "";
    public string BindingId { get; init; } = "";
    public string MethodologyId { get; init; } = "";
    public int Revision { get; init; }
    public bool SignedApproval { get; init; }
    public string AssignmentState { get; init; } = "";
}

/// <summary>Test-only resolution result with the intended server contract fields.</summary>
public sealed record MockProcessBindingResult(
    string Status, string AssignmentState, string BindingId,
    string MethodologyId, int BindingRevision);

/// <summary>Behavior-providing server test double used before production implementation.</summary>
public sealed class MockProcessBindingResolver
{
    private readonly List<MockProcessBinding> _rows;
    private readonly Dictionary<string, MockProcessBindingResult> _pins = new();

    public MockProcessBindingResolver(List<MockProcessBinding> rows) => _rows = rows;

    public MockProcessBindingResult Resolve(string workspace, string todo, string slice)
    {
        var key = string.Join("\u001F", workspace, todo, slice);
        if (_pins.TryGetValue(key, out var pinned))
            return pinned;
        foreach (var (scope, id) in new[] {
            ("todo", todo), ("workspace", workspace), ("global", "*") })
        {
            var matches = _rows.Where(row =>
                row.Scope == scope && row.ScopeId == id).ToArray();
            if (matches.Length == 0)
                continue;
            if (matches.Length != 1 || !matches[0].SignedApproval)
                return Unavailable();
            var row = matches[0];
            var result = new MockProcessBindingResult("ok", row.AssignmentState,
                row.BindingId, row.MethodologyId, row.Revision);
            _pins[key] = result;
            return result;
        }
        return Unavailable();
    }

    private static MockProcessBindingResult Unavailable() =>
        new("PROCESS_CONTEXT_UNAVAILABLE", "Unavailable", "", "", 0);
}
#endif
