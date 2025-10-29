using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects when disposal responsibility is not clearly propagated
/// either because fields are left undisposed or locals receiving disposables are
/// neither disposed nor returned.
/// DISP021: Disposal not propagated.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposalNotPropagatedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposalNotPropagated,
        title: "Disposal responsibility not propagated",
        messageFormat: "Type '{0}' does not dispose field '{1}'",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Disposables stored in fields or produced by helper methods must be disposed locally or their ownership must be transferred back to the caller.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterOperationBlockStartAction(AnalyzeMethod);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol)
            return;

        if (typeSymbol.TypeKind != TypeKind.Class && typeSymbol.TypeKind != TypeKind.Struct)
            return;

        var disposableFields = typeSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsStatic && DisposableHelper.IsAnyDisposableType(f.Type))
            .ToList();

        if (disposableFields.Count == 0)
            return;

        var disposeLikeMethods = typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(IsDisposeLike)
            .ToList();

        foreach (var field in disposableFields)
        {
            if (disposeLikeMethods.Any(method => MethodDisposesField(method, field, context.CancellationToken)))
                continue;

            var location = typeSymbol.Locations.FirstOrDefault() ?? field.Locations.FirstOrDefault();
            var fieldReference = field.DeclaringSyntaxReferences.FirstOrDefault();
            var fieldLocation = fieldReference != null
                ? Location.Create(fieldReference.SyntaxTree, fieldReference.Span)
                : field.Locations.FirstOrDefault() ?? location;
            context.ReportDiagnostic(Diagnostic.Create(Rule, fieldLocation, field.Name, typeSymbol.Name));
        }
    }

    private static void AnalyzeMethod(OperationBlockStartAnalysisContext context)
    {
        if (context.OwningSymbol is not IMethodSymbol method ||
            method.MethodKind is not MethodKind.Ordinary && method.MethodKind is not MethodKind.PropertySet)
        {
            return;
        }

        var locals = new Dictionary<ILocalSymbol, LocalUsage>(SymbolEqualityComparer.Default);

        context.RegisterOperationAction(operationContext =>
        {
            if (operationContext.Operation is not IVariableDeclaratorOperation declarator)
                return;

            var local = declarator.Symbol;
            if (!DisposableHelper.IsAnyDisposableType(local.Type))
                return;

            if (DisposableHelper.IsInUsingStatement(declarator.Syntax))
                return;

            string? source = null;
            var value = declarator.Initializer?.Value;

            switch (value)
            {
                case IObjectCreationOperation:
                    source = local.Type.Name;
                    break;
                case IInvocationOperation invocation when DisposableHelper.IsAnyDisposableType(invocation.Type):
                    source = invocation.TargetMethod.Name;
                    break;
            }

            if (source == null)
                return;

            locals[local] = new LocalUsage(
                declarator.Syntax.GetLocation(),
                source);
        }, OperationKind.VariableDeclarator);

        context.RegisterOperationAction(operationContext =>
        {
            if (!DisposableHelper.IsDisposalCall(operationContext.Operation, out _))
                return;

            var targetLocal = GetLocalFromInvocation(operationContext.Operation);
            if (targetLocal != null && locals.TryGetValue(targetLocal, out var usage))
            {
                usage.IsDisposed = true;
            }
        }, OperationKind.Invocation, OperationKind.ConditionalAccess);

        context.RegisterOperationAction(operationContext =>
        {
            if (operationContext.Operation is IReturnOperation returnOp &&
                returnOp.ReturnedValue is ILocalReferenceOperation localRef &&
                locals.TryGetValue(localRef.Local, out var usage))
            {
                usage.IsReturned = true;
            }
        }, OperationKind.Return);

        context.RegisterOperationBlockEndAction(blockEndContext =>
        {
            foreach (var kvp in locals)
            {
                var local = kvp.Key;
                var usage = kvp.Value;

                if (usage.IsDisposed || usage.IsReturned)
                    continue;

                blockEndContext.ReportDiagnostic(
                    Diagnostic.Create(Rule, usage.Location, local.Name, usage.Source));
            }
        });
    }

    private static bool IsDisposeLike(IMethodSymbol method)
    {
        if (method is null)
            return false;

        if (method.Name == "Dispose" && method.Parameters.Length == 0)
            return true;

        if (DisposableHelper.IsDisposeBoolMethod(method))
            return true;

        if (method.Name == "DisposeAsync" && method.Parameters.Length == 0)
            return true;

        return false;
    }

    private static bool MethodDisposesField(IMethodSymbol method, IFieldSymbol field, CancellationToken cancellationToken)
    {
        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax(cancellationToken) is not MethodDeclarationSyntax methodSyntax)
                continue;

            // Check standard block body
            if (methodSyntax.Body != null && DisposesField(methodSyntax.Body, field.Name))
                return true;

            // Check expression-bodied members
            if (methodSyntax.ExpressionBody != null &&
                DisposesField(methodSyntax.ExpressionBody.Expression, field.Name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool DisposesField(SyntaxNode node, string fieldName)
    {
        foreach (var invocation in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == fieldName &&
                    memberAccess.Name.Identifier.Text == "Dispose")
                {
                    return true;
                }
            }
            else if (invocation.Expression is MemberBindingExpressionSyntax memberBinding &&
                     memberBinding.Name.Identifier.Text == "Dispose")
            {
                if (invocation.Parent is ConditionalAccessExpressionSyntax conditional &&
                    conditional.Expression is IdentifierNameSyntax conditionalIdentifier &&
                    conditionalIdentifier.Identifier.Text == fieldName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static ILocalSymbol? GetLocalFromInvocation(IOperation operation)
    {
        return operation switch
        {
            IInvocationOperation invocation when invocation.Instance is ILocalReferenceOperation localRef => localRef.Local,
            IConditionalAccessOperation conditional when conditional.Operation is ILocalReferenceOperation localRef &&
                                                         conditional.WhenNotNull is IInvocationOperation => localRef.Local,
            _ => null
        };
    }

    private sealed class LocalUsage
    {
        public LocalUsage(Location location, string source)
        {
            Location = location;
            Source = source;
        }

        public Location Location { get; }
        public string Source { get; }
        public bool IsDisposed { get; set; }
        public bool IsReturned { get; set; }
    }
}
