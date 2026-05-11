using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Godot.BindingsGeneration;

internal sealed class FileBindingsGeneratorWriterFactory : IBindingsGeneratorWriterFactory
{
    public required string OutputDirectoryPath { get; init; }
    public string? TestOutputDirectoryPath { get; init; }

    [MemberNotNullWhen(true, nameof(TestOutputDirectoryPath))]
    public bool SupportsTestOutput => !string.IsNullOrEmpty(TestOutputDirectoryPath);

    public TextWriter CreateWriter(string pathHint, bool isTestOutput)
    {
        string baseDirectory = OutputDirectoryPath;
        if (isTestOutput)
        {
            Debug.Assert(SupportsTestOutput);
            baseDirectory = TestOutputDirectoryPath;
        }

        string filePath = Path.Combine(baseDirectory, pathHint);

        // Ensure the output directory exists.
        string? directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var stream = File.Create(filePath);
        return new StreamWriter(stream, leaveOpen: false);
    }
}
