using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Godot.UpgradeAssistant.Tests;

internal static partial class CSharpCodeFixVerifier<TCodeFix, TAnalyzer>
    where TCodeFix : CodeFixProvider, new()
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        public Test(MetadataReference[] beforeReferences, MetadataReference[] afterReferences)
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100;

            TestState.AdditionalReferences.AddRange(beforeReferences);
            FixedState.AdditionalReferences.AddRange(afterReferences);
        }

        public void SetAnalyzerConfigText(string configText)
        {
            var sourceText = SourceText.From(configText);
            TestState.AnalyzerConfigFiles.Add(("/.globalconfig", sourceText));
        }
    }

    public static Task Verify(string sources, string fixedSources, MetadataReference[] beforeReferences, MetadataReference[] afterReferences)
    {
        return MakeVerifier(sources, fixedSources, beforeReferences, afterReferences).RunAsync();
    }

    public static Test MakeVerifier(string source, string results, MetadataReference[] beforeReferences, MetadataReference[] afterReferences)
    {
        var verifier = new Test(beforeReferences, afterReferences);

        verifier.TestCode = File.ReadAllText(Path.Combine(Constants.SourceFolderPath, source)).ReplaceLineEndings();
        verifier.FixedCode = File.ReadAllText(Path.Combine(Constants.GeneratedSourceFolderPath, results)).ReplaceLineEndings();

        return verifier;
    }

    public static BatchTest MakeBatchVerifier(string source, string results, MetadataReference[] beforeReferences, MetadataReference[] afterReferences)
    {
        var verifier = new BatchTest(beforeReferences, afterReferences);

        verifier.TestCode = File.ReadAllText(Path.Combine(Constants.SourceFolderPath, source)).ReplaceLineEndings();
        verifier.FixedCode = File.ReadAllText(Path.Combine(Constants.GeneratedSourceFolderPath, results)).ReplaceLineEndings();

        return verifier;
    }
}
