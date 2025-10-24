using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ThrowsAnalyzer.Core;
using ThrowsAnalyzer.Configuration;

namespace ThrowsAnalyzer
{

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TryCatchAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.MethodContainsTryCatch);

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
        if (!AnalyzerOptionsReader.IsAnalyzerEnabled(context.Options, context.Node.SyntaxTree, "try_catch"))
        {
            return;
        }

        // Check if this member type is enabled in configuration
        var memberTypeKey = AnalyzerOptionsReader.GetMemberTypeKey(node.Kind());
        if (!AnalyzerOptionsReader.IsMemberTypeEnabled(context.Options, context.Node.SyntaxTree, memberTypeKey))
        {
            return;
        }

        // Use generic detector
        if (!ExecutableMemberHelper.IsExecutableMember(node))
        {
            return;
        }

        if (TryCatchDetector.HasTryCatchBlocks(node))
        {
            // Get appropriate location and name based on member type
            var location = AnalyzerHelper.GetMemberLocation(node);
            var memberName = ExecutableMemberHelper.GetMemberDisplayName(node);

            var diagnostic = Diagnostic.Create(
                MethodThrowsDiagnosticsBuilder.MethodContainsTryCatch,
                location,
                memberName);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
}
