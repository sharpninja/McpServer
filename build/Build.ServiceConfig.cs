using Nuke.Common.IO;
using Serilog;

partial class Build
{
    internal const string BrainSlotConfigDirectoryName = "brain-slots";
    internal const string BrainSlotConfigFileName = "quad-brain-slot-assignments.yaml";

    /// <summary>Copies source-controlled brain-slot runtime configuration into a deployment root.</summary>
    internal static string CopyBrainSlotRuntimeConfig(AbsolutePath rootDirectory, string destinationRoot)
    {
        var sourceDirectory = rootDirectory / "config" / BrainSlotConfigDirectoryName;
        var sourceFile = sourceDirectory / BrainSlotConfigFileName;
        if (!File.Exists(sourceFile))
            throw new FileNotFoundException($"Required brain-slot config file was not found: {sourceFile}");

        var destinationDirectory = Path.Combine(destinationRoot, "config", BrainSlotConfigDirectoryName);
        Directory.CreateDirectory(destinationDirectory);

        var destinationFile = Path.Combine(destinationDirectory, BrainSlotConfigFileName);
        File.Copy(sourceFile, destinationFile, true);
        Log.Information("  Brain-slot config deployed: {Path}", destinationFile);
        return destinationFile;
    }
}
