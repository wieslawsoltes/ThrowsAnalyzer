using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Analyzer that detects exceptions being thrown in potential hot paths.
    ///
    /// Throwing exceptions in hot paths (loops, frequently-called methods) can cause
    /// significant performance degradation because:
    /// - Exception creation is expensive (stack trace capture, allocation)
    /// - Stack unwinding has overhead
    /// - Can prevent JIT optimizations
    ///
    /// This analyzer detects:
    /// - Exceptions thrown inside loops (for, foreach, while, do-while)
    /// - Exceptions in methods with performance-critical indicators
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExceptionInHotPathAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS029";

        private static readonly LocalizableString Title =
            "Exception thrown in potential hot path";

        private static readonly LocalizableString MessageFormat =
            "Exception '{0}' is thrown in {1} - consider performance implications";

        private static readonly LocalizableString Description =
            "Throwing exceptions in loops or frequently-called methods can cause significant performance degradation. " +
            "Consider using return values, Try patterns, or other error handling mechanisms for expected error conditions.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Performance",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeThrowStatement, SyntaxKind.ThrowStatement);
            context.RegisterSyntaxNodeAction(AnalyzeThrowExpression, SyntaxKind.ThrowExpression);
        }

        private void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
        {
            var throwStmt = (ThrowStatementSyntax)context.Node;
            if (throwStmt.Expression == null)
                return; // Rethrow

            AnalyzeThrow(context, throwStmt, throwStmt.Expression);
        }

        private void AnalyzeThrowExpression(SyntaxNodeAnalysisContext context)
        {
            var throwExpr = (ThrowExpressionSyntax)context.Node;
            AnalyzeThrow(context, throwExpr, throwExpr.Expression);
        }

        private void AnalyzeThrow(SyntaxNodeAnalysisContext context, SyntaxNode throwNode, ExpressionSyntax exceptionExpression)
        {
            // Get exception type
            var exceptionType = context.SemanticModel.GetTypeInfo(exceptionExpression).Type;
            if (exceptionType == null)
                return;

            // Check if throw is inside a loop
            var loopContext = GetLoopContext(throwNode);
            if (loopContext != null)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    throwNode.GetLocation(),
                    exceptionType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    loopContext);

                context.ReportDiagnostic(diagnostic);
                return;
            }

            // Check if throw is in a performance-critical method
            var methodContext = GetPerformanceCriticalContext(throwNode);
            if (methodContext != null)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    throwNode.GetLocation(),
                    exceptionType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    methodContext);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private string GetLoopContext(SyntaxNode node)
        {
            var current = node.Parent;
            while (current != null)
            {
                switch (current)
                {
                    case ForStatementSyntax _:
                        return "for loop";

                    case ForEachStatementSyntax _:
                        return "foreach loop";

                    case WhileStatementSyntax _:
                        return "while loop";

                    case DoStatementSyntax _:
                        return "do-while loop";

                    // Stop at method boundary
                    case MethodDeclarationSyntax _:
                    case LocalFunctionStatementSyntax _:
                    case AnonymousFunctionExpressionSyntax _:
                        return null;
                }

                current = current.Parent;
            }

            return null;
        }

        private string GetPerformanceCriticalContext(SyntaxNode node)
        {
            // Find containing method
            var current = node.Parent;
            while (current != null)
            {
                if (current is MethodDeclarationSyntax methodDecl)
                {
                    // Check for performance-related attributes or naming
                    var methodName = methodDecl.Identifier.Text.ToLower();

                    // Methods with these names/patterns are likely performance-critical
                    if (methodName.Contains("parse") || methodName.Contains("tryparse"))
                        return "parsing method";

                    if (methodName.Contains("convert") || methodName.Contains("transform"))
                        return "conversion method";

                    if (methodName.Contains("validate") || methodName.Contains("check"))
                        return "validation method (consider returning bool)";

                    break;
                }

                if (current is LocalFunctionStatementSyntax)
                    break;

                current = current.Parent;
            }

            return null;
        }
    }
}
