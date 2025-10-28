using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynAnalyzer.Core.Members;
using RoslynAnalyzer.Core.Configuration;
using RoslynAnalyzer.Core.Helpers;
using ThrowsAnalyzer.Analyzers;
using ThrowsAnalyzer.Configuration;

namespace ThrowsAnalyzer
{

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnhandledThrowsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.MethodContainsUnhandledThrow);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register for all executable member types
        context.RegisterSyntaxNodeAction(AnalyzeExecutableMember,
            AnalyzerConfiguration.ExecutableMemberSyntaxKinds);
    }

    private static void AnalyzeExecutableMember(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;

        // Check if this analyzer is enabled
        if (!ThrowsAnalyzerOptions.IsAnalyzerEnabled(context.Options, context.Node.SyntaxTree, "unhandled_throw"))
        {
            return;
        }

        // Check if this member type is enabled in configuration
        var memberTypeKey = ThrowsAnalyzerOptions.GetMemberTypeKey(node.Kind());
        if (!ThrowsAnalyzerOptions.IsMemberTypeEnabled(context.Options, context.Node.SyntaxTree, memberTypeKey))
        {
            return;
        }

        // Use generic detector
        if (!ExecutableMemberHelper.IsExecutableMember(node))
        {
            return;
        }

        if (UnhandledThrowDetector.HasUnhandledThrows(node))
        {
            // Get appropriate location and name based on member type
            var location = DiagnosticHelpers.GetMemberLocation(node);
            var memberName = ExecutableMemberHelper.GetMemberDisplayName(node);

            var diagnostic = Diagnostic.Create(
                MethodThrowsDiagnosticsBuilder.MethodContainsUnhandledThrow,
                location,
                memberName);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
}
