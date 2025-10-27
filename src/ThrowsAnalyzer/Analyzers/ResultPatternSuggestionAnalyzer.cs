using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Analyzer that suggests using Result<T> pattern instead of exceptions for expected errors.
    ///
    /// The Result<T> pattern is useful for:
    /// - Validation errors that are expected
    /// - Operations that frequently fail in normal scenarios
    /// - Performance-critical code
    /// - Making error handling explicit in method signatures
    ///
    /// This analyzer detects methods that throw exceptions for what appear to be
    /// expected error conditions (validation, parsing, etc.) and suggests considering
    /// the Result<T> pattern instead.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ResultPatternSuggestionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS030";

        private static readonly LocalizableString Title =
            "Consider using Result<T> pattern for expected errors";

        private static readonly LocalizableString MessageFormat =
            "Method '{0}' throws '{1}' for expected validation - consider using Result<T> pattern";

        private static readonly LocalizableString Description =
            "Methods that throw exceptions for expected error conditions (validation, parsing, etc.) " +
            "may benefit from using the Result<T> pattern. This makes error handling explicit, " +
            "improves performance, and clarifies the method's contract.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Design",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDecl = (MethodDeclarationSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl);

            if (methodSymbol == null || methodDecl.Body == null)
                return;

            // Check if method name suggests validation or parsing
            if (!IsValidationOrParsingMethod(methodSymbol))
                return;

            // Find all throw statements
            var throws = methodDecl.Body.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Where(t => t.Expression != null)
                .ToList();

            if (throws.Count == 0)
                return;

            // Analyze the thrown exceptions
            var validationExceptions = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var throwStmt in throws)
            {
                var exceptionType = context.SemanticModel.GetTypeInfo(throwStmt.Expression).Type;
                if (exceptionType == null)
                    continue;

                // Check if this is a validation-related exception
                if (IsValidationException(exceptionType))
                {
                    validationExceptions.Add(exceptionType);
                }
            }

            // If we found validation exceptions, suggest Result<T>
            if (validationExceptions.Count > 0)
            {
                // Report on the method declaration
                var exceptionNames = string.Join(", ", validationExceptions.Select(e =>
                    e.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));

                var diagnostic = Diagnostic.Create(
                    Rule,
                    methodDecl.Identifier.GetLocation(),
                    methodSymbol.Name,
                    exceptionNames);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool IsValidationOrParsingMethod(IMethodSymbol method)
        {
            var methodName = method.Name.ToLower();

            // Check method name patterns
            return methodName.Contains("validate") ||
                   methodName.Contains("check") ||
                   methodName.Contains("verify") ||
                   methodName.Contains("parse") ||
                   methodName.Contains("convert") ||
                   methodName.Contains("process") ||
                   methodName.Contains("create") ||
                   methodName.StartsWith("is") ||
                   methodName.StartsWith("can");
        }

        private bool IsValidationException(ITypeSymbol exceptionType)
        {
            var typeName = exceptionType.Name.ToLower();

            // Common validation exception types
            return typeName.Contains("argument") ||        // ArgumentException, ArgumentNullException
                   typeName.Contains("invalid") ||         // InvalidOperationException, InvalidDataException
                   typeName.Contains("format") ||          // FormatException
                   typeName.Contains("validation") ||      // Custom validation exceptions
                   typeName.Contains("notfound") ||        // NotFound exceptions
                   typeName.Contains("notsupported");      // NotSupportedException
        }
    }
}
