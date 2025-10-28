using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynAnalyzer.Core.Members;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Analyzer that detects the rethrow anti-pattern where 'throw ex;' is used instead of 'throw;'.
    /// Using 'throw ex;' resets the stack trace, losing valuable debugging information.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RethrowAntiPatternAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.RethrowAntiPattern);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeThrowStatement,
                SyntaxKind.ThrowStatement);
        }

        private static void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
        {
            var throwStmt = (ThrowStatementSyntax)context.Node;

            // Check if this is inside a catch block
            var catchClause = throwStmt.Ancestors()
                .OfType<CatchClauseSyntax>()
                .FirstOrDefault();

            if (catchClause == null)
                return;

            // Check if it's rethrowing the caught exception variable
            if (throwStmt.Expression is IdentifierNameSyntax identifier)
            {
                var catchDeclaration = catchClause.Declaration;
                if (catchDeclaration?.Identifier.Text == identifier.Identifier.Text)
                {
                    // This is "throw ex;" anti-pattern
                    var memberNode = GetContainingMember(throwStmt);
                    if (memberNode != null)
                    {
                        var memberName = ExecutableMemberHelper.GetMemberDisplayName(memberNode);
                        var diagnostic = Diagnostic.Create(
                            MethodThrowsDiagnosticsBuilder.RethrowAntiPattern,
                            throwStmt.GetLocation(),
                            memberName);

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the containing executable member for a syntax node.
        /// </summary>
        private static SyntaxNode GetContainingMember(SyntaxNode node)
        {
            return node.Ancestors()
                .FirstOrDefault(n => ExecutableMemberHelper.IsExecutableMember(n));
        }
    }
}
