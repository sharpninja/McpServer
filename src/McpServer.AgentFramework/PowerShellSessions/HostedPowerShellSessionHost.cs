using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;

namespace McpServer.AgentFramework.PowerShellSessions;

internal sealed class HostedPowerShellSessionHost : PSHost
{
    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly HostedPowerShellSessionHostUserInterface _ui = new();

    public HostedPowerShellCommandExecutionContext? CurrentExecutionContext
    {
        get => _ui.CurrentExecutionContext;
        set => _ui.CurrentExecutionContext = value;
    }

    public override CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    public override CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;

    public override Guid InstanceId => _instanceId;

    public override string Name => nameof(HostedPowerShellSessionHost);

    public override PSHostUserInterface UI => _ui;

    public override Version Version => new(1, 0);

    public override void EnterNestedPrompt() =>
        throw new NotSupportedException("Nested prompts are not supported by the hosted PowerShell session.");

    public override void ExitNestedPrompt() =>
        throw new NotSupportedException("Nested prompts are not supported by the hosted PowerShell session.");

    public override void NotifyBeginApplication()
    {
    }

    public override void NotifyEndApplication()
    {
    }

    public override void SetShouldExit(int exitCode)
    {
    }

    private sealed class HostedPowerShellSessionHostUserInterface : PSHostUserInterface
    {
        private readonly HostedPowerShellSessionRawUserInterface _rawUi = new();

        public HostedPowerShellCommandExecutionContext? CurrentExecutionContext { get; set; }

        public override PSHostRawUserInterface RawUI => _rawUi;

        public override Dictionary<string, PSObject> Prompt(
            string caption,
            string message,
            Collection<FieldDescription> descriptions)
        {
            var context = GetExecutionContext();
            var result = new Dictionary<string, PSObject>(StringComparer.OrdinalIgnoreCase);

            WriteSectionHeader(caption, message, context);
            foreach (var description in descriptions)
            {
                Write($"{description.Name}: ");
                result[description.Name] = PSObject.AsPSObject(ReadLine());
            }

            return result;
        }

        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName)
        {
            var context = GetExecutionContext();
            WriteSectionHeader(caption, message, context);
            Write(string.IsNullOrWhiteSpace(userName) ? "User name: " : $"User name [{userName}]: ");
            var resolvedUserName = ReadLine();
            if (string.IsNullOrWhiteSpace(resolvedUserName))
                resolvedUserName = userName;

            Write("Password: ");
            return new PSCredential(resolvedUserName, ReadLineAsSecureString());
        }

        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName,
            PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options) =>
            PromptForCredential(caption, message, userName, targetName);

        public override int PromptForChoice(
            string caption,
            string message,
            Collection<ChoiceDescription> choices,
            int defaultChoice)
        {
            var context = GetExecutionContext();
            WriteSectionHeader(caption, message, context);
            for (var index = 0; index < choices.Count; index++)
                WriteLine($"[{index}] {choices[index].Label}");

            while (true)
            {
                Write(defaultChoice >= 0 ? $"Choice [{defaultChoice}]: " : "Choice: ");
                var input = ReadLine();
                if (string.IsNullOrWhiteSpace(input) && defaultChoice >= 0)
                    return defaultChoice;

                if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var selectedChoice)
                    && selectedChoice >= 0
                    && selectedChoice < choices.Count)
                {
                    return selectedChoice;
                }

                context.WriteError("Enter the numeric index for one of the listed choices.", true);
            }
        }

        public override string ReadLine()
        {
            var context = GetExecutionContext();
            var line = context.ReadLine?.Invoke(context.CancellationToken);
            return line ?? string.Empty;
        }

        public override SecureString ReadLineAsSecureString()
        {
            var secureString = new SecureString();
            foreach (var character in ReadLine())
                secureString.AppendChar(character);

            secureString.MakeReadOnly();
            return secureString;
        }

        public override void Write(string value) => GetExecutionContext().WriteOutput(value, false);

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value) =>
            Write(value);

        public override void WriteDebugLine(string message) =>
            GetExecutionContext().WriteOutput(message, true);

        public override void WriteErrorLine(string value) =>
            GetExecutionContext().WriteError(value, true);

        public override void WriteLine(string value) =>
            GetExecutionContext().WriteOutput(value, true);

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            if (record is null)
                return;

            var progressText = string.IsNullOrWhiteSpace(record.StatusDescription)
                ? record.Activity
                : $"{record.Activity}: {record.StatusDescription}";

            if (!string.IsNullOrWhiteSpace(progressText))
                GetExecutionContext().WriteOutput(progressText, true);
        }

        public override void WriteVerboseLine(string message) =>
            GetExecutionContext().WriteOutput(message, true);

        public override void WriteWarningLine(string message) =>
            GetExecutionContext().WriteOutput(message, true);

        private HostedPowerShellCommandExecutionContext GetExecutionContext() =>
            CurrentExecutionContext ?? throw new InvalidOperationException(
                "PowerShell host interaction is only available while a hosted command is executing.");

        private static void WriteSectionHeader(
            string caption,
            string message,
            HostedPowerShellCommandExecutionContext context)
        {
            if (!string.IsNullOrWhiteSpace(caption))
                context.WriteOutput(caption, true);

            if (!string.IsNullOrWhiteSpace(message))
                context.WriteOutput(message, true);
        }
    }

    private sealed class HostedPowerShellSessionRawUserInterface : PSHostRawUserInterface
    {
        private ConsoleColor _backgroundColor = ConsoleColor.Black;
        private Size _bufferSize = new(120, 300);
        private Coordinates _cursorPosition = new(0, 0);
        private int _cursorSize = 1;
        private ConsoleColor _foregroundColor = ConsoleColor.Gray;
        private Size _windowSize = new(120, 40);
        private Coordinates _windowPosition = new(0, 0);
        private string _windowTitle = "Hosted PowerShell Session";

        public override ConsoleColor BackgroundColor
        {
            get => _backgroundColor;
            set => _backgroundColor = value;
        }

        public override Size BufferSize
        {
            get => _bufferSize;
            set => _bufferSize = value;
        }

        public override Coordinates CursorPosition
        {
            get => _cursorPosition;
            set => _cursorPosition = value;
        }

        public override int CursorSize
        {
            get => _cursorSize;
            set => _cursorSize = value;
        }

        public override void FlushInputBuffer()
        {
            if (Console.IsInputRedirected)
                return;

            while (Console.KeyAvailable)
                _ = Console.ReadKey(intercept: true);
        }

        public override ConsoleColor ForegroundColor
        {
            get => _foregroundColor;
            set => _foregroundColor = value;
        }

        public override BufferCell[,] GetBufferContents(Rectangle rectangle) =>
            throw new NotSupportedException("Buffer inspection is not supported by the hosted PowerShell session.");

        public override bool KeyAvailable =>
            !Console.IsInputRedirected && Console.KeyAvailable;

        public override Size MaxPhysicalWindowSize => _windowSize;

        public override Size MaxWindowSize => _windowSize;

        public override Coordinates WindowPosition
        {
            get => _windowPosition;
            set => _windowPosition = value;
        }

        public override Size WindowSize
        {
            get => _windowSize;
            set => _windowSize = value;
        }

        public override string WindowTitle
        {
            get => _windowTitle;
            set => _windowTitle = value;
        }

        public override KeyInfo ReadKey(ReadKeyOptions options) =>
            throw new NotSupportedException("Raw key reading is not supported by the hosted PowerShell session.");

        public override void ScrollBufferContents(
            Rectangle source,
            Coordinates destination,
            Rectangle clip,
            BufferCell fill) =>
            throw new NotSupportedException("Buffer scrolling is not supported by the hosted PowerShell session.");

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents) =>
            throw new NotSupportedException("Buffer writes are not supported by the hosted PowerShell session.");

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill) =>
            throw new NotSupportedException("Buffer writes are not supported by the hosted PowerShell session.");
    }
}

internal sealed class HostedPowerShellCommandExecutionContext
{
    private readonly StringBuilder _capturedError = new();
    private readonly StringBuilder _capturedOutput = new();

    public HostedPowerShellCommandExecutionContext(
        CancellationToken cancellationToken,
        Func<CancellationToken, string?>? readLine = null,
        TextWriter? outputWriter = null,
        TextWriter? errorWriter = null,
        bool captureHostOutput = false)
    {
        CancellationToken = cancellationToken;
        ReadLine = readLine;
        OutputWriter = outputWriter ?? TextWriter.Null;
        ErrorWriter = errorWriter ?? TextWriter.Null;
        CaptureHostOutput = captureHostOutput;
    }

    public CancellationToken CancellationToken { get; }

    public bool CaptureHostOutput { get; }

    public string CapturedErrorText => Normalize(_capturedError);

    public string CapturedOutputText => Normalize(_capturedOutput);

    public TextWriter ErrorWriter { get; }

    public TextWriter OutputWriter { get; }

    public Func<CancellationToken, string?>? ReadLine { get; }

    public void WriteError(string? value, bool appendNewLine)
    {
        if (CaptureHostOutput)
        {
            Append(_capturedError, value, appendNewLine);
            return;
        }

        if (appendNewLine)
            ErrorWriter.WriteLine(value);
        else
            ErrorWriter.Write(value);

        ErrorWriter.Flush();
    }

    public void WriteOutput(string? value, bool appendNewLine)
    {
        if (CaptureHostOutput)
        {
            Append(_capturedOutput, value, appendNewLine);
            return;
        }

        if (appendNewLine)
            OutputWriter.WriteLine(value);
        else
            OutputWriter.Write(value);

        OutputWriter.Flush();
    }

    private static void Append(StringBuilder builder, string? value, bool appendNewLine)
    {
        if (!string.IsNullOrEmpty(value))
            builder.Append(value);

        if (appendNewLine)
            builder.AppendLine();
    }

    private static string Normalize(StringBuilder builder)
    {
        var text = builder.ToString();
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.ReplaceLineEndings(Environment.NewLine).TrimEnd();
    }
}
