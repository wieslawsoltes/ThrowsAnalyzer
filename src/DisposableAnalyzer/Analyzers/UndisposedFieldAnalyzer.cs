using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects disposable fields that are not disposed in the containing type.
/// DISP002: Disposable field not disposed in type
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UndisposedFieldAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.UndisposedField,
        title: "Disposable field not disposed in type",
        messageFormat: "Field '{0}' implements IDisposable but is not disposed. Type should implement IDisposable and call field.Dispose()",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types with disposable fields should implement IDisposable and dispose the fields in the Dispose method.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Create state to track disposed fields and types across the compilation
            var disposedFieldsPerType = new System.Collections.Concurrent.ConcurrentDictionary<INamedTypeSymbol, HashSet<IFieldSymbol>>(SymbolEqualityComparer.Default);
            var typesToAnalyze = new System.Collections.Concurrent.ConcurrentBag<INamedTypeSymbol>();

            // Register operation block action to find disposed fields
            compilationContext.RegisterOperationBlockAction(operationContext =>
            {
                AnalyzeOperationBlock(operationContext, disposedFieldsPerType);
            });

            // Register symbol action to collect types to analyze
            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                if (symbolContext.Symbol is INamedTypeSymbol namedType)
                {
                    typesToAnalyze.Add(namedType);
                }
            }, SymbolKind.NamedType);

            // Register compilation end action to report diagnostics after all operations are analyzed
            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                foreach (var namedType in typesToAnalyze)
                {
                    AnalyzeNamedTypeForDiagnostics(endContext, namedType, disposedFieldsPerType);
                }
            });
        });
    }

    private void AnalyzeNamedTypeForDiagnostics(CompilationAnalysisContext context, INamedTypeSymbol namedType,
        System.Collections.Concurrent.ConcurrentDictionary<INamedTypeSymbol, HashSet<IFieldSymbol>> disposedFieldsPerType)
    {
        // Skip interfaces, enums, delegates
        if (namedType.TypeKind != TypeKind.Class && namedType.TypeKind != TypeKind.Struct)
            return;

        // Get all disposable instance fields (static fields don't need to be disposed)
        var disposableFields = DisposableHelper.GetDisposableFields(namedType)
            .Where(f => !f.IsStatic)
            .ToList();
        if (!disposableFields.Any())
            return;

        // Check if type implements IDisposable
        bool implementsDisposable = DisposableHelper.IsDisposableType(namedType);
        bool implementsAsyncDisposable = DisposableHelper.IsAsyncDisposableType(namedType);

        // Only analyze types that implement IDisposable/IAsyncDisposable
        // Types that don't implement it are handled by DisposableNotImplementedAnalyzer (DISP007)
        if (!implementsDisposable && !implementsAsyncDisposable)
        {
            return;
        }

        // Type implements IDisposable - check if fields are disposed
        var disposeMethod = DisposableHelper.GetDisposeMethod(namedType);
        var disposeAsyncMethod = DisposableHelper.GetDisposeAsyncMethod(namedType);

        if (disposeMethod == null && disposeAsyncMethod == null)
        {
            // Type implements IDisposable but has no Dispose method
            foreach (var field in disposableFields)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    field.Locations.FirstOrDefault(),
                    field.Name);
                context.ReportDiagnostic(diagnostic);
            }
            return;
        }

        // Check which fields are disposed in Dispose method(s)
        var disposedFields = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);

        // Get disposed fields from the shared state (populated by OperationBlockAction)
        if (disposedFieldsPerType.TryGetValue(namedType, out var fields))
        {
            disposedFields.UnionWith(fields);
        }

        // Report fields that are not disposed
        foreach (var field in disposableFields)
        {
            if (!disposedFields.Contains(field))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    field.Locations.FirstOrDefault(),
                    field.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private void AnalyzeOperationBlock(OperationBlockAnalysisContext context,
        System.Collections.Concurrent.ConcurrentDictionary<INamedTypeSymbol, HashSet<IFieldSymbol>> disposedFieldsPerType)
    {
        // Only analyze Dispose and DisposeAsync methods
        var method = context.OwningSymbol as IMethodSymbol;
        if (method == null)
            return;

        // Check if this is a Dispose() or DisposeAsync() method
        var isDisposeMethod = method.Name == "Dispose" && method.Parameters.Length == 0;
        var isDisposeBoolMethod = DisposableHelper.IsDisposeBoolMethod(method);
        var isDisposeAsyncMethod = method.Name == "DisposeAsync" && method.Parameters.Length == 0;

        if (!isDisposeMethod && !isDisposeBoolMethod && !isDisposeAsyncMethod)
            return;

        // Track disposed fields in this method
        var disposedFields = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);

        // Analyze all operations in the method
        foreach (var operation in context.OperationBlocks)
        {
            AnalyzeOperationForFieldDisposal(operation, disposedFields);
        }

        // Store the disposed fields in the shared state for use by AnalyzeNamedType
        var containingType = method.ContainingType;
        if (containingType != null && disposedFields.Any())
        {
            var fields = disposedFieldsPerType.GetOrAdd(containingType,
                _ => new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default));

            lock (fields)
            {
                fields.UnionWith(disposedFields);
            }
        }
    }


    private void AnalyzeOperationForFieldDisposal(IOperation operation, HashSet<IFieldSymbol> disposedFields)
    {
        // Look for field.Dispose() calls
        if (operation is IInvocationOperation invocation)
        {
            if (invocation.Instance is IFieldReferenceOperation fieldRef)
            {
                if (DisposableHelper.IsDisposalCall(invocation, out _))
                {
                    disposedFields.Add(fieldRef.Field);
                }
            }

            // Also check for conditional access (field?.Dispose())
            if (invocation.Parent is IConditionalAccessOperation conditionalAccess &&
                conditionalAccess.Operation is IFieldReferenceOperation condFieldRef)
            {
                if (DisposableHelper.IsDisposalCall(invocation, out _))
                {
                    disposedFields.Add(condFieldRef.Field);
                }
            }
        }

        // Recursively check child operations
        foreach (var child in operation.Children)
        {
            AnalyzeOperationForFieldDisposal(child, disposedFields);
        }
    }
}
