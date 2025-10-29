using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects collections of disposable objects without proper disposal.
/// DISP020: Collection of disposables not disposed
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableCollectionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableCollection,
        title: "Collection of disposables not disposed",
        messageFormat: "Field '{0}' in type '{1}' is a collection of disposable objects. Ensure all elements are disposed",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Collections containing disposable objects should dispose all elements when the collection is no longer needed.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
    }

    private void AnalyzeField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;

        // Skip static fields
        if (field.IsStatic)
            return;

        // Check if field is a collection type
        if (!IsCollectionType(field.Type, out var elementType))
            return;

        // Check if element type is disposable
        if (!DisposableHelper.IsAnyDisposableType(elementType))
            return;

        // Check if containing type implements IDisposable
        var containingType = field.ContainingType;
        var implementsDisposable =
            DisposableHelper.IsDisposableType(containingType) ||
            DisposableHelper.IsAsyncDisposableType(containingType);

        if (implementsDisposable && IsCollectionDisposed(containingType, field, context))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            Rule,
            field.Locations.FirstOrDefault(),
            field.Name,
            containingType.Name);
        context.ReportDiagnostic(diagnostic);
    }

    private bool IsCollectionType(ITypeSymbol type, out ITypeSymbol? elementType)
    {
        elementType = null;

        // Array types
        if (type is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }

        if (type is not INamedTypeSymbol namedType)
            return false;

        // Check for common collection types
        var originalDefinition = namedType.OriginalDefinition.ToDisplayString();

        // Generic collections
        if (originalDefinition.StartsWith("System.Collections.Generic.List<") ||
            originalDefinition.StartsWith("System.Collections.Generic.IList<") ||
            originalDefinition.StartsWith("System.Collections.Generic.ICollection<") ||
            originalDefinition.StartsWith("System.Collections.Generic.IEnumerable<") ||
            originalDefinition.StartsWith("System.Collections.Generic.HashSet<") ||
            originalDefinition.StartsWith("System.Collections.Generic.LinkedList<") ||
            originalDefinition.StartsWith("System.Collections.Generic.Queue<") ||
            originalDefinition.StartsWith("System.Collections.Generic.Stack<") ||
            originalDefinition.StartsWith("System.Collections.ObjectModel.Collection<") ||
            originalDefinition.StartsWith("System.Collections.ObjectModel.ObservableCollection<"))
        {
            if (namedType.TypeArguments.Length > 0)
            {
                elementType = namedType.TypeArguments[0];
                return true;
            }
        }

        return false;
    }

    private bool IsCollectionDisposed(
        INamedTypeSymbol containingType,
        IFieldSymbol field,
        SymbolAnalysisContext context)
    {
        var disposeMethods = GetCandidateDisposeMethods(containingType);
        if (disposeMethods.Length == 0)
        {
            // No dispose methods defined - treat as not disposed
            return false;
        }

        foreach (var disposeMethod in disposeMethods)
        {
            foreach (var syntaxRef in disposeMethod.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax(context.CancellationToken);
                if (syntax == null)
                    continue;

                var semanticModel = context.Compilation.GetSemanticModel(syntax.SyntaxTree);
                if (HasDisposalLoop(syntax, semanticModel, field, context.CancellationToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private ImmutableArray<IMethodSymbol> GetCandidateDisposeMethods(INamedTypeSymbol containingType)
    {
        var builder = ImmutableArray.CreateBuilder<IMethodSymbol>();

        var disposeMethod = DisposableHelper.GetDisposeMethod(containingType);
        if (disposeMethod != null)
        {
            builder.Add(disposeMethod);
        }

        var disposeBoolMethod = containingType.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => DisposableHelper.IsDisposeBoolMethod(m));
        if (disposeBoolMethod != null)
        {
            builder.Add(disposeBoolMethod);
        }

        var disposeAsyncMethod = DisposableHelper.GetDisposeAsyncMethod(containingType);
        if (disposeAsyncMethod != null)
        {
            builder.Add(disposeAsyncMethod);
        }

        return builder.ToImmutable();
    }

    private bool HasDisposalLoop(
        SyntaxNode methodSyntax,
        SemanticModel semanticModel,
        IFieldSymbol field,
        CancellationToken cancellationToken)
    {
        foreach (var forEach in methodSyntax.DescendantNodes().OfType<ForEachStatementSyntax>())
        {
            var expressionSymbol = semanticModel.GetSymbolInfo(forEach.Expression, cancellationToken).Symbol;
            var matchesBySymbol = SymbolEqualityComparer.Default.Equals(expressionSymbol, field);

            var matchesByName = forEach.Expression is IdentifierNameSyntax identifier &&
                                identifier.Identifier.Text == field.Name;

            if (!matchesBySymbol && !matchesByName)
                continue;

            if (ContainsDisposeInvocation(forEach.Statement, semanticModel, cancellationToken))
                return true;
        }

        return false;
    }

    private bool ContainsDisposeInvocation(
        StatementSyntax statement,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        foreach (var invocation in statement.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method)
            {
                if (method.Name is "Dispose" or "DisposeAsync" && method.Parameters.Length == 0)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
