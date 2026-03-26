using System;
using System.Diagnostics;
using System.IO;
using Godot.BindingsGeneration.Logging;
using Godot.BindingsGeneration.ApiDump;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Godot.BindingsGeneration;

/// <summary>
/// MSBuild task to execute the Godot bindings generator.
/// </summary>
public class GenerateTask : Task
{
    /// <summary>
    /// Path to the extension API dump JSON file.
    /// </summary>
    [Required]
    public required string ExtensionApiPath { get; set; }

    /// <summary>
    /// Path to the extension interface header file.
    /// </summary>
    [Required]
    public required string ExtensionInterfacePath { get; set; }

    /// <summary>
    /// Path to the directory where the C# bindings will be generated.
    /// </summary>
    [Required]
    public required string OutputPath { get; set; }

    /// <summary>
    /// Path to the directory where the C# tests will be generated.
    /// </summary>
    public string? TestOutputPath { get; set; }

    /// <summary>
    /// When enabled, the task will enforce that the API version matches
    /// the version of the Godot .NET packages.
    /// </summary>
    public bool ValidateBindingsVersion { get; set; }

    /// <summary>
    /// Execute the MSBuild task.
    /// </summary>
    /// <returns><see langword="true"/> if the task was successful.</returns>
    public override bool Execute()
    {
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new ThrowingAssertionListener());

        try
        {
            // Clean output directories first.
            if (Directory.Exists(OutputPath))
            {
                Directory.Delete(OutputPath, recursive: true);
            }
            if (Directory.Exists(TestOutputPath))
            {
                Directory.Delete(TestOutputPath, recursive: true);
            }

            var logger = new MSBuildTaskLogger(Log);

            ClangGenerator.Generate(ExtensionInterfacePath, OutputPath, TestOutputPath, logger);

            var api = LoadExtensionApi(ExtensionApiPath);
            if (api is null || string.IsNullOrWhiteSpace(api.Header.VersionFullName))
            {
                Log.LogError("Error parsing the Godot extension API dump.");
                return false;
            }

            Log.LogMessage(MessageImportance.High, $"Generating C# bindings for '{api.Header.VersionFullName}'.");

            BindingsGeneratorOptions options = new()
            {
                ValidateBindingsVersion = ValidateBindingsVersion,
            };

            BindingsGenerator.Generate(api, OutputPath, TestOutputPath, options, logger);

            return !Log.HasLoggedErrors;
        }
        catch (Exception e)
        {
            Log.LogErrorFromException(e, showStackTrace: true);
            return false;
        }
    }

    private static GodotApi? LoadExtensionApi(string path)
    {
        using var stream = File.OpenRead(path);
        return GodotApi.Deserialize(stream);
    }

    private sealed class ThrowingAssertionListener : TraceListener
    {
        public override void Write(string? message) { }
        public override void WriteLine(string? message) { }

        public override void Fail(string? message, string? detailMessage)
        {
            throw new InvalidOperationException($"Assertion failed: {message} {detailMessage}");
        }
    }
}
