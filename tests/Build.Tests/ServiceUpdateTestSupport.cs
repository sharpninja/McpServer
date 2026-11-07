using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static NukeBuild.Tests.ServiceUpdateTestSupport;

namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-SERVICEUPDATE-001: the reflection and native-command helpers the service-update tests share, linked into
/// both the build's infrastructure tests (Build.Tests) and the build's integration tests (NukeBuild.IntegrationTests).
/// </summary>
/// <remarks>
/// <para>2026-11-07, TEST-MCP-SERVICEUPDATE-001; TR-MCP-NUKE-015 AC-004, AC-005; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from the private helpers of ServiceUpdateTests when that class split into its infrastructure half (Build.Tests) and its Linux integration half (Build.IntegrationTests) in slice S4 (hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); each half reaches them through a using-static directive, so the test bodies are unchanged.</para>
/// <para>2026-11-07, TEST-MCP-SERVICEUPDATE-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the summary names NukeBuild.IntegrationTests, Build.IntegrationTests renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the helpers are unchanged.</para>
/// </remarks>
internal static class ServiceUpdateTestSupport
{
    /// <summary>Runs installed metadata utilities against an isolated fixture, failing on timeout or nonzero exit.</summary>
    /// <param name="command">The utility.</param>
    /// <param name="arguments">Its arguments.</param>
    /// <returns>The utility's standard output.</returns>
    internal static string Native(string command, params string[] arguments)
    {
        var info = new ProcessStartInfo(command);
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        var result = BoundedProcess.Run(info, TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        Assert.Equal(0, result.ExitCode);
        return result.Stdout;
    }

    /// <summary>Reflection keeps absent production contracts compilable for the red gate.</summary>
    /// <param name="name">The production type name in the build assembly.</param>
    /// <returns>The type.</returns>
    internal static Type Contract(string name) => typeof(WindowsServiceHelper).Assembly.GetType(name) ?? throw new InvalidOperationException("Missing production contract: " + name);

    /// <summary>Reads a returned contract record.</summary>
    /// <param name="target">The record.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property value.</returns>
    internal static object Property(object target, string name) => target.GetType().GetProperty(name)!.GetValue(target)!;

    /// <summary>Invokes the intended API while preserving its original exception for assertions.</summary>
    /// <param name="type">The production type name.</param>
    /// <param name="method">The method name.</param>
    /// <param name="instance">The instance, or null for a static method.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>The method's result.</returns>
    internal static object Call(string type, string method, object? instance, params object[] args)
    {
        try { return (Contract(type).GetMethod(method, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Missing production method: " + method)).Invoke(instance, args)!; }
        catch (TargetInvocationException ex) { ExceptionDispatchInfo.Capture(ex.InnerException!).Throw(); throw; }
    }
}

/// <summary>
/// TEST-MCP-SERVICEUPDATE-001: isolated live-state fixture and command double; tar executes against temporary files only.
/// Shared by link with <see cref="ServiceUpdateTestSupport"/>; each half of ServiceUpdateTests names it Fixture through a
/// using alias.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-SERVICEUPDATE-001; TR-MCP-NUKE-015 AC-004, AC-005; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved unchanged from the private nested class ServiceUpdateTests.Fixture (slice S4, hostile validation finding R14-F4); only its name and accessibility changed, so both halves of the split class share one fixture.</remarks>
internal sealed class ServiceUpdateFixture : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "service-update-" + Guid.NewGuid().ToString("N"));
    public string Install => Path.Combine(Root, "app");
    public string Stage => Path.Combine(Root, "stage");
    public string Data => Path.Combine(Root, "data");
    public string Config => Path.Combine(Install, "appsettings.yaml");
    public string Executable => Path.Combine(Install, "QBrainAi.Support.Mcp");
    public string Env => Path.Combine(Root, "service.env");
    public string Backup { get; set; }
    public string UnitExecutable { get; set; }
    public string WorkingDirectory { get; set; }
    public string LoadState { get; set; } = "loaded";
    public bool FailArchive { get; set; }
    public bool FailCopy { get; set; }
    public bool FailHealth { get; set; }
    public Queue<string?> ProcessObservations { get; } = new();
    public int ProcessChecks { get; private set; }
    public bool WrongProcess { get; set; }
    public bool RealTar { get; set; }
    public bool InspectRealFileKinds { get; set; }
    public bool OptionalEnvironment { get; set; }
    /// <summary>
    /// 2026-11-04, TR-MCP-SERVICEUPDATE-001 repeated-properties (slice Z3): a required environment file the fake
    /// systemctl show names on a second EnvironmentFiles line, as systemd 262 prints one line per file; null prints no
    /// second line.
    /// </summary>
    public string? SecondEnv { get; set; }
    public string DropIns { get; set; } = "";
    public string PrivateOwner { get; set; } = "0";
    public string ServiceName { get; set; } = "example.service";
    public string ExtraProperties { get; set; } = "";
    public string Arguments { get; set; } = "--urls http://localhost:7147";
    public Action? OnCopy { get; set; }
    public string OriginalConfig => "DataFolder: '" + Data + "'\n";
    public List<string> Events { get; } = [];
    public List<string> TarArguments { get; } = [];
    private bool started;
    private readonly Dictionary<string, byte[]> snapshot = [];

    /// <summary>Builds ordinary config/data plus an ELF header matching the test RID.</summary>
    public ServiceUpdateFixture(bool requireLinux = true)
    {
        // Fail the red gate at contract binding, never mistake a missing API for safe rejection.
        if (requireLinux) _ = Contract("LinuxServiceHelper");
        foreach (var path in new[] { Install, Stage, Data }) Directory.CreateDirectory(path);
        Backup = Path.Combine(Root, "backups"); UnitExecutable = Executable;
        WorkingDirectory = Install;
        File.WriteAllText(Config, OriginalConfig); File.WriteAllText(Executable, "old");
        File.WriteAllText(Path.Combine(Data, "state"), "original");
        File.WriteAllText(Env, "ConnectionStrings__Default=private\n");
        File.WriteAllText(Path.Combine(Root, "example.service"), "[Service]\n");
        var elf = new byte[64]; elf[0] = 127; elf[1] = 69; elf[2] = 76; elf[3] = 70; elf[4] = 2; elf[5] = 1; elf[18] = 62;
        File.WriteAllBytes(Path.Combine(Stage, "QBrainAi.Support.Mcp"), elf);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(Path.Combine(Stage, "QBrainAi.Support.Mcp"), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        File.WriteAllText(Path.Combine(Stage, "appsettings.yaml"), "DataFolder: wrong\n");
    }

    /// <summary>Adapts the native-command contract without requiring unimplemented types at compile time.</summary>
    public object Update()
    {
        var helper = CreateHelper();
        var options = Activator.CreateInstance(Contract("LinuxServiceUpdateOptions"), ServiceName, Install, Stage, Backup, "linux-x64", Path.Combine(Root, "locks"))!;
        return Call("LinuxServiceHelper", "Update", helper, options, (Action)(() =>
        {
            Events.Add("defaults"); OnCopy?.Invoke();
            if (FailCopy) { File.WriteAllText(Config, "changed"); File.WriteAllText(Path.Combine(Data, "state"), "changed"); throw new IOException("injected copy failure"); }
        }), (Action)(() => { Events.Add("health"); if (FailHealth) { File.WriteAllText(Config, "changed"); File.WriteAllText(Path.Combine(Data, "state"), "changed"); throw new IOException("injected health failure"); } }));
    }

    /// <summary>Binds the command double for whole-update and isolated preflight tests.</summary>
    public object CreateHelper()
    {
        var resultType = Contract("ServiceCommandResult");
        var command = Expression.Parameter(typeof(string)); var arguments = Expression.Parameter(typeof(IReadOnlyList<string>));
        var body = Expression.Convert(Expression.Call(Expression.Constant(this), GetType().GetMethod(nameof(Run))!, command, arguments), resultType);
        var runner = Expression.Lambda(typeof(Func<,,>).MakeGenericType(typeof(string), typeof(IReadOnlyList<string>), resultType), body, command, arguments).Compile();
        return Activator.CreateInstance(Contract("LinuxServiceHelper"), runner)!;
    }

    /// <summary>Fakes systemd/id/readlink; real tar provides independent preservation evidence.</summary>
    public object Run(string command, IReadOnlyList<string> args)
    {
        object Result(int code, string output) => Activator.CreateInstance(Contract("ServiceCommandResult"), code, output)!;
        if (command == "id") return Result(0, "0");
        if (command == "stat") return Result(0, args.Contains("%f")
            ? InspectRealFileKinds ? Native("stat", args.ToArray()) : Directory.Exists(args.Last()) ? "41ed" : "81a4"
            : PrivateOwner);
        if (command == "readlink")
        {
            ProcessChecks++;
            var observation = ProcessObservations.Count > 0 ? ProcessObservations.Dequeue() : WrongProcess ? "/unexpected/process" : Executable;
            return observation is null ? Result(1, "") : Result(0, observation);
        }
        if (command == "systemctl")
        {
            if (args[0] == "show") return Result(0, $"LoadState={LoadState}\nType=simple\nFragmentPath={Path.Combine(Root, "example.service")}\nDropInPaths={DropIns}\nEnvironmentFiles={Env} (ignore_errors={(OptionalEnvironment ? "yes" : "no")})\n{(SecondEnv is null ? "" : $"EnvironmentFiles={SecondEnv} (ignore_errors=no)\n")}Environment=\nWorkingDirectory={WorkingDirectory}\nExecStart={{ path={UnitExecutable} ; argv[]={UnitExecutable} {Arguments} ; ignore_errors=no ; }}\nMainPID={(started ? Environment.ProcessId : 0)}\nActiveState={(started ? "active" : "inactive")}\n{ExtraProperties}");
            if (args[0] == "is-active") return Result(started ? 0 : 3, started ? "active" : "inactive");
            Events.Add(args[0]); if (args[0] == "start") started = true; if (args[0] == "stop") started = false;
            return Result(0, "");
        }
        if (command != "tar") throw new InvalidOperationException("Unexpected command: " + command);
        TarArguments.AddRange(args);
        if (args.Contains("--create")) { Events.Add("archive"); if (FailArchive) return Result(2, "injected"); }
        if (args.Contains("--extract")) Events.Add("restore");
        if (!RealTar)
        {
            if (args.Contains("--create"))
            {
                var array = args.ToArray();
                foreach (var relative in array.Skip(Array.IndexOf(array, "--") + 1))
                {
                    var path = Path.GetFullPath(relative, Path.GetPathRoot(Root)!);
                    foreach (var file in Directory.Exists(path) ? Directory.GetFiles(path, "*", SearchOption.AllDirectories) : new[] { path })
                        snapshot[file] = File.ReadAllBytes(file);
                }
                File.WriteAllText(array[Array.IndexOf(array, "--file") + 1], "archive-double");
            }
            if (args.Contains("--extract")) foreach (var (path, bytes) in snapshot)
            { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, bytes); }
            return Result(0, args.Contains("--version") ? "tar (GNU tar) test-double" : "");
        }
        var info = new ProcessStartInfo(command);
        foreach (var arg in args) info.ArgumentList.Add(arg);
        var result = BoundedProcess.Run(info, TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        return Result(result.ExitCode, result.Stdout);
    }

    /// <summary>Removes only this test's temporary tree.</summary>
    public void Dispose() => Directory.Delete(Root, true);
}
