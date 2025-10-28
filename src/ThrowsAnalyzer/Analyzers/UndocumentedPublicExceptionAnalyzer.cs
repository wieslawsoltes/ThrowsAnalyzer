using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynAnalyzer.Core.Analysis.CallGraph;
using ThrowsAnalyzer.Analysis;

namespace ThrowsAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer that detects public API methods that throw undocumented exceptions.
    /// Reports THROWS019: "Public method '{0}' may throw {1}, but it is not documented"
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UndocumentedPublicExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS019";

        private static readonly LocalizableString Title = "Public API throws undocumented exception";
        private static readonly LocalizableString MessageFormat = "Public method '{0}' may throw {1}, but it is not documented";
        private static readonly LocalizableString Description = "Public API methods should document all exceptions they may throw using XML documentation comments with <exception> tags. This helps consumers of the API understand what exceptions to expect and handle.";
        private const string Category = "Documentation";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                // Create tracker once per compilation
                var tracker = new ExceptionPropagationTracker(
                    compilationContext.Compilation,
                    compilationContext.CancellationToken);

                compilationContext.RegisterSyntaxNodeAction(
                    nodeContext => AnalyzeMethod(nodeContext, tracker),
                    SyntaxKind.MethodDeclaration);

                compilationContext.RegisterSyntaxNodeAction(
                    nodeContext => AnalyzeConstructor(nodeContext, tracker),
                    SyntaxKind.ConstructorDeclaration);
            });
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, ExceptionPropagationTracker tracker)
        {
            var methodDecl = (MethodDeclarationSyntax)context.Node;

            // Get the method symbol
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken);
            if (methodSymbol == null)
                return;

            // Only analyze public methods
            if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
                return;

            // Only analyze methods in public types
            if (!IsPublicApi(methodSymbol))
                return;

            AnalyzeMemberForUndocumentedExceptions(
                context,
                tracker,
                methodSymbol,
                methodDecl.Identifier.GetLocation(),
                GetMethodDisplayName(methodSymbol));
        }

        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context, ExceptionPropagationTracker tracker)
        {
            var ctorDecl = (ConstructorDeclarationSyntax)context.Node;

            // Get the constructor symbol
            var ctorSymbol = context.SemanticModel.GetDeclaredSymbol(ctorDecl, context.CancellationToken);
            if (ctorSymbol == null)
                return;

            // Only analyze public constructors
            if (ctorSymbol.DeclaredAccessibility != Accessibility.Public)
                return;

            // Only analyze constructors in public types
            if (!IsPublicApi(ctorSymbol))
                return;

            AnalyzeMemberForUndocumentedExceptions(
                context,
                tracker,
                ctorSymbol,
                ctorDecl.Identifier.GetLocation(),
                $"{ctorSymbol.ContainingType?.Name}()");
        }

        private static void AnalyzeMemberForUndocumentedExceptions(
            SyntaxNodeAnalysisContext context,
            ExceptionPropagationTracker tracker,
            IMethodSymbol methodSymbol,
            Location location,
            string memberName)
        {
            // Analyze exception flow
            var flowInfoTask = tracker.AnalyzeMethodAsync(methodSymbol);
            var flowInfo = Task.Run(async () => await flowInfoTask).GetAwaiter().GetResult();

            // Check if method has unhandled exceptions
            if (!flowInfo.HasUnhandledExceptions)
                return;

            // Get documented exceptions from XML comments
            var documentedExceptions = GetDocumentedExceptions(methodSymbol);

            // Find undocumented exceptions
            var undocumentedExceptions = flowInfo.PropagatedExceptions
                .Select(ex => ex.ExceptionType)
                .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
                .Where(exType => !IsExceptionDocumented(exType, documentedExceptions))
                .ToList();

            if (undocumentedExceptions.Count == 0)
                return;

            // Report diagnostic
            var exceptionNames = undocumentedExceptions.Select(ex => ex.Name).ToList();
            var exceptionList = exceptionNames.Count == 1
                ? exceptionNames[0]
                : string.Join(", ", exceptionNames.Take(exceptionNames.Count - 1)) +
                  " and " + exceptionNames[exceptionNames.Count - 1];

            var diagnostic = Diagnostic.Create(
                Rule,
                location,
                memberName,
                exceptionList);

            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsPublicApi(IMethodSymbol method)
        {
            // Check if method is in a public type
            var containingType = method.ContainingType;
            while (containingType != null)
            {
                if (containingType.DeclaredAccessibility != Accessibility.Public)
                    return false;

                containingType = containingType.ContainingType;
            }

            return true;
        }

        private static ImmutableHashSet<string> GetDocumentedExceptions(IMethodSymbol method)
        {
            var xmlDoc = method.GetDocumentationCommentXml();
            if (string.IsNullOrWhiteSpace(xmlDoc))
                return ImmutableHashSet<string>.Empty;

            try
            {
                var doc = XDocument.Parse(xmlDoc);
                var exceptionElements = doc.Descendants("exception");

                var documentedTypes = exceptionElements
                    .Select(e => e.Attribute("cref")?.Value)
                    .Where(cref => !string.IsNullOrWhiteSpace(cref))
                    .Select(cref => ExtractTypeName(cref))
                    .ToImmutableHashSet();

                return documentedTypes;
            }
            catch
            {
                // If XML parsing fails, assume no exceptions are documented
                return ImmutableHashSet<string>.Empty;
            }
        }

        private static string ExtractTypeName(string cref)
        {
            // cref format: "T:System.ArgumentNullException" or "System.ArgumentNullException"
            if (cref.StartsWith("T:"))
            {
                cref = cref.Substring(2);
            }

            // Get just the type name (without namespace)
            var lastDot = cref.LastIndexOf('.');
            if (lastDot >= 0)
            {
                return cref.Substring(lastDot + 1);
            }

            return cref;
        }

        private static bool IsExceptionDocumented(ITypeSymbol exceptionType, ImmutableHashSet<string> documentedExceptions)
        {
            // Check if the exception type or any of its base types are documented
            var current = exceptionType;
            while (current != null && current.Name != "Object")
            {
                var typeName = current.Name;
                var fullTypeName = current.ToDisplayString();

                if (documentedExceptions.Contains(typeName) ||
                    documentedExceptions.Contains(fullTypeName))
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        private static string GetMethodDisplayName(IMethodSymbol method)
        {
            if (method.ContainingType != null)
            {
                return $"{method.ContainingType.Name}.{method.Name}";
            }
            return method.Name;
        }
    }
}
