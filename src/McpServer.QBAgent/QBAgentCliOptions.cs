namespace McpServer.QBAgent;

/// <summary>Command-line and environment options for qbagent display of Arbiter intent JSON.</summary>
internal sealed class QBAgentCliOptions
{
    internal const string ShowIntentFlag = "--show-intent";
    internal const string HideIntentFlag = "--hide-intent";
    internal const string ResumeFlag = "--resume";
    internal const string ResumeLatest = "latest";
    internal const string ShowIntentEnv = "QBAGENT_SHOW_INTENT";
    internal const string HideIntentEnv = "QBAGENT_HIDE_INTENT";

    public string? StartDirectory { get; init; }

    public bool ShowVersion { get; init; }

    /// <summary>When false, Arbiter intent JSON is omitted from terminal display.</summary>
    public bool ShowIntent { get; init; }

    /// <summary>
    /// Session id to resume, <see cref="ResumeLatest"/> for the newest log, or null for a new session.
    /// </summary>
    public string? ResumeSessionId { get; init; }

    internal static QBAgentCliOptions Parse(string[] args, IReadOnlyDictionary<string, string?>? environment = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        var showFlag = false;
        var hideFlag = false;
        var showVersion = false;
        string? directory = null;
        string? resume = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--version", StringComparison.OrdinalIgnoreCase))
            {
                showVersion = true;
                continue;
            }

            if (string.Equals(arg, ShowIntentFlag, StringComparison.OrdinalIgnoreCase))
            {
                showFlag = true;
                continue;
            }

            if (string.Equals(arg, HideIntentFlag, StringComparison.OrdinalIgnoreCase))
            {
                hideFlag = true;
                continue;
            }

            if (string.Equals(arg, ResumeFlag, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    i++;
                    resume = args[i];
                }
                else
                {
                    resume = ResumeLatest;
                }

                continue;
            }

            if (arg.StartsWith('-'))
                continue;

            directory = arg;
        }

        return new QBAgentCliOptions
        {
            StartDirectory = directory,
            ShowVersion = showVersion,
            ShowIntent = ResolveShowIntent(showFlag, hideFlag, environment),
            ResumeSessionId = resume,
        };
    }

    internal static bool ResolveShowIntent(
        bool showFlag,
        bool hideFlag,
        IReadOnlyDictionary<string, string?>? environment)
    {
        if (hideFlag)
            return false;
        if (showFlag)
            return true;

        var env = environment ?? ReadProcessEnvironment();
        if (IsTruthy(env, HideIntentEnv))
            return false;
        return IsTruthy(env, ShowIntentEnv);
    }

    private static Dictionary<string, string?> ReadProcessEnvironment()
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in new[] { ShowIntentEnv, HideIntentEnv })
            map[name] = Environment.GetEnvironmentVariable(name);
        return map;
    }

    private static bool IsTruthy(IReadOnlyDictionary<string, string?> environment, string key)
    {
        if (!environment.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            return false;
        return value is "1" or "true" or "TRUE" or "yes" or "YES";
    }
}
