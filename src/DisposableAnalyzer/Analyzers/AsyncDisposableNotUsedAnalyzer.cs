using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects IAsyncDisposable types used with synchronous disposal.
/// DISP011: Should use await using for IAsyncDisposable
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncDisposableNotUsedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.AsyncDisposableNotUsed,
        title: "Should use await using for IAsyncDisposable",
        messageFormat: "Type '{0}' implements IAsyncDisposable. Use 'await using' instead of 'using' for proper async disposal",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types implementing IAsyncDisposable should be disposed using 'await using' to ensure proper asynchronous cleanup.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register both syntax and operation-based analysis for maximum compatibility
        context.RegisterSyntaxNodeAction(AnalyzeUsingStatement, SyntaxKind.UsingStatement);
        context.RegisterOperationAction(AnalyzeUsingOperation, OperationKind.Using);
        // Also detect manual DisposeAsync() calls and suggest using await using instead
        context.RegisterOperationAction(AnalyzeInvocationOperation, OperationKind.Invocation);
    }

    private void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
    {
        var usingStatement = (UsingStatementSyntax)context.Node;

        // Check if this is an await using (which is correct)
        if (usingStatement.AwaitKeyword.Kind() == SyntaxKind.AwaitKeyword)
            return;

        // Get the type being disposed
        ITypeSymbol? disposableType = null;

        if (usingStatement.Declaration != null)
        {
            var semanticModel = context.SemanticModel;
            disposableType = semanticModel.GetTypeInfo(usingStatement.Declaration.Type).Type;
        }
        else if (usingStatement.Expression != null)
        {
            var semanticModel = context.SemanticModel;
            disposableType = semanticModel.GetTypeInfo(usingStatement.Expression).Type;
        }

        if (disposableType == null)
            return;

        // Check if type implements IAsyncDisposable
        if (DisposableHelper.IsAsyncDisposableType(disposableType))
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                usingStatement.UsingKeyword.GetLocation(),
                disposableType.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void AnalyzeUsingOperation(OperationAnalysisContext context)
    {
        var usingOperation = (IUsingOperation)context.Operation;

        // Check if this is await using (which is correct)
        if (usingOperation.IsAsynchronous)
            return;

        // Get the type being disposed
        var disposableType = usingOperation.Resources?.Type;
        if (disposableType == null)
            return;

        // Check if type implements IAsyncDisposable
        if (DisposableHelper.IsAsyncDisposableType(disposableType))
        {
            // Try to get the location of the type expression
            Location location;
            if (usingOperation.Resources is IVariableDeclarationGroupOperation declGroup &&
                declGroup.Declarations.FirstOrDefault()?.Initializer?.Value?.Syntax is ObjectCreationExpressionSyntax creation)
            {
                location = creation.Type.GetLocation();
            }
            else if (usingOperation.Resources?.Syntax is ObjectCreationExpressionSyntax directCreation)
            {
                location = directCreation.Type.GetLocation();
            }
            else
            {
                // Fallback to the whole using statement
                location = usingOperation.Syntax.GetLocation();
            }

            var diagnostic = Diagnostic.Create(
                Rule,
                location,
                disposableType.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void AnalyzeInvocationOperation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;

        // Check if this is a DisposeAsync() call
        if (invocation.TargetMethod.Name != "DisposeAsync")
            return;

        // Check if the instance implements IAsyncDisposable
        var instanceType = invocation.Instance?.Type;
        if (instanceType == null || !DisposableHelper.IsAsyncDisposableType(instanceType))
            return;

        // Check if this is being called on a local variable (not a field or parameter)
        // We only want to suggest await using for locals, not for disposal in Dispose methods, etc.
        if (invocation.Instance is not ILocalReferenceOperation)
            return;

        // Report diagnostic suggesting to use await using instead
        var diagnostic = Diagnostic.Create(
            Rule,
            invocation.Syntax.GetLocation(),
            instanceType.Name);
        context.ReportDiagnostic(diagnostic);
    }
}
