using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Godot.Analyzers;
using Godot.Common.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Godot.SourceGeneration;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class EventHandlerSuffixSuppressor : DiagnosticSuppressor
{
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create([
        Descriptors.GDSP0001_EventHandlerSuffix,
    ]);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            AnalyzeDiagnostic(context, diagnostic, context.CancellationToken);
        }
    }

    private static void AnalyzeDiagnostic(SuppressionAnalysisContext context, Diagnostic diagnostic, CancellationToken cancellationToken = default)
    {
        var location = diagnostic.Location;
        var root = location.SourceTree?.GetRoot(cancellationToken);
        var delegateDeclaration = root?
            .FindNode(location.SourceSpan)
            .DescendantNodesAndSelf()
            .OfType<DelegateDeclarationSyntax>()
            .FirstOrDefault();

        if (delegateDeclaration is null)
        {
            return;
        }

        var semanticModel = context.GetSemanticModel(delegateDeclaration.SyntaxTree);
        var delegateSymbol = semanticModel.GetDeclaredSymbol(delegateDeclaration, cancellationToken);
        if (delegateSymbol is null)
        {
            return;
        }

        if (!delegateSymbol.HasAttribute(KnownTypeNames.SignalAttribute))
        {
            return;
        }

        context.ReportSuppression(Suppression.Create(
            Descriptors.GDSP0001_EventHandlerSuffix,
            diagnostic
        ));
    }
}
