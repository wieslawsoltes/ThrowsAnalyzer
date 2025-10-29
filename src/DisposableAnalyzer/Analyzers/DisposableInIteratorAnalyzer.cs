using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects disposable usage in iterator methods (yield return).
/// DISP015: Disposable in iterator method
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableInIteratorAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableInIterator,
        title: "Disposable in iterator method",
        messageFormat: "Disposable object '{0}' in iterator method will have deferred disposal. Consider extracting validation to a wrapper method",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Iterator methods defer execution, which can delay disposal of resources. Extract argument validation and disposal logic to a wrapper method.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationBlockStartAction(AnalyzeOperationBlockStart);
    }

    private void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext context)
    {
        if (context.OwningSymbol is not IMethodSymbol method)
            return;

        // Check if this is an iterator method
        if (!IsIteratorMethod(method))
            return;

        var disposableUsages = new List<ResourceUsage>();

        context.RegisterOperationAction(operationContext =>
        {
            if (operationContext.Operation is IVariableDeclaratorOperation declarator)
            {
                if (declarator.Initializer?.Value == null)
                    return;

                // Skip resources that are already in a using statement/declaration
                if (DisposableHelper.IsInUsingStatement(declarator.Syntax))
                    return;

                var resourceType = declarator.Symbol?.Type;
                if (resourceType == null || !DisposableHelper.IsAnyDisposableType(resourceType))
                    return;

                var location = declarator.Initializer.Value.Syntax?.GetLocation() ?? declarator.Syntax.GetLocation();
                var resourceDisplayName = resourceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                disposableUsages.Add(new ResourceUsage(location, resourceDisplayName));
            }

            // Also check for using statements (which are problematic in iterators)
            if (operationContext.Operation is IUsingOperation usingOp)
            {
                // Using/await using ensures deterministic disposal even in iterators
                return;
            }
        }, OperationKind.VariableDeclarator, OperationKind.Using);

        context.RegisterOperationBlockEndAction(blockEndContext =>
        {
            foreach (var usage in disposableUsages)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    usage.Location,
                    usage.DisplayName);
                blockEndContext.ReportDiagnostic(diagnostic);
            }
        });
    }

    private bool IsIteratorMethod(IMethodSymbol method)
    {
        // Check if return type is IEnumerable<T> or IEnumerator<T>
        var returnType = method.ReturnType;
        if (returnType is INamedTypeSymbol namedType)
        {
            var typeName = namedType.OriginalDefinition.ToDisplayString();
            return typeName == "System.Collections.Generic.IEnumerable<T>" ||
                   typeName == "System.Collections.Generic.IEnumerator<T>" ||
                   typeName == "System.Collections.IEnumerable" ||
                   typeName == "System.Collections.IEnumerator" ||
                   typeName == "System.Collections.Generic.IAsyncEnumerable<T>" ||
                   typeName == "System.Collections.Generic.IAsyncEnumerator<T>";
        }

        return false;
    }

    private readonly struct ResourceUsage
    {
        public ResourceUsage(Location location, string displayName)
        {
            Location = location;
            DisplayName = displayName;
        }

        public Location Location { get; }
        public string DisplayName { get; }
    }
}
