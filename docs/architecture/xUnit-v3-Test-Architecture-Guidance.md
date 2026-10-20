# xUnit.net v3 Test Architecture Guidance

Binding team guidance for writing and structuring .NET tests with xUnit.net v3. It applies to every test project owned by Payton Byrd, including the C# emulator and its test harness, the Avalonia UI, and the MCP server tooling. It binds human contributors and AI coding agents equally.

- Document owner: Payton Byrd
- Research date: Oct 20, 2026
- Versions verified on that date (xunit.net home page and NuGet): `xunit.v3` 4.0.2, `xunit.analyzers` 2.2.0, `xunit.runner.visualstudio` 4.0.1, `xunit.v3.templates` 4.0.2, `xunit-console-tool` 4.0.2
- Version target versus current pin: 4.0.2 is the target version for test projects. It is not what every repository uses today. For example, McpServer's `Directory.Packages.props` centrally pins `xunit.v3` to 3.2.2 at the time of writing. Until a repository completes the 4.0 migration (section 16), its checked-in pin wins, and rules marked as 4.0 only do not apply to it yet.
- All facts come from live primary sources (xunit.net, the xunit GitHub repositories, NuGet, Microsoft Learn, and Avalonia's own docs and repository). Anything I could not verify is either left out or listed in the "Unverified items and discrepancies" section.

## How to read this document

- **MUST** / **MUST NOT**: mandatory. A violation blocks merge.
- **SHOULD** / **SHOULD NOT**: the default. Any deviation needs a written reason in the PR description.
- **MAY**: optional. Use your judgment.
- Adoption: rules that require per-project files or configuration (XU-110, XU-131) bind new test projects immediately and existing ones from the first PR that brings them under this guide. Each of those rules says so.
- Every rule has an ID (XU-NNN), a short rationale, and a source. If a rule's source says "Team policy", the rule is our own convention and not an xUnit.net requirement. Those rules still link the xUnit.net documentation that motivates them.
- These rules use the API names from xUnit.net v3 4.0.x. Some names changed in 4.0, mostly around parallelization. Where that matters, the rule says which version it applies to.

---

## 1. Team rules

**XU-001 (MUST)** RED tests are not acceptable. Test data is mocked to prove tests run green before production code is started.

- Rationale: The main branch and every pull request must always be green. When you design a test before the production code exists, first prove the test's structure, data, and assertions with mocked data or fakes, then write the production code. A failing test is never committed as a "placeholder".
- Source: Team policy. See also the xUnit.net guidance that skipping is temporary, not a parking lot: https://xunit.net/xunit.analyzers/rules/xUnit1004

**XU-002 (MUST)** Do not commit a test that fails, and do not commit a test that is skipped to hide a failure.

- Rationale: XU-001 applies to both cases. A skip that hides a failure is a red test in disguise.
- Source: Team policy; https://xunit.net/xunit.analyzers/rules/xUnit1004

**XU-003 (MUST)** AI coding agents MUST NOT do any of the following to make a build or test run pass unless a human approves it in the PR: lower an analyzer severity, add `NoWarn` or `#pragma warning disable`, add `Skip`, mark a test `Explicit`, delete a test, or weaken an assertion.

- Rationale: These actions hide defects instead of fixing them. Agents must fix the root cause or stop and report.
- Source: Team policy; https://xunit.net/xunit.analyzers/rules/xUnit1004

**XU-004 (MUST)** Run the full test suite before every PR is marked ready. For agents, that means running it before you report a task as done.

- Rationale: v3 test projects are executables, so `dotnet test` or `dotnet run` gives a fast local signal.
- Source: https://xunit.net/docs/getting-started/v3/getting-started

**XU-005 (MUST)** Test code follows the repository's documentation policy. In McpServer, all code must have XML documentation comments and all public APIs must be documented, and that includes test classes, test methods, fixtures, and helpers. Code samples in this document omit those comments for brevity, except the sample under XU-035, which shows the expected shape. Do not copy a sample without adding them.

- Rationale: Samples are trimmed to show the xUnit.net point being made. Without XML documentation, projects that generate a documentation file report CS1591 for public members.
- Source: Team policy (AGENTS.md, "Correctness"); https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/

---

## 2. Project structure and packages

**XU-010 (MUST)** Test projects MUST be SDK-style and target .NET 8 or later, or .NET Framework 4.7.2 or later.

- Rationale: These are the v3 minimum runtimes, and pre-SDK-style projects are not supported.
- Source: https://xunit.net/docs/getting-started/v3/migration

**XU-011 (MUST)** Test projects MUST set `<OutputType>Exe</OutputType>`.

- Rationale: v3 test projects are stand-alone executables. For .NET (not .NET Framework) projects, the tests live in the `.dll` and the `.exe` is only a stub launcher, so multi-assembly runners must be given the `.dll` path. The exception is a Native AOT project after `dotnet publish`: the produced executable is self-contained, and xUnit.net's first-party multi-assembly runners are given that executable as the test assembly.
- Source: https://xunit.net/docs/getting-started/v3/migration ; https://xunit.net/docs/getting-started/v3/native-aot

**XU-012 (MUST)** Each test project MUST reference exactly one core package. Choose one of:

- `xunit.v3` (the default MTP version, which is MTP v2 for 4.0.0 and later)
- `xunit.v3.mtp-v2` (MTP v2, chosen explicitly)
- `xunit.v3.mtp-off` (MTP support disabled)
- For a project that opts into Native AOT (4.0 or later, C# only, .NET 9 or later): the AOT variant of the same choice, `xunit.v3.aot`, `xunit.v3.aot.mtp-v2`, or `xunit.v3.aot.mtp-off`
- For a test project that references a shared test utility library built with `xunit.v3.assert.source` (XU-014): the matching core-only package (`xunit.v3.core`, `xunit.v3.core.mtp-v2`, or `xunit.v3.core.mtp-off`; for Native AOT, `xunit.v3.core.aot`, `xunit.v3.core.aot.mtp-v2`, or `xunit.v3.core.aot.mtp-off`) plus an explicit `xunit.analyzers` reference, instead of `xunit.v3` or `xunit.v3.aot`

Do not reference v2 packages (`xunit`, `xunit.abstractions`, `xunit.core`, `xunit.assert`, `xunit.console`).

- Rationale: `xunit.v3` brings in `xunit.v3.core`, `xunit.v3.assert`, and `xunit.analyzers`. Support for MTP v1 (`xunit.v3.mtp-v1`) was removed in 4.0.0. Native AOT projects use the `.aot` package variants, which xUnit.net lists side by side with the reflection packages.
- Source: https://xunit.net/docs/nuget-packages-v3 ; https://xunit.net/docs/getting-started/v3/microsoft-testing-platform ; https://xunit.net/releases/v3/4.0.0 ; https://xunit.net/docs/getting-started/v3/native-aot

**XU-013 (MUST NOT)** Do not reference `xunit.v3` or `xunit.v3.core` from a project that is not a test project. That includes shared test harness libraries. Shared libraries reference `xunit.v3.extensibility.core` (for framework types) and/or `xunit.v3.assert` or `xunit.v3.assert.source` (for assertions).

- Rationale: `xunit.v3.core` injects an entry point that turns the project into an executable.
- Source: https://xunit.net/docs/nuget-packages-v3

**XU-014 (SHOULD)** Put custom assertions in a shared "test utility" library that references `xunit.v3.assert.source`. Extend the partial `Assert` class there. Test projects that reference that library MUST NOT also receive `xunit.v3.assert`, so they use the core-only package from XU-012 instead of `xunit.v3`.

- Rationale: The source package exposes `Assert` as a partial class so you can add methods to it. This is the documented pattern for multi-project test suites. The source `Assert` class is `public` by default, and `xunit.v3` (through `xunit.v3.mtp-v2` in 4.x) depends on `xunit.v3.assert`, so a test project with both sees two public `Xunit.Assert` types and fails with CS0433. The core-only packages do not depend on `xunit.v3.assert` or `xunit.analyzers`, which is why the analyzers must be added explicitly. Verified on Oct 20, 2026 by building a 3.2.2 test project against such a utility library: with `xunit.v3` it failed with CS0433, and with `xunit.v3.core` it built.
- Source: https://xunit.net/docs/nuget-packages-v3 ; https://github.com/xunit/assert.xunit/blob/main/README.md ; https://api.nuget.org/v3-flatcontainer/xunit.v3.mtp-v2/4.0.2/xunit.v3.mtp-v2.nuspec ; https://api.nuget.org/v3-flatcontainer/xunit.v3.core.mtp-v2/4.0.2/xunit.v3.core.mtp-v2.nuspec

**XU-015 (MUST NOT)** Do not remove or disable `xunit.analyzers`.

- Rationale: The analyzers enforce most of this document at compile time (see section 13).
- Source: https://xunit.net/docs/nuget-packages-v3

**XU-016 (SHOULD)** Pin package versions centrally, for example with Central Package Management, and upgrade every test project together. The exception is XU-161.

- Rationale: 4.0 contained breaking changes, mostly in extensibility APIs, and third-party extensions may lag behind (see XU-161).
- Source: https://xunit.net/releases/v3/4.0.0

**XU-017 (SHOULD)** Create new test projects with the official template (`dotnet new install xunit.v3.templates`, then `dotnet new xunit3`). Then apply the rules in this document.

- Rationale: The template produces the correct `OutputType`, the package reference, `xunit.runner.json` wiring, and MTP settings.
- Source: https://xunit.net/docs/getting-started/v3/getting-started

Minimal project file (MTP v2, .NET 10 SDK, with `global.json` as shown in XU-020):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3.mtp-v2" Version="4.0.2" />
  </ItemGroup>
</Project>
```

---

## 3. Runner choice: Microsoft Testing Platform versus VSTest

**XU-020 (SHOULD)** Use Microsoft Testing Platform (MTP) for `dotnet test`.

- With .NET SDK 10 or later, put `{"test": {"runner": "Microsoft.Testing.Platform"}}` in the solution's `global.json`.
- With .NET SDK 8 or 9, set `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>` in the project.
- Rationale: xUnit.net v3 supports MTP natively, and the current template defaults to it.
- Source: https://xunit.net/docs/getting-started/v3/microsoft-testing-platform ; https://xunit.net/docs/getting-started/v3/getting-started

**XU-021 (MUST)** If any consumer still runs tests through VSTest (an older Visual Studio, or a CI task that only speaks VSTest), keep the `xunit.runner.visualstudio` (3.0.0 or later) and `Microsoft.NET.Test.Sdk` package references. If every consumer uses MTP, you MAY remove them. On xUnit.net 4.x with the .NET 10 SDK or later, a project that must still run under VSTest MUST also use the MTP-off core package (`xunit.v3.mtp-off`, or `xunit.v3.aot.mtp-off` for Native AOT) instead of the default `xunit.v3`, or move that consumer to MTP.

- Rationale: VSTest mode needs both packages. xUnit.net recommends keeping them until every environment uses MTP. In 4.0 the default package enables MTP v2, and xUnit.net offers the MTP-off packages for those who want to use VSTest on the .NET 10 SDK or later. Without them, a run can fail with "Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later".
- Source: https://xunit.net/docs/getting-started/v3/microsoft-testing-platform ; https://xunit.net/docs/getting-started/v3/migration ; https://xunit.net/releases/v3/4.0.0

**XU-022 (MUST)** Every test project's README or build script MUST say which command line UX it uses: native xUnit.net (the default) or MTP (`<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`). Scripts MUST use the switches for that UX.

- Rationale: The switches differ. For example, `-class` in the native UX is `--filter-class` in MTP, and `-trx` is `--report-xunit-trx`.
- Source: https://xunit.net/docs/getting-started/v3/microsoft-testing-platform

**XU-023 (SHOULD)** For quick local runs, use `dotnet run` (put runner options after `--`) or run the built executable directly.

- Rationale: There is no adapter overhead, and `-?` lists every switch.
- Source: https://xunit.net/docs/getting-started/v3/migration ; https://xunit.net/docs/getting-started/v3/getting-started

**XU-024 (MAY)** Use the console runner .NET tool (`dotnet tool install xunit-console-tool --global`, which requires .NET 10 SDK, then run `xunit-console`) to run several test assemblies in parallel. On Windows only, it MAY also mix v1/v2 assemblies with v3 ones.

- Rationale: A single v3 project does not need it. Custom reporters do not work through it. On Linux and macOS the runner supports only v3 .NET test projects, because v1 and v2 assemblies need .NET Framework, which is not supported on those operating systems.
- Source: https://xunit.net/docs/getting-started/v3/getting-started ; https://xunit.net/releases/v3/4.0.0

---

## 4. Test class and naming conventions

**XU-030 (MUST)** Test classes must be public (xUnit1000). They must not be nested inside a generic class (xUnit1032) and must not be open generic types (xUnit1063).

- Rationale: The framework cannot discover or construct them otherwise.
- Source: https://xunit.net/xunit.analyzers/rules/

**XU-031 (MUST NOT)** Do not overload test methods (xUnit1024). Do not make `[Fact]` methods generic (xUnit1061). Do not put more than one `[Fact]` or `[Theory]` on a method (xUnit1002).

- Rationale: These are framework errors. Overloads are reported at run time in reflection mode, and are not run at all in Native AOT mode.
- Source: https://xunit.net/xunit.analyzers/rules/ ; https://xunit.net/releases/v3/4.0.2

**XU-032 (MUST)** Helper methods in a test class must be `private` or `internal` (xUnit1013 flags public methods that are not tests).

- Rationale: A public helper looks like a test that someone forgot to mark.
- Source: https://xunit.net/xunit.analyzers/rules/

**XU-033 (SHOULD)** Name things as follows:

- Test class: `<TypeUnderTest>Tests`, in a namespace that mirrors the production namespace with a `.Tests` suffix.
- Test method: `Method_Scenario_ExpectedOutcome`. For example, `Adc_WithCarrySet_AddsOneExtra`.
- Async test methods MAY end in `Async`. If they do, the display option `removeAsyncSuffix` (v3 4.0 or later) MAY be enabled.
- Rationale: The default display name is `classAndMethod`, so these names produce readable reports. `methodDisplayOptions` can turn underscores into spaces (`replaceUnderscoreWithSpace`) if that reads better.
- Source: Team policy; https://xunit.net/docs/config-xunit-runner-json

**XU-034 (MAY)** Use nested classes to express starting contexts ("test class as context"). For example, `StackTests.EmptyStack` and `StackTests.SingleItemStack`.

- Rationale: xUnit.net documents this pattern for grouping tests by setup state.
- Source: https://xunit.net/docs/shared-context

**XU-035 (SHOULD)** Each test checks one behavior using Arrange, Act, Assert. Put constants and literals in the "expected" argument position (xUnit2000).

- Rationale: When a test fails, it is clear what went wrong. xUnit2000 catches swapped arguments that produce misleading failure messages.
- Source: Team policy; https://xunit.net/xunit.analyzers/rules/

```csharp
namespace Emulator.Cpu.Tests;

/// <summary>
/// Unit tests for <see cref="Alu"/> arithmetic.
/// </summary>
public class AluTests
{
    /// <summary>
    /// Verifies that ADC adds one extra when the carry flag is set.
    /// </summary>
    [Fact]
    public void Adc_WithCarrySet_AddsOneExtra()
    {
        var alu = new Alu { Carry = true };

        var result = alu.Adc(0x10, 0x01);

        Assert.Equal(0x12, result);
    }
}
```

---

## 5. Fact versus Theory, and theory data

**XU-040 (MUST)** Use `[Fact]` for invariants: tests that are always true. Use `[Theory]` for tests that are only true for a particular set of data.

- Rationale: These are xUnit.net's definitions. A theory produces one test per data row, and the runner shows which row failed.
- Source: https://xunit.net/docs/getting-started/v3/getting-started

**XU-041 (MUST)** Signature rules:

- Facts take no parameters (xUnit1001) and carry no data (xUnit1005).
- Theories must have data (xUnit1003), must have parameters (xUnit1006), and should use every parameter (xUnit1026).
- Theories MAY use `params` arrays and default parameter values. xUnit1022 and xUnit1023 only flag those on xUnit.net versions before 2.2.0, so they do not restrict v3 projects.
- Rationale: The analyzers enforce these rules. Most of them are errors by default.
- Source: https://xunit.net/xunit.analyzers/rules/ ; https://xunit.net/xunit.analyzers/rules/xUnit1022 ; https://xunit.net/xunit.analyzers/rules/xUnit1023

**XU-042 (SHOULD)** New data sources should be a public static member that returns `IEnumerable<TheoryDataRow<...>>`, referenced with `[MemberData(nameof(...))]`.

- Rationale: xUnit.net calls this the preferred signature. It is type safe, supports per-row metadata, and is checked by the analyzers. The theory data page says `TheoryData<...>` and `IEnumerable<object?[]>` "should be considered to be deprecated and only supported for backward compatibility reasons".
- Source: https://xunit.net/docs/getting-started/v3/theory-data-attributes

```csharp
public static IEnumerable<TheoryDataRow<byte, byte, byte>> AddCases =>
[
    new(0x01, 0x01, 0x02),
    new(0xFF, 0x01, 0x00) { Label = "wraps" },
];

[Theory]
[MemberData(nameof(AddCases))]
public void Add_ReturnsWrappedSum(byte a, byte b, byte expected) =>
    Assert.Equal(expected, new Alu().Add(a, b));
```

**XU-043 (MAY)** Existing code MAY keep using `TheoryData<T1, ...>`. Do not wrap types that implement `ITheoryDataRow` in `TheoryData<...>` (xUnit1052).

- Rationale: It is still supported and still type checked (xUnit1037 to xUnit1040). It can also carry per-row metadata: `TheoryData<T>` and its multi-argument forms accept typed rows through `Add(TheoryDataRow<...>)`, `AddRange`, and the constructors. xUnit1052 is about a different case, a `TheoryData<...>` whose type argument is itself a row type, such as `TheoryData<TheoryDataRow<int>>`.
- Source: https://xunit.net/docs/getting-started/v3/theory-data-attributes ; https://xunit.net/xunit.analyzers/rules/ ; https://api.xunit.net/v3/3.2.2/Xunit.TheoryData-1.html ; https://xunit.net/xunit.analyzers/rules/xUnit1052

```csharp
public static TheoryData<int, bool> OddCases = new() { { 3, true }, { 6, false } };
```

**XU-044 (MUST)** Rules for `MemberData`:

- Reference the member with `nameof` (xUnit1014).
- The member must exist (xUnit1015), be public (xUnit1016) and static (xUnit1017), and be statically initialized (xUnit1053).
- Use `MemberType = typeof(...)` to share data across classes.
- Rationale: Errors are caught at compile time, and refactoring stays safe.
- Source: https://xunit.net/xunit.analyzers/rules/ ; https://xunit.net/docs/getting-started/v3/theory-data-attributes

**XU-045 (SHOULD)** Use `[InlineData]` only for small sets of constants. Rows must be unique (xUnit1025). Use `MemberData` when you need non-constant values, `decimal` values, or data shared between tests.

- Rationale: Attribute arguments only accept a limited set of types, and copying the same data onto several tests drifts out of sync.
- Source: https://xunit.net/docs/getting-started/v3/theory-data-attributes

**XU-046 (SHOULD)** Prefer `MemberData` to `ClassData`. If you do use `ClassData`, the class should implement `IEnumerable<TheoryDataRow<...>>`. Untyped rows are flagged by xUnit1050 (ClassData) and xUnit1042 (MemberData).

- Rationale: xUnit.net calls `ClassData` "a more complex and less convenient alternative".
- Source: https://xunit.net/docs/getting-started/v3/theory-data-attributes

```csharp
public sealed class OpcodeCases : IEnumerable<TheoryDataRow<byte, string>>
{
    public IEnumerator<TheoryDataRow<byte, string>> GetEnumerator()
    {
        yield return new(0xEA, "NOP");
        yield return new(0x00, "BRK");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

**XU-047 (SHOULD)** In reflection mode, theory data should be serializable, so each row can be listed and run on its own in Test Explorer. This rule does not apply to Native AOT projects, because xUnit.net lists serialization as not available in AOT mode (see section 17 for what is not verified about AOT discovery). Serializable means any of:

- the built-in intrinsics and system types (for example `int`, `string`, `byte`, `DateTime`, `Guid`, `Uri`, `Version`, `Type`)
- enums, single-dimensional arrays of serializable values, and tuples (v3 3.0.1 or later)
- types that implement both `IFormattable` and `IParsable<TSelf>` (v3 1.1.0 or later)
- types that implement `IXunitSerializable` (namespace `Xunit.Sdk` in v3)
- types covered by an `IXunitSerializer` that you register with `[assembly: RegisterXunitSerializer(typeof(MySerializer), typeof(MyType))]`

- Rationale: If any row is not serializable, discovery falls back to a single test for the whole method. xUnit1044 to xUnit1047 warn about this.
- Source: https://github.com/xunit/xunit.net/blob/main/site/docs/getting-started/v3/custom-serialization.md ; https://xunit.net/xunit.analyzers/rules/xUnit1044 ; https://xunit.net/docs/getting-started/v3/theory-data-attributes ; https://xunit.net/docs/getting-started/v3/native-aot

**XU-048 (MAY)** Use `[Theory(DisableDiscoveryEnumeration = true)]` for data sets that are large or expensive to build.

- Rationale: It stops the data from being enumerated during discovery.
- Source: https://xunit.net/docs/getting-started/v3/theory-data-attributes

**XU-049 (MAY)** Use `MatrixTheoryData` to combine 2 to 5 data sets into every combination.

- Rationale: It builds combinatorial cases (for example opcode by flag state) without writing every row by hand.
- Source: https://xunit.net/docs/getting-started/v3/whats-new

```csharp
public static TheoryData<byte, bool> OpcodeByCarry =
    new MatrixTheoryData<byte, bool>([0x69, 0xE9], [true, false]);
```

**XU-050 (SHOULD)** Put per-row metadata (`Skip`, `Explicit`, `Label`, `Timeout`, `Traits`, `TestDisplayName`, and, in v3 4.0 or later only, `DisableParallelization`) on the `TheoryDataRow` or the `InlineData` row, not on the whole theory.

- Rationale: The setting then affects only the row that needs it. `DisableParallelization` does not exist on data rows or data attributes in 3.2.x; it arrived with the 4.0 parallelization changes (XU-084).
- Source: https://xunit.net/docs/getting-started/v3/theory-data-attributes ; https://xunit.net/releases/v3/4.0.0 ; https://github.com/xunit/xunit/blob/v3-4.0.2/src/xunit.v3.core/ITheoryDataRow.cs

**XU-051 (MAY)** `MemberData` methods MAY be async and return `Task<>` or `ValueTask<>`. They MUST NOT fetch data from the network or other external systems (see XU-095).

- Rationale: v3 supports async data, but data from outside the test makes tests non-deterministic.
- Source: https://xunit.net/docs/getting-started/v3/whats-new

---

## 6. Fixtures and lifetimes

Pick the narrowest lifetime that works, from narrowest to widest: per test (constructor), then class fixture, then collection fixture, then assembly fixture, then pipeline startup.

**XU-060 (MUST)** The default is per-test setup in the test class constructor, with cleanup in `IDisposable.Dispose` or `IAsyncDisposable.DisposeAsync`. Use `IAsyncLifetime` when setup is async.

- Rationale: xUnit.net creates a new instance of the test class for every test, so each test starts from a clean context.
- Source: https://xunit.net/docs/shared-context

```csharp
public sealed class CpuTests : IAsyncLifetime
{
    private Cpu cpu = null!;

    public async ValueTask InitializeAsync() =>
        cpu = await Cpu.CreateAsync(new FakeBus(), TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => cpu.DisposeAsync();
}
```

**XU-061 (MUST)** Get async lifetimes right in v3:

- `InitializeAsync` and `DisposeAsync` return `ValueTask`.
- `IAsyncLifetime` inherits from `IAsyncDisposable`.
- If an object implements both `IAsyncDisposable` and `IDisposable`, only `DisposeAsync` is called.
- If `InitializeAsync` throws, `DisposeAsync` is not called. Clean up any partly created resources inside `InitializeAsync` before rethrowing.
- Rationale: These behaviors changed from v2, and the last one is documented in the 4.0.2 release notes.
- Source: https://xunit.net/docs/getting-started/v3/migration ; https://xunit.net/releases/v3/4.0.2

**XU-062 (SHOULD)** Use a class fixture (`IClassFixture<T>`) when setup is expensive and can be shared by all tests in one class. Inject it through the constructor. xUnit1033 suggests adding the constructor argument.

- Rationale: The fixture is created before the class's first test and cleaned up after its last test.
- Source: https://xunit.net/docs/shared-context

**XU-063 (SHOULD)** Use a collection fixture when the context must be shared across several test classes.

- Mark the definition class `[CollectionDefinition]` and make it public (xUnit1027).
- Put `ICollectionFixture<T>` on the definition class, never on a test class (xUnit1059).
- Keep the definition in the same assembly as the tests.
- Point test classes at the collection by type: `[Collection(typeof(EmulatorCollection))]`, or `[Collection<EmulatorCollection>]` on .NET 8 or later.
- Rationale: Referring to the collection by type avoids string typos. Collection definitions do not work across assemblies.
- Source: https://xunit.net/docs/shared-context ; https://xunit.net/docs/getting-started/v3/whats-new ; https://xunit.net/xunit.analyzers/rules/

```csharp
[CollectionDefinition]
public sealed class RomCollection : ICollectionFixture<RomImageFixture>;

[Collection<RomCollection>]
public sealed class BootTests(RomImageFixture rom)
{
    [Fact]
    public void Reset_LoadsVector() => Assert.Equal(0xFCE2, rom.ResetVector);
}
```

**XU-064 (MAY)** Use an assembly fixture (`[assembly: AssemblyFixture(typeof(T))]`, or `AssemblyFixture<T>` on .NET 8 or later with v3 4.0) for expensive resources that the whole process shares. It MUST be thread safe.

- Rationale: An assembly fixture does not change parallelization, so several tests can use it at the same moment.
- Source: https://xunit.net/docs/shared-context ; https://xunit.net/releases/v3/4.0.0

**XU-065 (MUST)** A shared fixture (class, collection, or assembly) MUST NOT hold mutable state that tests change, with two exceptions:

- The fixture gives each test its own isolated state (for example, a new instance or scope per test).
- The fixture resets that state before each test, and every consumer of the fixture runs serially. That holds for class and collection fixtures under the default `parallelMode` `collections`. It does not hold for assembly fixtures (XU-064), or for any fixture under `parallelMode` `all` (XU-084), because those can be used by several tests at the same moment. In v3 4.0 or later, the fixture SHOULD do the reset by implementing `INotifyTestLifecycle` or `INotifyTestLifecycleAsync`.

- Rationale: Leftover state makes tests depend on run order. The 4.0 lifecycle notification interfaces exist for resetting data before each test, but a reset while another test is still using the fixture would corrupt that test, so resetting is only safe when consumers are serialized.
- Source: https://xunit.net/releases/v3/4.0.0 ; https://xunit.net/docs/shared-context ; https://xunit.net/docs/running-tests-in-parallel

**XU-066 (MUST)** Every fixture constructor argument must come from a declared fixture (xUnit1041). A fixture MAY take a fixture with a longer lifetime as a constructor argument: a class fixture MAY receive assembly and/or collection fixtures, and a collection fixture MAY receive assembly fixtures. A fixture MUST NOT take a fixture of the same or a shorter lifetime (for example, one class fixture taking another class fixture). If you need that, write one fixture that builds and owns the others.

- Rationale: The `IClassFixture<T>` and `ICollectionFixture<T>` API contracts explicitly allow these parent-lifetime constructor arguments. xUnit.net does not control the creation order of fixtures at the same level, so same-level dependencies are unsupported.
- Source: https://api.xunit.net/v3/4.0.0/Xunit.IClassFixture-1.html ; https://api.xunit.net/v3/4.0.0/Xunit.ICollectionFixture-1.html ; https://xunit.net/docs/shared-context ; https://xunit.net/xunit.analyzers/rules/

**XU-067 (MAY)** Implement `ITestPipelineStartup` (registered with `[assembly: TestPipelineStartup(typeof(T))]`) only for global setup that both discovery and execution need. The interface has `ValueTask StartAsync(IMessageSink diagnosticMessageSink)` and `ValueTask StopAsync()`. Only one startup class is allowed. Use fixtures for anything that only execution needs.

- Rationale: The startup class runs very early, including during discovery. If `StartAsync` fails, the runner exits without discovering or running anything.
- Source: https://xunit.net/docs/getting-started/v3/whats-new ; https://github.com/xunit/xunit/blob/main/src/xunit.v3.core/Abstractions/Framework/ITestPipelineStartup.cs

---

## 7. Async and cancellation

**XU-070 (MUST)** Async tests return `Task` or `ValueTask`. Never use `async void` (xUnit1048 warns, xUnit1049 errors).

- Rationale: v3 fails `async void` tests immediately at run time.
- Source: https://xunit.net/docs/getting-started/v3/migration ; https://xunit.net/xunit.analyzers/rules/

**XU-071 (MUST)** Pass `TestContext.Current.CancellationToken`, or a linked token that includes it, to every call that accepts a `CancellationToken` (xUnit1051).

- Rationale: The token fires on timeouts and when the user cancels the run, so cancellation is orderly.
- Source: https://xunit.net/xunit.analyzers/rules/xUnit1051 ; https://xunit.net/docs/getting-started/v3/whats-new

```csharp
[Fact]
public async Task ListTools_ReturnsRegisteredTools()
{
    var ct = TestContext.Current.CancellationToken;
    var server = new FakeMcpServerHost();

    var tools = await server.ListToolsAsync(ct);

    Assert.Contains(tools, t => t.Name == "disassemble");
}
```

**XU-072 (MUST NOT)** Do not block on tasks inside a test method. That means no `.Wait()`, no `.Result`, and nothing similar (xUnit1031). Make the test async and `await`.

- Rationale: Tests run on a limited pool of threads, so blocking can cause deadlocks.
- Source: https://xunit.net/xunit.analyzers/rules/xUnit1031

**XU-073 (MUST NOT)** Do not call `ConfigureAwait(false)` in test methods (xUnit1030). Turn off CA2007 in test projects. The analyzers package includes a CA2007 suppressor for test methods.

- Rationale: `ConfigureAwait(false)` moves the rest of the test onto the general thread pool, so xUnit.net can no longer limit how many tests run at once.
- Source: https://xunit.net/xunit.analyzers/rules/xUnit1030 ; https://github.com/xunit/xunit.analyzers/blob/main/src/xunit.analyzers/Utility/Descriptors.Suppressors.cs

**XU-074 (MUST)** Always await async assertions (xUnit2021). Use `Assert.ThrowsAsync` for exceptions thrown asynchronously (xUnit2014).

- Rationale: An assertion that is not awaited can pass when it should fail. Both rules are errors by default.
- Source: https://xunit.net/xunit.analyzers/rules/

**XU-075 (SHOULD)** Integration-style tests that could hang should set `Timeout` (in milliseconds) on `[Fact]` or `[Theory]`. Any test with a `Timeout` MUST consume the cancellation token (xUnit1069).

- Rationale: In v3, a timeout does not stop a test by force. It only signals the token. Timeouts are accurate under the default `conservative` algorithm, and they work for synchronous tests too.
- Source: https://xunit.net/xunit.analyzers/rules/xUnit1069 ; https://xunit.net/docs/getting-started/v3/whats-new ; https://xunit.net/docs/running-tests-in-parallel

**XU-076 (MUST NOT)** Do not store `TestContext.Current` (or `ITestContextAccessor.Current`) in a field to use later. Read it at the moment you need it.

- Rationale: It is a "moment in time" snapshot of the pipeline state.
- Source: https://xunit.net/docs/getting-started/v3/whats-new

---

## 8. Parallelization and collections

**XU-080 (MUST)** Write every test assuming that tests in other collections run at the same time.

- Rationale: The default mode is `collections`, with one collection per test class. Tests within a class run one after another. Different classes run in parallel.
- Source: https://xunit.net/docs/running-tests-in-parallel

**XU-081 (MUST)** If test classes share an external resource that is not thread safe (a file, a port, a process-wide singleton), put them in the same collection. If that resource must not run alongside anything else, define the collection with `[CollectionDefinition(DisableParallelization = true)]`.

- Rationale: Tests in the same collection run sequentially. A collection with parallelization disabled never runs at the same time as any other test.
- Source: https://xunit.net/docs/running-tests-in-parallel

**XU-082 (SHOULD)** Keep the default `conservative` parallel algorithm. Switch to `aggressive` only if measurements show a significant slowdown and no test relies on `Timeout`.

- Rationale: `conservative` keeps timeouts accurate and puts less pressure on the task system. `aggressive` can make timing inaccurate.
- Source: https://xunit.net/docs/running-tests-in-parallel

**XU-083 (MUST)** In v3 4.0 or later, set assembly-level parallelism with `[assembly: Parallelization(Mode = ..., MaxThreads = ..., Algorithm = ...)]` or with configuration (`parallelMode`, `maxParallelThreads`, `parallelAlgorithm`). The old `CollectionBehavior` properties (`DisableTestParallelization`, `MaxParallelThreads`, `ParallelAlgorithm`) are obsolete and cannot be called.

- Rationale: The 4.0 release notes make these properties obsolete.
- Source: https://xunit.net/releases/v3/4.0.0 ; https://xunit.net/docs/running-tests-in-parallel

```csharp
[assembly: Parallelization(Mode = ParallelMode.Collections, Algorithm = ParallelAlgorithm.Conservative)]
```

**XU-084 (MAY)** A project MAY use `parallelMode` `all` (v3 4.0 or later) only if every test in it is fully isolated. Opt individual items out with `DisableParallelization = true`, which exists on `[TestClass]`, `[Fact]`, `[Theory]`, `[InlineData]`, `TheoryDataRow`, and `[CollectionDefinition]`. Once something is opted out, nothing below it can opt back in.

- Rationale: In `all` mode, tests run in parallel even within a single class. See the unverified items section for a naming discrepancy in the docs.
- Source: https://xunit.net/docs/running-tests-in-parallel ; https://xunit.net/releases/v3/4.0.0 ; https://github.com/xunit/xunit/blob/v3-4.0.2/src/xunit.v3.core/FactAttribute_reflection.cs

**XU-085 (MUST NOT)** Do not turn off parallelization for a whole assembly to "fix" a flaky test without finding and recording the root cause.

- Rationale: That hides shared-state bugs and slows every run. The right fix is a collection or isolation (section 9).
- Source: Team policy; https://xunit.net/docs/running-tests-in-parallel

**XU-086 (MUST NOT)** Tests must not depend on execution order. Do not write orderers that make one test depend on another.

- Rationale: xUnit.net randomizes order in a stable way, based on the test assembly's unique ID. That ID comes from the assembly's file path, so CI and local runs use different orders.
- Source: https://xunit.net/docs/getting-started/v3/whats-new

**XU-087 (SHOULD)** In reflection mode, to reproduce a failure that depends on order, copy the assembly unique ID from the run output into a `<AssemblyName>.uniqueid` file next to the test assembly (v3 3.0.1 or later). This does not work for Native AOT projects.

- Rationale: The same ID reproduces the same order. xUnit.net lists the `.uniqueid` override file as a feature that is not available in AOT mode, so an AOT project has no documented way to replay an order this way.
- Source: https://xunit.net/docs/getting-started/v3/whats-new ; https://xunit.net/docs/getting-started/v3/native-aot

---

## 9. Isolation and determinism

**XU-090 (MUST NOT)** Tests must not share mutable static state. That includes static caches, singletons, and environment variables changed during a test.

- Rationale: Collections run in parallel by default (XU-080), so shared mutable state causes failures that depend on timing and order.
- Source: https://xunit.net/docs/running-tests-in-parallel ; https://xunit.net/docs/shared-context

**XU-091 (MUST)** Production code that reads the clock or uses timers MUST take a `System.TimeProvider`. Tests pass `FakeTimeProvider` (from the `Microsoft.Extensions.TimeProvider.Testing` package) or a custom `TimeProvider` subclass. Tests never read `DateTime.Now` or `DateTimeOffset.UtcNow`.

- Rationale: `TimeProvider` makes time testable and predictable. `FakeTimeProvider` can set the time and advance it manually.
- Source: https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview

```csharp
var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
var vsync = new VsyncTimer(time);

time.Advance(TimeSpan.FromMilliseconds(16));

Assert.Equal(1, vsync.FrameCount);
```

**XU-092 (MUST NOT)** Do not use `Thread.Sleep` or a real `Task.Delay` to wait for something to happen. Advance a fake `TimeProvider`, or await a `TaskCompletionSource` that the code under test completes.

- Rationale: Sleeping is slow and flaky under parallel load. It also blocks the limited test threads (see XU-072).
- Source: Team policy; https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview ; https://xunit.net/xunit.analyzers/rules/xUnit1031

**XU-093 (MUST)** Randomness must be injected and seeded. Production code takes a `Random` instance (or a seed). Tests pass a fixed seed and write the seed to `ITestOutputHelper` whenever it is generated.

- Rationale: A failing case can then be replayed exactly. xUnit.net itself moved to stable, repeatable test ordering for the same reason.
- Source: Team policy; https://xunit.net/docs/getting-started/v3/whats-new

**XU-094 (MUST)** Set the culture explicitly in `xunit.runner.json` (`"culture": "invariant"`, or a named culture). Tests that check behavior in specific cultures cover each culture explicitly. See the "Cultured tests" item in section 17 for why this guide does not prescribe `CulturedFact` or `CulturedTheory`.

- Rationale: The default is the machine's culture, so number and date formatting differ between developer machines and CI.
- Source: https://xunit.net/docs/config-xunit-runner-json ; https://xunit.net/docs/getting-started/v3/whats-new

**XU-095 (MUST)** Unit tests MUST mock or fake external I/O (network, databases, real devices, files outside a temp folder unique to the test). Integration tests that really need such a dependency MUST be marked `Explicit` or otherwise kept out of the required validation scope (XU-121), and SHOULD use fixtures to manage the resource.

- Rationale: This is how XU-001 works in practice. Mocked data proves a test is green before the production code is written, and keeps it green whatever the environment.
- Source: Team policy; https://xunit.net/docs/shared-context

**XU-096 (MUST)** Tests that touch the file system MUST use a temporary folder unique to the test, and delete it during cleanup.

- Rationale: Tests in different collections run at the same time (XU-080).
- Source: Team policy; https://xunit.net/docs/running-tests-in-parallel

**XU-097 (SHOULD)** Do not leave threads running after a test ends.

- Rationale: At shutdown, the test process waits `shutdownForegroundThreadWaitSeconds` (default 10, v3 4.0 or later) for foreground threads, then returns failure. Output written from threads that are not tied to a test is discarded.
- Source: https://xunit.net/docs/config-xunit-runner-json ; https://xunit.net/docs/capturing-output

**XU-098 (MUST NOT)** Do not call `Assert.OverrideMaxEnumerableLength`, `Assert.OverrideMaxObjectDepth`, `Assert.OverrideMaxObjectMemberCount`, or `Assert.OverrideMaxStringLength` outside a test method.

- Rationale: The overrides are `AsyncLocal`. Outside a test they may affect an unknown set of tests.
- Source: https://xunit.net/releases/v3/4.0.0

---

## 10. Output and diagnostics

**XU-100 (SHOULD)** Write test diagnostics to `ITestOutputHelper`, which lives in the `Xunit` namespace in v3. Inject it through the constructor, or read `TestContext.Current.TestOutputHelper`.

- Rationale: Output is attached to the specific test and appears in reports. `Write` output is buffered until a newline.
- Source: https://xunit.net/docs/capturing-output ; https://xunit.net/docs/getting-started/v3/migration ; https://github.com/xunit/xunit/blob/main/src/xunit.v3.core/ITestContext.cs

```csharp
public sealed class DisassemblerTests(ITestOutputHelper output)
{
    [Fact]
    public void Disassemble_Nop()
    {
        var text = Disassembler.Decode([0xEA]);
        output.WriteLine($"Decoded: {text}");
        Assert.Equal("NOP", text);
    }
}
```

**XU-101 (MAY)** For legacy code that writes to the console, `[assembly: CaptureConsole]` and/or `[assembly: CaptureTrace]` MAY be enabled for the whole assembly.

- Rationale: Both are off by default. Output from threads not tied to a test is discarded. `Debug` output is only captured in Debug builds.
- Source: https://xunit.net/docs/capturing-output

**XU-102 (SHOULD)** Attach large artifacts with `TestContext.Current.AddAttachment` instead of writing them as output. Examples: emulator memory dumps, frame buffers, screenshots, MCP transcripts.

- Rationale: Attachments are named, can hold text or binary content (with a MIME type), and are recorded in the report formats.
- Source: https://xunit.net/docs/getting-started/v3/whats-new

**XU-103 (MAY)** Use `TestContext.Current.AddWarning` to report something suspicious that is not a failure. CI MAY set `failWarns` to `true`.

- Rationale: Warnings are reported on passing tests. `failWarns` turns them into failures.
- Source: https://xunit.net/docs/getting-started/v3/whats-new ; https://xunit.net/docs/config-xunit-runner-json

**XU-104 (SHOULD)** Fixtures and extensions should report through `TestContext.Current.SendDiagnosticMessage`. Turn on `diagnosticMessages` to see those messages.

- Rationale: Diagnostic messages are shown separately from test output, and only when enabled.
- Source: https://xunit.net/docs/capturing-output ; https://xunit.net/docs/config-xunit-runner-json

**XU-105 (MAY)** Enable `showLiveOutput` temporarily while debugging. Do not commit it as `true`.

- Rationale: It is off by default, and xUnit.net expects people to turn it on only while debugging.
- Source: https://xunit.net/docs/capturing-output

---

## 11. Configuration

**XU-110 (MUST)** Each test project MUST have an `xunit.runner.json` at its root. New test projects comply from the start. An existing project adopts the file, with the team baseline below, in the first PR that brings it under this guide; until then it is not in violation. In McpServer, none of the test projects that reference `xunit.v3` had an `xunit.runner.json` on Oct 20, 2026, so this rollout is still to do. The file includes `"$schema": "https://xunit.net/schema/current/xunit.runner.schema.json"` and is included as `Content` with `CopyToOutputDirectory="PreserveNewest"`.

- Rationale: Settings stay in source control, and editors get autocomplete from the schema.
- Source: https://xunit.net/docs/config-xunit-runner-json

**XU-111 (SHOULD)** Use `xunit.runner.json` as the single source of xUnit.net settings. Use `testconfig.json` (under an `xUnit` object) only for settings specific to MTP, or for projects that only ever run under MTP.

- Rationale: `testconfig.json` is only read in MTP mode. First-party runners and VSTest ignore it.
- Source: https://xunit.net/docs/config-testconfig-json

**XU-112 (MUST)** Know these precedence rules:

- `<AssemblyName>.xunit.runner.json` takes precedence over `xunit.runner.json`, and the two are not merged.
- Runner command line options override code and configuration.
- Rationale: Otherwise people are surprised when a setting appears to be ignored.
- Source: https://xunit.net/docs/config-xunit-runner-json ; https://xunit.net/docs/running-tests-in-parallel

**XU-113 (SHOULD NOT)** Do not set `maxParallelThreads` to `-1` or `"unlimited"`.

- Rationale: The parallel algorithm only applies when the thread count is limited, so timeouts and thread pressure become unpredictable. A multiplier such as `"1x"` or `"2x"` is the supported way to scale with the machine's CPU count.
- Source: https://xunit.net/docs/config-xunit-runner-json ; https://xunit.net/docs/running-tests-in-parallel

**XU-114 (MUST)** Know these verified defaults before overriding them:

- `parallelMode` `"collections"` (v3 4.0 or later)
- `parallelizeTestCollections` `true`
- `parallelAlgorithm` `"conservative"`
- `maxParallelThreads` = the number of CPU threads
- `culture` `"default"`
- `methodDisplay` `"classAndMethod"`
- `methodDisplayOptions` `"none"`
- `diagnosticMessages` `false`
- `longRunningTestSeconds` `0` (disabled)
- `failSkips` `false`
- `failWarns` `false`
- `stopOnFail` `false`
- `showLiveOutput` `false`
- `shutdownForegroundThreadWaitSeconds` `10`
- `printMaxEnumerableLength` `5`
- `printMaxStringLength` `50`
- `printMaxObjectDepth` `3`
- `printMaxObjectMemberCount` `5`
- `assertEquivalentMaxDepth` `50`
- `preEnumerateTheories` has no framework-wide default; each runner decides.
- Rationale: Do not override a value only to "set" it to the default.
- Source: https://xunit.net/docs/config-xunit-runner-json

Team baseline `xunit.runner.json`:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "culture": "invariant",
  "parallelMode": "collections",
  "parallelizeTestCollections": true,
  "diagnosticMessages": true,
  "longRunningTestSeconds": 60
}
```

`diagnosticMessages` is `true` because long-running test warnings are diagnostic messages and only appear when diagnostics are enabled (XU-143).

`parallelizeTestCollections` is kept in the baseline so that older v3 projects (before 4.0), such as the Avalonia project pinned in XU-161, read the same intent. xUnit.net documents that setting both is safe.

---

## 12. Skip and explicit tests

**XU-120 (MUST NOT)** Do not commit a test with a static `Skip = "..."` unless it links to a work item and a human approved it in the PR. xUnit1004 is raised to error (section 13), so the skip also needs a reviewed suppression.

- Rationale: xUnit.net says skipping should be a temporary mechanism. Under XU-001, a skip that hides a failure is a red test.
- Source: https://xunit.net/xunit.analyzers/rules/xUnit1004

**XU-121 (MUST)** A test that depends on the environment (operating system, missing hardware, licensed ROM images that CI does not have) MUST NOT be in the required validation scope unless that scope provides the dependency. The validation gate requires zero failures and zero skips in the executed scope (XU-140), so a skip inside that scope fails the gate. Either provide the dependency on the gate's agents, or keep the test out of the gate: mark it `Explicit = true` (XU-123), or tag it with a trait and exclude that trait from the gate run (`--filter-not-trait "name=value"` in MTP, `-trait- "name=value"` in the native UX).

Outside the required validation scope (for example a scheduled hardware job or a local run), such a test SHOULD use `SkipUnless` or `SkipWhen` together with `Skip` (the reason), and `SkipType` if the property lives on another class.

- The property must be `public static bool` (xUnit1054). A method such as `OperatingSystem.IsWindows()` cannot be named directly; wrap it in a property.
- Never set both `SkipUnless` and `SkipWhen` (xUnit1055).
- Rationale: The repository gate treats skipped tests as not passing. Conditional skips are evaluated at run time and do not trigger xUnit1004, so they are only safe where a skip is an expected outcome.
- Source: Team policy (AGENTS.md, "Byrd Test Gate"); https://xunit.net/docs/getting-started/v3/whats-new ; https://xunit.net/xunit.analyzers/rules/xUnit1004 ; https://xunit.net/xunit.analyzers/rules/xUnit1054 ; https://xunit.net/docs/getting-started/v3/microsoft-testing-platform

```csharp
public static class TestEnvironment
{
    public static bool IsWindows => OperatingSystem.IsWindows();
}

[Fact(Skip = "Requires Windows", SkipUnless = nameof(TestEnvironment.IsWindows), SkipType = typeof(TestEnvironment), Explicit = true)]
public void NativeAudioBackend_Initializes() { /* ... */ }
```

**XU-122 (MAY)** Outside the required validation scope (XU-121), when the condition can only be checked inside the test, skip at run time with `Assert.Skip(reason)`, `Assert.SkipUnless(condition, reason)`, or `Assert.SkipWhen(condition, reason)`. The test itself MUST still be kept out of the gate run as XU-121 describes.

- Rationale: This is the documented way to skip dynamically from within the test body. Inside the gate, the resulting skip would fail validation.
- Source: https://xunit.net/docs/getting-started/v3/whats-new

```csharp
[Fact(Explicit = true)]
public void FullRomSuite_Passes()
{
    var path = Environment.GetEnvironmentVariable("EMU_TEST_ROMS");
    Assert.SkipWhen(path is null, "EMU_TEST_ROMS not set");
    // ...
}
```

**XU-123 (SHOULD)** Mark expensive, manual, or long-running tests `Explicit = true` instead of skipping them. Run them with `-explicit only` (native UX), `--explicit only` (MTP), or `"explicit": "only"` in `testconfig.json`. The values are `on`, `off`, and `only`, and the default is `off`.

- Rationale: Explicit tests show as "not run" unless someone asks for them, which states the intent more accurately than a skip.
- Source: https://xunit.net/docs/getting-started/v3/whats-new ; https://xunit.net/docs/config-testconfig-json ; https://xunit.net/docs/getting-started/v3/microsoft-testing-platform

**XU-124 (MAY)** Outside the required validation scope (XU-121), theories whose data can legitimately be empty in some environments MAY set `SkipTestWithoutData = true` on `[Theory]`. Theories in the gate run MUST provide stable, non-empty data instead.

- Rationale: An empty theory is then skipped instead of failing, and a skip inside the gate fails validation (XU-140, XU-147).
- Source: https://xunit.net/docs/getting-started/v3/whats-new

---

## 13. Analyzer enforcement

### 13.1 Rule groups and default severities

The defaults below come from the xunit.analyzers rules page and `Descriptors.xUnit1xxx.cs` / `Descriptors.xUnit2xxx.cs` in the `xunit/xunit.analyzers` repository (analyzers 2.2.0). E means error, W warning, I info, H hidden.

Test structure and discovery:

- xUnit1000 (E) Test classes must be public
- xUnit1001 (E) Fact methods cannot have parameters
- xUnit1002 (E) Test methods cannot have multiple Fact or Theory attributes
- xUnit1003 (E) Theory methods must have test data
- xUnit1005 (W) Fact methods should not have test data
- xUnit1006 (W) Theory methods should have parameters
- xUnit1008 (W) Test data attribute should only be used on a Theory
- xUnit1013 (W) Public method should be marked as test
- xUnit1024 (E) Test methods cannot have overloads
- xUnit1028 (E) Test method must have valid return type
- xUnit1029 (E) Local functions cannot be test functions
- xUnit1032 (E) Test classes cannot be nested within a generic class
- xUnit1043 (E) Constructors on classes derived from FactAttribute must be public when used on test methods
- xUnit1061 (E) Fact methods cannot be generic
- xUnit1063 (E) Test class cannot be an open generic type

Theory data correctness:

- xUnit1007 (E) ClassData must point at a valid class
- xUnit1009 (E) InlineData values must match the number of method parameters
- xUnit1010 (E) and xUnit1035 (E) The value is not convertible to the method parameter type
- xUnit1011 (E) and xUnit1036 (E) There is no matching method parameter
- xUnit1012 (W) and xUnit1034 (W) Null usage for non-nullable parameters
- xUnit1014 (W) MemberData should use nameof operator for member name
- xUnit1015 to xUnit1020 (E) MemberData must reference an existing, public, static member of a valid kind and data type, with a getter
- xUnit1021 (W) MemberData should not have parameters if the referenced member is not a method
- xUnit1022 (E) Theory methods cannot have a parameter array (applies only before xUnit.net 2.2.0)
- xUnit1023 (E) Theory methods cannot have default parameter values (applies only before xUnit.net 2.2.0)
- xUnit1025 (W) InlineData should be unique within the Theory it belongs to
- xUnit1026 (W) Theory methods should use all of their parameters
- xUnit1037, xUnit1038, xUnit1039 (E) Theory data type arguments must match the test method parameters
- xUnit1040 (W) Theory data type argument is nullable while the parameter is not
- xUnit1042 (I) MemberData returns untyped data rows
- xUnit1050 (I) ClassData returns untyped data rows
- xUnit1052 (W) Avoid using TheoryData with types that implement ITheoryDataRow
- xUnit1053 (W) The static member used as theory data must be statically initialized
- xUnit1065 (E) MemberData method is ambiguous
- xUnit1067 (E) There is no matching MemberData method argument
- xUnit1068 (E) MemberData cannot point to an open generic type

Serializability:

- xUnit1044 (I) and xUnit1046 (I) Theory data arguments that are not serializable (TheoryData and TheoryDataRow)
- xUnit1045 (I) and xUnit1047 (I) Theory data arguments that might not be serializable

Fixtures, collections, and framework-created types:

- xUnit1027 (E) Collection definition classes must be public
- xUnit1033 (I) Classes using IClassFixture or ICollectionFixture should add a constructor argument of type T
- xUnit1041 (W) Fixture arguments to test classes must have fixture sources
- xUnit1056 (E) Type must have a single public non-static constructor
- xUnit1059 (W) Test classes may not be decorated with ICollectionFixture<>

Async and cancellation:

- xUnit1030 (W) Do not call ConfigureAwait in test method
- xUnit1031 (W) Do not use blocking task operations in test method
- xUnit1048 (W) Avoid using async void for test methods
- xUnit1049 (E) Do not use async void for test methods
- xUnit1051 (W) Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken
- xUnit1069 (W) Test methods with a Timeout should reference TestContext.Current.CancellationToken
- xUnit2014 (E) Do not use throws check to check for asynchronously thrown exception
- xUnit2021 (E) Async assertions should be awaited

Skipping and culture:

- xUnit1004 (I) Test methods should not be skipped
- xUnit1054 (E) Properties used for conditional skipping must be public, static, and return bool
- xUnit1055 (E) Conditional skipping cannot set both SkipUnless and SkipWhen
- xUnit1060 (E) Cultured test methods must have at least one culture
- xUnit1070 (E) Cultured test methods cannot have null cultures
- xUnit1071 (W) Cultured test methods should not have duplicate cultures

Native AOT only (relevant only if a project opts into Native AOT):

- xUnit1057 (E) Type must be public or internal (reflection mode permits private types here)
- xUnit1058, xUnit1062, xUnit1064, xUnit1066 (E)
- xUnit1072 (W)

Assertions (2xxx):

- Warnings: xUnit2000, xUnit2002 to xUnit2013, xUnit2015, xUnit2017, xUnit2018, xUnit2020, xUnit2026 to xUnit2031
- Errors: xUnit2014, xUnit2016, xUnit2021
- Info: xUnit2022 to xUnit2025, xUnit2032, xUnit2033
- Hidden: xUnit2001, xUnit2019

Extensibility (3xxx): xUnit3000 to xUnit3007. These only matter in extension projects.

Suppressors shipped by the analyzers: CA1515 (test classes must be public), CA2007 (test methods should not call ConfigureAwait), CS8618 (members initialized in `IAsyncLifetime.InitializeAsync`), and VSTHRD200 (async naming on test methods).

Sources: https://xunit.net/xunit.analyzers/rules/ ; https://github.com/xunit/xunit.analyzers/blob/main/src/xunit.analyzers/Utility/Descriptors.xUnit1xxx.cs ; https://github.com/xunit/xunit.analyzers/blob/main/src/xunit.analyzers/Utility/Descriptors.xUnit2xxx.cs ; https://github.com/xunit/xunit.analyzers/blob/main/src/xunit.analyzers/Utility/Descriptors.Suppressors.cs

### 13.2 Enforcement rules

**XU-130 (MUST)** Every test project MUST build with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. The simplest way is a `Directory.Build.props` that covers the tests folder.

- Rationale: The compiler then reports analyzer warnings as errors, so the xUnit warning rules block the build. Diagnostics at Info level (`suggestion`) do not fail the build. That is why section 13.3 raises selected Info rules to `warning` or `error`.
- Source: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/errors-warnings ; https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options

**XU-131 (MUST)** Use the `.editorconfig` in 13.3. Set the severity of the critical correctness rules to `error` explicitly, so they fail even in a local build where `TreatWarningsAsErrors` is off. New test projects comply from the start. Existing test projects adopt it in the first PR that brings them under this guide; until then they are not in violation. In McpServer, the root `.editorconfig` had no `dotnet_diagnostic.xUnit*` entries on Oct 20, 2026, so this rollout is still to do.

- Rationale: An entry for a single rule ID takes precedence over category-level and global entries.
- Source: https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options

**XU-132 (MUST NOT)** Do not suppress an xUnit diagnostic with `NoWarn`, `#pragma`, or `SuppressMessage` without a justification comment and a human reviewer (XU-003). There is one documented exception: xUnit1031 MAY be set to `none` in an assembly where parallelization is fully disabled.

- Rationale: The xUnit1031 rule page documents that exception.
- Source: https://xunit.net/xunit.analyzers/rules/xUnit1031

**XU-133 (MAY)** If code generated by xUnit.net conflicts with an analyzer, define `XUNIT_GENERATED_DISABLE_WARNINGS` (v3 4.0.1 or later). Do not use it for any other purpose.

- Rationale: It turns off all compiler warnings in generated code. xUnit.net advises using it only when the generated code conflicts with an analyzer.
- Source: https://xunit.net/releases/v3/4.0.1

### 13.3 Sample `.editorconfig` for test folders

Put this file in the root of the tests folder, so it applies only to test code:

```ini
# tests/.editorconfig : xUnit.net v3 team policy (see XU-130 to XU-133)
[*.cs]

# Async and cancellation: never allowed
dotnet_diagnostic.xUnit1030.severity = error
dotnet_diagnostic.xUnit1031.severity = error
dotnet_diagnostic.xUnit1048.severity = error
dotnet_diagnostic.xUnit1049.severity = error
dotnet_diagnostic.xUnit1051.severity = error
dotnet_diagnostic.xUnit1069.severity = error
dotnet_diagnostic.xUnit2014.severity = error
dotnet_diagnostic.xUnit2021.severity = error

# Skips must be reviewed (XU-120); environment skips via SkipUnless/SkipWhen do not trigger this
dotnet_diagnostic.xUnit1004.severity = error

# Structure and data correctness warnings become errors
dotnet_diagnostic.xUnit1005.severity = error
dotnet_diagnostic.xUnit1006.severity = error
dotnet_diagnostic.xUnit1008.severity = error
dotnet_diagnostic.xUnit1013.severity = error
dotnet_diagnostic.xUnit1014.severity = error
dotnet_diagnostic.xUnit1025.severity = error
dotnet_diagnostic.xUnit1026.severity = error
dotnet_diagnostic.xUnit1041.severity = error
dotnet_diagnostic.xUnit1052.severity = error
dotnet_diagnostic.xUnit1053.severity = error
dotnet_diagnostic.xUnit1059.severity = error

# Typed data rows and serializability (Info by default, raised)
dotnet_diagnostic.xUnit1042.severity = warning
dotnet_diagnostic.xUnit1050.severity = warning
dotnet_diagnostic.xUnit1044.severity = warning
dotnet_diagnostic.xUnit1046.severity = warning
dotnet_diagnostic.xUnit1045.severity = suggestion
dotnet_diagnostic.xUnit1047.severity = suggestion

# Assertion quality (Info by default, raised)
dotnet_diagnostic.xUnit2022.severity = warning
dotnet_diagnostic.xUnit2023.severity = warning
dotnet_diagnostic.xUnit2024.severity = warning
dotnet_diagnostic.xUnit2025.severity = warning
dotnet_diagnostic.xUnit2032.severity = warning
dotnet_diagnostic.xUnit2033.severity = warning

# CA2007 conflicts with xUnit1030 in tests
dotnet_diagnostic.CA2007.severity = none
```

Rationale: Every key uses the documented `dotnet_diagnostic.<rule ID>.severity` syntax, and every value is one of `error`, `warning`, `suggestion`, `silent`, `none`, or `default`. Turning off CA2007 in test projects is recommended on the xUnit1030 page.

Sources: https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options ; https://xunit.net/xunit.analyzers/rules/xUnit1030 ; https://xunit.net/xunit.analyzers/rules/

---

## 14. CI guidance

**XU-140 (MUST)** On every PR, CI MUST build with `TreatWarningsAsErrors` and run every test project in the required validation scope. That scope MUST finish with zero failures and zero skips. Tests excluded under XU-121 or XU-142 are outside the scope. Under MTP, a non-zero exit code fails the job.

- Rationale: This enforces XU-001, section 13, and the repository's validation gate, which does not count skipped tests as passing. MTP reports failures through its exit codes (see https://aka.ms/testingplatform/exitcodes, which the MTP output links to).
- Source: Team policy (AGENTS.md, "Byrd Test Gate"); https://xunit.net/docs/getting-started/v3/getting-started ; https://xunit.net/docs/getting-started/v3/microsoft-testing-platform

**XU-141 (MUST)** CI MUST publish results in a machine-readable format.

- MTP: `--report-xunit-trx` (others are `--report-xunit` for xUnit.net XML, `--report-junit`, `--report-ctrf`, `--report-nunit`, `--report-xunit-html`, and `--report-xunit-markdown` in 4.0.1 or later). Reports go to the results directory, which you can change with `--results-directory`.
- Native UX in 4.0 or later: `-result-trx <file>` (others are `-result-xml`, `-result-junit`, `-result-ctrf`, `-result-markdown`). The old `-trx` and `-xml` style switches are deprecated.
- Rationale: The 4.0 release renamed both sets of switches.
- Source: https://xunit.net/releases/v3/4.0.0 ; https://xunit.net/releases/v3/4.0.1 ; https://xunit.net/docs/getting-started/v3/microsoft-testing-platform

**XU-142 (MUST NOT)** The default PR pipeline does not run explicit tests. A separate scheduled or manual job MAY run them with `--explicit only` (MTP) or `-explicit only` (native).

- Rationale: The PR pipeline stays fast and deterministic. The default value of `explicit` is `off`.
- Source: https://xunit.net/docs/config-testconfig-json

**XU-143 (SHOULD)** CI should detect hung tests by setting `longRunningTestSeconds` above 0 together with `diagnosticMessages: true`, or with the `--long-running` / `-longRunning` switch.

- Rationale: Long-running warnings are diagnostic messages, so they only appear when diagnostics are enabled.
- Source: https://xunit.net/docs/config-xunit-runner-json ; https://xunit.net/docs/getting-started/v3/microsoft-testing-platform

**XU-144 (SHOULD)** CI should keep the assembly unique ID that appears in the run output, so a failure that depends on order can be reproduced locally (XU-087).

- Rationale: CI and local runs use different orders because the ID is derived from the file path.
- Source: https://xunit.net/docs/getting-started/v3/whats-new

**XU-145 (SHOULD)** Collect code coverage under MTP with the `Microsoft.Testing.Extensions.CodeCoverage` package (`dotnet test --coverage --coverage-output-format cobertura` with the .NET 10 SDK or later in MTP mode; with the .NET 8 or 9 SDK, where MTP runs on top of the VSTest-based `dotnet test`, add the separator: `dotnet test -- --coverage --coverage-output-format cobertura`). When `dotnet test` runs several projects, do not pass `--coverage-output`.

- Rationale: The standard Coverlet experience is not supported under MTP. A fixed output name makes each project overwrite the previous project's report.
- Source: https://xunit.net/docs/getting-started/v3/code-coverage-with-mtp ; https://xunit.net/docs/getting-started/v3/microsoft-testing-platform ; https://learn.microsoft.com/dotnet/core/testing/migrating-vstest-microsoft-testing-platform

**XU-146 (SHOULD NOT)** Do not enable `stopOnFail` in CI.

- Rationale: The full list of failures is needed to diagnose a run. Even when enabled, tests already in flight still finish.
- Source: https://xunit.net/docs/config-xunit-runner-json

**XU-147 (SHOULD)** The gate job SHOULD enable `failSkips` (`--fail-skips on` in MTP, `-failSkips` in the native UX, or `"failSkips": true` in `xunit.runner.json`) so that any skip inside the required validation scope fails the run (XU-140). CI MAY set `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` to opt out of MTP telemetry.

- Rationale: `failSkips` treats every skip as a failure, including `SkipUnless`/`SkipWhen` skips, which matches the zero-skip gate. The telemetry opt-out is documented by xUnit.net.
- Source: https://xunit.net/docs/config-xunit-runner-json ; https://xunit.net/docs/getting-started/v3/microsoft-testing-platform

---

**XU-148 (SHOULD)** When CI fails because foreground threads are still running at shutdown, fix the thread leak. Do not raise `shutdownForegroundThreadWaitSeconds` to hide it.

- Rationale: If foreground threads do not stop within the configured time (default `10` seconds, v3 4.0 or later), the test project returns failure. A leaked thread usually means a fixture or the code under test is not cleaned up (see XU-097).
- Source: https://xunit.net/docs/config-xunit-runner-json ; https://xunit.net/releases/v3/4.0.0

---

## 15. Anti-patterns

Each item below is forbidden unless a rule above explicitly allows it.

- Committing a failing test as a "TODO" or placeholder (XU-001).
- Adding `Skip` to get the build green (XU-002, XU-120).
- `async void` test methods (XU-070).
- `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` in tests (XU-072).
- `ConfigureAwait(false)` in test methods (XU-073).
- Calling async APIs without `TestContext.Current.CancellationToken` (XU-071).
- `Assert.ThrowsAsync` or other async assertions without `await` (XU-074).
- A `Timeout` on a test that never observes the cancellation token (XU-075).
- `Thread.Sleep` or a real `Task.Delay` to wait for something (XU-092).
- `DateTime.Now`, `DateTimeOffset.UtcNow`, or an unseeded `Random` in code under test (XU-091, XU-093).
- Relying on the machine's default culture (XU-094).
- Mutable static fields shared between tests (XU-090).
- Fixtures with state that tests change and that is never reset (XU-065).
- Fixtures that take a same-level or shorter-lived fixture as a constructor argument (XU-066).
- `ICollectionFixture<T>` on a test class instead of the collection definition (XU-063).
- Collection names as magic strings when the type-based `[Collection]` is available (XU-063).
- Using `ITestPipelineStartup` for work that a fixture can do (XU-067).
- Tests that only pass in a particular order, or orderers that chain tests together (XU-086).
- Turning off parallelization for the whole assembly to hide a flaky test (XU-085).
- Setting `maxParallelThreads` to unlimited (XU-113).
- `MemberData("Name")` string literals instead of `nameof` (XU-044).
- New data sources returning `IEnumerable<object[]>` (XU-042).
- Theory data that cannot be serialized, without a reason (XU-047).
- Public helper methods in test classes (XU-032).
- Storing `TestContext.Current` in a field (XU-076).
- Referencing `xunit.v3` from a non-test library (XU-013).
- Using the obsolete `CollectionBehavior` parallelism properties on 4.0 (XU-083).
- Writing `Console.WriteLine` diagnostics without `CaptureConsole` (XU-100, XU-101).
- Dumping large binary artifacts into test output instead of attachments (XU-102).
- Network, database, or real-device access in unit tests (XU-095).
- Committing `showLiveOutput: true` or using `stopOnFail` in CI (XU-105, XU-146).
- Running explicit tests in the default PR pipeline (XU-142).
- Lowering analyzer severities, or adding `NoWarn`, without review (XU-003, XU-132).

---

## 16. v2 to v3 migration checklist

Every item comes from the official migration guide unless another source is given.

- Replace the `xunit` package with `xunit.v3` (or `xunit.v3.mtp-v2` / `xunit.v3.mtp-off`). Remove `xunit.abstractions` and `xunit.console`. Rename the others: `xunit.assert` becomes `xunit.v3.assert`, `xunit.assert.source` becomes `xunit.v3.assert.source`, `xunit.core` becomes `xunit.v3.core`, `xunit.runner.console` becomes `xunit.v3.runner.console`, `xunit.runner.msbuild` becomes `xunit.v3.runner.msbuild`, `xunit.extensibility.core` and `xunit.extensibility.execution` become `xunit.v3.extensibility.core`, and `xunit.runner.utility` and `xunit.runner.reporters` become `xunit.v3.runner.utility`.
- Keep `xunit.analyzers`. Upgrade `xunit.runner.visualstudio` to 3.0.0 or later (current on Oct 20, 2026: 4.0.1).
- Set `<OutputType>Exe</OutputType>`.
- Move to net8.0 or later, or net472 or later. Confirm the project is SDK-style.
- Fix the namespace change for `ITestOutputHelper`: it moved from `Xunit.Abstractions` to `Xunit`.
- Change `async void` tests to return `Task` or `ValueTask`.
- Update `IAsyncLifetime`: `InitializeAsync` and `DisposeAsync` now return `ValueTask`. Do not rely on `Dispose` and `DisposeAsync` both being called.
- Replace attribute constructors that take a type name string and an assembly name with `typeof(...)`. This affects `CollectionBehavior`, `TestCaseOrderer`, `TestCollectionOrderer`, and `TestFramework`.
- Replace `PropertyData` with `MemberData`. Replace `AssemblyTrait` with `Trait` applied to the assembly.
- Switch theory data to `IEnumerable<TheoryDataRow<...>>` (XU-042).
- Add `TestContext.Current.CancellationToken` wherever xUnit1051 fires (XU-071).
- If you target v3 4.0, replace the `CollectionBehavior` parallelism properties with `[assembly: Parallelization]`, and the `parallelizeTestCollections` setting with `parallelMode` (XU-083; source: https://xunit.net/releases/v3/4.0.0).
- Choose the runner and UX (section 3). For MTP, add `global.json` (.NET SDK 10 or later) or `TestingPlatformDotnetTestSupport` (.NET SDK 8 or 9).
- Update CI switches for the new report and filter names (XU-141).
- Check that every third-party xUnit extension supports the target version before upgrading (XU-160, XU-161).
- Run the suite. It must be green (XU-001).

Source: https://xunit.net/docs/getting-started/v3/migration

**XU-160 (MUST)** Before you move a project to a new major version of xUnit.net v3 (for example 3.x to 4.x), confirm that every third-party package built on xUnit.net extensibility supports that version.

- Rationale: 4.0 contains many breaking changes, mostly in extensibility APIs. A package compiled against an older version can fail at discovery time.
- Source: https://xunit.net/releases/v3/4.0.0 ; https://github.com/AvaloniaUI/Avalonia/issues/22072

**XU-161 (MUST)** Until AvaloniaUI/Avalonia issue #22072 is resolved, pin the Avalonia UI test project to the xUnit.net v3 3.2.x line (for example `xunit.v3.mtp-v2` 3.2.2). Do not upgrade that one project to 4.x. The other test projects target 4.0.2 once their repository completes the 4.0 migration; until then they stay on the repository's checked-in pin (McpServer currently pins 3.2.2 in `Directory.Packages.props`).

- Rationale: `Avalonia.Headless.XUnit` 12.1.4 depends on `xunit.v3.extensibility.core` 3.2.2. Issue #22072 reports discovery failing with a `MissingMethodException` on xunit.v3 4.0.0, and an Avalonia maintainer called 4.0 unsupported for now. The issue was still open on Oct 20, 2026. Each v3 test project is its own executable, so different projects can be on different versions. In that project, write UI tests with `[AvaloniaFact]` / `[AvaloniaTheory]` and `[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]`. If you use `AvaloniaTestIsolationLevel.PerAssembly`, also disable parallelization, because Avalonia does not support concurrent execution in that mode.
- Source: https://github.com/AvaloniaUI/Avalonia/issues/22072 ; https://www.nuget.org/packages/Avalonia.Headless.XUnit/ ; https://docs.avaloniaui.net/docs/testing/headless-xunit ; https://xunit.net/docs/getting-started/v3/microsoft-testing-platform

---

## 17. Unverified items and discrepancies

- **Parallelization property names.** The "Running Tests in Parallel" page shows `[TestClass(DisableParallelism = true)]` and `[Fact(DisableParallelism = true)]`. The source at tag `v3-4.0.2` (`FactAttribute_reflection.cs`, `TestClassAttribute_reflection.cs`) declares `DisableParallelization`. This document uses `DisableParallelization`, which matches the source. Confirm it in the IDE.
- **Cultured tests.** `CulturedFactAttribute` and `CulturedTheoryAttribute` (constructor takes `string[] cultures`) exist in the xunit/xunit source at tag `v3-4.0.2`, and analyzer rules xUnit1060, xUnit1070, and xUnit1071 cover them. I found no xunit.net documentation page for them, so this guide does not prescribe how to use them.
- **TheoryDataRow arity.** "What's new in v3" says the generic `TheoryDataRow<>` has 1 to 10 type arguments. The newer theory data page (Oct 6, 2026) says 1 to 15. This guide does not depend on either number.
- **Rule title wording.** A few rule titles differ slightly between the website and the source descriptors. For example, xUnit1012 is "Null should not be used for value type parameters" on the site and "Null should only be used for nullable parameters" in source. xUnit1030 is "Do not call ConfigureAwait in test method" on the site and "Do not call ConfigureAwait(false) in test method" in source. The IDs and default severities match.
- **Rule xUnit2019** appears on the rules page (Hidden) but not in the extracted `Descriptors.xUnit2xxx.cs` list. It is listed here as Hidden, as the website shows it.
- **xUnit1056.** Its title ("Type must have a single public non-static constructor") and scope (test classes, and types used as class or collection fixtures) are verified on its rule page. The page does not say whether it applies outside Native AOT mode, so this guide only lists it. xUnit1057 is documented as Native AOT only and is listed in that group.
- **`dotnet test` default runner.** The MTP page (May 27, 2026) says `dotnet test` uses VSTest by default. The getting started page (May 2, 2026, template 4.0.0-pre.108) says the template defaults to MTP and writes `global.json`. In practice, a project's runner depends on its template version and settings. XU-020 sets the team default explicitly, so this does not matter for our projects.
- **Avalonia and xUnit 4.0.** The status of AvaloniaUI/Avalonia#22072 is as of Oct 20, 2026. Check it again before lifting XU-161. Whether Avalonia needs the 3.2.x MTP v2 package or the default 3.2.x package was not tested. `xunit.v3.mtp-v2` 3.2.2 exists on NuGet, but I did not build it against Avalonia.
- **Native AOT theory discovery.** The Native AOT page lists serialization as not available in AOT mode, so XU-047 is limited to reflection mode. How AOT mode discovers and lists individual theory rows without serialization was not verified, so this guide gives no AOT-specific rule for it.
- **The v3 command line options page** (https://xunit.net/docs/getting-started/v3/cmdline) returned HTTP 500 during research. Command line switches here were taken from the MTP page and the release notes instead.
- **Team conventions.** The naming scheme (XU-033), seeded randomness (XU-093), temp folders (XU-096), and the agent rules (XU-003) are team policy, not xUnit.net requirements, and are labeled that way.
- **Version-specific CI runner commands** (for example GitHub Actions task syntax) are deliberately left out, because I did not verify them against a primary source.

---

## 18. Sources

Every URL below was accessed Oct 20, 2026.

xUnit.net documentation:

- https://xunit.net/ (accessed Oct 20, 2026)
- https://xunit.net/docs/getting-started/v3/getting-started (accessed Oct 20, 2026)
- https://xunit.net/docs/getting-started/v3/migration (accessed Oct 20, 2026)
- https://xunit.net/docs/getting-started/v3/whats-new (accessed Oct 20, 2026)
- https://xunit.net/docs/getting-started/v3/microsoft-testing-platform (accessed Oct 20, 2026)
- https://xunit.net/docs/getting-started/v3/theory-data-attributes (accessed Oct 20, 2026)
- https://xunit.net/docs/getting-started/v3/code-coverage-with-mtp (accessed Oct 20, 2026)
- https://xunit.net/docs/getting-started/v3/native-aot (accessed Oct 20, 2026)
- https://api.xunit.net/v3/4.0.0/Xunit.IClassFixture-1.html (accessed Oct 20, 2026)
- https://api.xunit.net/v3/4.0.0/Xunit.ICollectionFixture-1.html (accessed Oct 20, 2026)
- https://api.xunit.net/v3/3.2.2/Xunit.TheoryData-1.html (accessed Oct 20, 2026)
- https://xunit.net/docs/getting-started/v3/cmdline (accessed Oct 20, 2026; returned HTTP 500, not used for facts)
- https://github.com/xunit/xunit.net/blob/main/site/docs/getting-started/v3/custom-serialization.md (accessed Oct 20, 2026)
- https://xunit.net/docs/shared-context (accessed Oct 20, 2026)
- https://xunit.net/docs/running-tests-in-parallel (accessed Oct 20, 2026)
- https://xunit.net/docs/capturing-output (accessed Oct 20, 2026)
- https://xunit.net/docs/config-xunit-runner-json (accessed Oct 20, 2026)
- https://xunit.net/docs/config-testconfig-json (accessed Oct 20, 2026)
- https://xunit.net/docs/nuget-packages-v3 (accessed Oct 20, 2026)
- https://xunit.net/releases/v3/4.0.0 (accessed Oct 20, 2026)
- https://xunit.net/releases/v3/4.0.1 (accessed Oct 20, 2026)
- https://xunit.net/releases/v3/4.0.2 (accessed Oct 20, 2026)
- https://xunit.net/releases/analyzers/2.2.0 (accessed Oct 20, 2026)

xUnit.net analyzer rules:

- https://xunit.net/xunit.analyzers/rules/ (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1004 (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1030 (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1031 (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1022 (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1023 (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1044 (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1052 (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1054 (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1056 (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1057 (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1051 (accessed Oct 20, 2026)
- https://xunit.net/xunit.analyzers/rules/xUnit1069 (accessed Oct 20, 2026)

GitHub repositories:

- https://github.com/xunit/xunit (accessed Oct 20, 2026)
- https://github.com/xunit/xunit.analyzers (accessed Oct 20, 2026)
- https://github.com/xunit/xunit.net (accessed Oct 20, 2026)
- https://github.com/xunit/xunit.analyzers/blob/main/src/xunit.analyzers/Utility/Descriptors.xUnit1xxx.cs (accessed Oct 20, 2026)
- https://github.com/xunit/xunit.analyzers/blob/main/src/xunit.analyzers/Utility/Descriptors.xUnit2xxx.cs (accessed Oct 20, 2026)
- https://github.com/xunit/xunit.analyzers/blob/main/src/xunit.analyzers/Utility/Descriptors.Suppressors.cs (accessed Oct 20, 2026)
- https://github.com/xunit/xunit/blob/main/src/xunit.v3.core/ITestContext.cs (accessed Oct 20, 2026)
- https://github.com/xunit/xunit/blob/main/src/xunit.v3.core/Abstractions/Framework/ITestPipelineStartup.cs (accessed Oct 20, 2026)
- https://github.com/xunit/xunit/blob/main/src/xunit.v3.common/Options/ExplicitOption.cs (accessed Oct 20, 2026)
- https://github.com/xunit/xunit/blob/main/src/xunit.v3.core/Attributes/ParallelizationAttribute.cs (accessed Oct 20, 2026)
- https://github.com/xunit/xunit/blob/v3-4.0.2/src/xunit.v3.core/FactAttribute_reflection.cs (accessed Oct 20, 2026)
- https://github.com/xunit/xunit/blob/v3-4.0.2/src/xunit.v3.core/TestClassAttribute_reflection.cs (accessed Oct 20, 2026)
- https://github.com/xunit/xunit/blob/v3-4.0.2/src/xunit.v3.core/CulturedFactAttribute_reflection.cs (accessed Oct 20, 2026)

NuGet (version verification):

- https://api.nuget.org/v3-flatcontainer/xunit.v3/index.json (accessed Oct 20, 2026)
- https://api.nuget.org/v3-flatcontainer/xunit.v3.mtp-v2/index.json (accessed Oct 20, 2026)
- https://api.nuget.org/v3-flatcontainer/xunit.v3.mtp-off/index.json (accessed Oct 20, 2026)
- https://api.nuget.org/v3-flatcontainer/xunit.analyzers/index.json (accessed Oct 20, 2026)
- https://api.nuget.org/v3-flatcontainer/xunit.runner.visualstudio/index.json (accessed Oct 20, 2026)
- https://api.nuget.org/v3-flatcontainer/xunit.v3.runner.console/index.json (accessed Oct 20, 2026)
- https://api.nuget.org/v3-flatcontainer/xunit-console-tool/index.json (accessed Oct 20, 2026)
- https://api.nuget.org/v3-flatcontainer/xunit.v3.templates/index.json (accessed Oct 20, 2026)
- https://api.nuget.org/v3-flatcontainer/microsoft.extensions.timeprovider.testing/index.json (accessed Oct 20, 2026)
- https://api.nuget.org/v3-flatcontainer/avalonia.headless.xunit/12.1.4/avalonia.headless.xunit.nuspec (accessed Oct 20, 2026)
- https://www.nuget.org/packages/Avalonia.Headless.XUnit/ (accessed Oct 20, 2026)

Microsoft Learn:

- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/errors-warnings (accessed Oct 20, 2026)
- https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options (accessed Oct 20, 2026)
- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/ (accessed Oct 20, 2026)
- https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview (accessed Oct 20, 2026)
- https://learn.microsoft.com/dotnet/core/testing/migrating-vstest-microsoft-testing-platform (accessed Oct 20, 2026)
- https://aka.ms/testingplatform/exitcodes (linked from the MTP output on the xunit.net getting started page, referenced in XU-140, accessed Oct 20, 2026)

Repository policy:

- `AGENTS.md`, sections "Byrd Test Gate" and "Correctness", in this repository (read Oct 20, 2026)
- https://github.com/xunit/xunit/blob/v3-4.0.2/src/xunit.v3.core/ITheoryDataRow.cs (accessed Oct 20, 2026)
- https://github.com/xunit/assert.xunit/blob/main/README.md (accessed Oct 20, 2026)
- https://api.nuget.org/v3-flatcontainer/xunit.v3.mtp-v2/4.0.2/xunit.v3.mtp-v2.nuspec (accessed Oct 20, 2026)
- https://api.nuget.org/v3-flatcontainer/xunit.v3.core.mtp-v2/4.0.2/xunit.v3.core.mtp-v2.nuspec (accessed Oct 20, 2026)

Avalonia:

- https://docs.avaloniaui.net/docs/testing/headless-xunit (accessed Oct 20, 2026)
- https://github.com/AvaloniaUI/Avalonia/issues/22072 (accessed Oct 20, 2026)
- https://github.com/AvaloniaUI/Avalonia/pull/20481 (accessed Oct 20, 2026)
