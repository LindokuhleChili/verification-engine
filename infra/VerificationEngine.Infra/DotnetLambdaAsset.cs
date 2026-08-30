using System.Diagnostics;
using Amazon.CDK.AWS.Lambda;

namespace VerificationEngine.Infra;

/// <summary>
/// Packages a .NET Lambda project the same way for every function in this stack, so
/// there is exactly one place that knows how a C# Lambda gets built.
///
/// Runs `dotnet publish` directly on the host, targeting linux-x64 as a framework-
/// dependent deployment against the managed dotnet8 runtime, rather than through CDK's
/// Docker-based asset bundling: a framework-dependent publish only needs the right
/// files selected for the target RID (managed assemblies are OS-agnostic; only
/// QuestPDF's native SkiaSharp asset is RID-specific - see the Api project's
/// SkiaSharp.NativeAssets.Linux.NoDependencies reference), which NuGet resolves
/// correctly cross-platform. That means no Docker Desktop dependency for `cdk deploy`
/// on a machine that doesn't have it installed - see docs/DEPLOYING.md.
/// </summary>
public static class DotnetLambdaAsset
{
    public static Code FromProject(string projectName)
    {
        // `cdk` runs the app command (see cdk.json) with the current directory set to
        // wherever cdk.json lives - this project's folder - so two levels up is the
        // repo root regardless of build configuration or target framework moniker.
        var repoRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."));
        var projectPath = Path.Combine(repoRoot, "backend", projectName, $"{projectName}.csproj");
        var outputPath = Path.Combine(repoRoot, "infra", "VerificationEngine.Infra", "lambda-publish", projectName);

        if (!File.Exists(projectPath))
            throw new FileNotFoundException($"Could not find Lambda project '{projectName}' at '{projectPath}'.");

        RunDotnetPublish(projectPath, outputPath);

        return Code.FromAsset(outputPath);
    }

    private static void RunDotnetPublish(string projectPath, string outputPath)
    {
        var arguments = $"publish \"{projectPath}\" -c Release -r linux-x64 --self-contained false -o \"{outputPath}\"";

        Console.WriteLine($"[DotnetLambdaAsset] dotnet {arguments}");

        using var process = Process.Start(new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Failed to start the `dotnet publish` process.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Console.WriteLine(stdout);

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"`dotnet publish` failed for '{projectPath}' with exit code {process.ExitCode}.\n{stdout}\n{stderr}");
    }
}
