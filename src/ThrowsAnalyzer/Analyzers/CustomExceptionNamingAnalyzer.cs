using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Analyzer that detects custom exception types that don't follow naming conventions.
    ///
    /// .NET naming guidelines specify that exception types should:
    /// - End with the suffix "Exception"
    /// - Inherit from System.Exception or a derived type
    /// - Be public if they cross assembly boundaries
    ///
    /// This analyzer helps ensure consistent exception naming throughout the codebase.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CustomExceptionNamingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS028";

        private static readonly LocalizableString Title =
            "Custom exception doesn't follow naming convention";

        private static readonly LocalizableString MessageFormat =
            "Exception type '{0}' should end with 'Exception'";

        private static readonly LocalizableString Description =
            "Custom exception types should follow .NET naming conventions and end with 'Exception'. " +
            "This makes the code more readable and helps developers immediately recognize exception types.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Naming",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var namedType = (INamedTypeSymbol)context.Symbol;

            // Only analyze classes
            if (namedType.TypeKind != TypeKind.Class)
                return;

            // Check if this type inherits from System.Exception
            if (!InheritsFromException(namedType, context.Compilation))
                return;

            // Get the type name
            var typeName = namedType.Name;

            // Check if it ends with "Exception"
            if (typeName.EndsWith("Exception"))
                return;

            // Skip well-known base exception types from .NET
            if (IsWellKnownExceptionType(namedType, context.Compilation))
                return;

            // Report diagnostic
            var diagnostic = Diagnostic.Create(
                Rule,
                namedType.Locations.FirstOrDefault(),
                typeName);

            context.ReportDiagnostic(diagnostic);
        }

        private bool InheritsFromException(INamedTypeSymbol type, Compilation compilation)
        {
            var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
            if (exceptionType == null)
                return false;

            var current = type.BaseType;
            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, exceptionType))
                    return true;

                current = current.BaseType;
            }

            return false;
        }

        private bool IsWellKnownExceptionType(INamedTypeSymbol type, Compilation compilation)
        {
            // Check if this is a type from System namespace (framework exception)
            var namespaceName = type.ContainingNamespace?.ToDisplayString();
            if (namespaceName != null && namespaceName.StartsWith("System"))
                return true;

            // Check if this is exactly System.Exception or SystemException
            var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
            var systemExceptionType = compilation.GetTypeByMetadataName("System.SystemException");

            if (SymbolEqualityComparer.Default.Equals(type, exceptionType) ||
                SymbolEqualityComparer.Default.Equals(type, systemExceptionType))
                return true;

            return false;
        }
    }
}
