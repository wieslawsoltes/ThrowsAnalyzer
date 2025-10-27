using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Analyzer that detects exceptions being used for control flow.
    ///
    /// Using exceptions for control flow is an anti-pattern because:
    /// - Exceptions are expensive (stack unwinding, allocation)
    /// - Makes code harder to understand and maintain
    /// - Violates principle of least surprise
    /// - Should use return values, out parameters, or Result<T> instead
    ///
    /// This analyzer detects patterns where exceptions are thrown and immediately caught
    /// in the same method, suggesting they're being used for control flow rather than
    /// exceptional circumstances.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExceptionControlFlowAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS027";

        private static readonly LocalizableString Title =
            "Exception used for control flow";

        private static readonly LocalizableString MessageFormat =
            "Exception '{0}' is thrown and caught in the same method - consider using return values instead";

        private static readonly LocalizableString Description =
            "Using exceptions for control flow is an anti-pattern. " +
            "Exceptions are expensive and should only be used for exceptional circumstances. " +
            "Consider using return values, out parameters, or the Result<T> pattern instead.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDecl = (MethodDeclarationSyntax)context.Node;
            if (methodDecl.Body == null)
                return;

            AnalyzeMethodBody(context, methodDecl.Body);
        }

        private void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
        {
            var localFunc = (LocalFunctionStatementSyntax)context.Node;
            if (localFunc.Body == null)
                return;

            AnalyzeMethodBody(context, localFunc.Body);
        }

        private void AnalyzeMethodBody(SyntaxNodeAnalysisContext context, BlockSyntax body)
        {
            // Find all try-catch blocks in the method
            var tryStatements = body.DescendantNodes()
                .OfType<TryStatementSyntax>()
                .ToList();

            foreach (var tryStmt in tryStatements)
            {
                // Find throw statements in the try block
                var throwsInTry = tryStmt.Block.DescendantNodes()
                    .OfType<ThrowStatementSyntax>()
                    .Where(t => t.Expression != null) // Exclude rethrows
                    .ToList();

                if (throwsInTry.Count == 0)
                    continue;

                // Check if these exceptions are caught in the same try-catch
                foreach (var throwStmt in throwsInTry)
                {
                    var thrownType = context.SemanticModel.GetTypeInfo(throwStmt.Expression).Type;
                    if (thrownType == null)
                        continue;

                    // Check if this exception is caught by any catch clause
                    foreach (var catchClause in tryStmt.Catches)
                    {
                        var caughtType = GetCaughtExceptionType(catchClause, context.SemanticModel);

                        // General catch or matching type
                        if (caughtType == null ||
                            IsAssignableTo(thrownType, caughtType, context.SemanticModel.Compilation))
                        {
                            // This looks like control flow - exception thrown and immediately caught
                            var diagnostic = Diagnostic.Create(
                                Rule,
                                throwStmt.GetLocation(),
                                thrownType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));

                            context.ReportDiagnostic(diagnostic);
                            break; // Only report once per throw
                        }
                    }
                }
            }
        }

        private ITypeSymbol GetCaughtExceptionType(CatchClauseSyntax catchClause, SemanticModel semanticModel)
        {
            if (catchClause.Declaration == null)
                return null; // General catch

            var typeInfo = semanticModel.GetTypeInfo(catchClause.Declaration.Type);
            return typeInfo.Type;
        }

        private bool IsAssignableTo(ITypeSymbol derivedType, ITypeSymbol baseType, Compilation compilation)
        {
            if (derivedType == null || baseType == null)
                return false;

            // Check direct equality
            if (SymbolEqualityComparer.Default.Equals(derivedType, baseType))
                return true;

            // Check inheritance chain
            var current = derivedType.BaseType;
            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    return true;

                current = current.BaseType;
            }

            return false;
        }
    }
}
