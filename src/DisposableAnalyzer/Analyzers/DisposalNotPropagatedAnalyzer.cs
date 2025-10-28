using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;
using RoslynAnalyzer.Core.Analysis.CallGraph;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects when disposable resources are created but not returned or disposed across method boundaries.
/// DISP021: Disposal not propagated
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposalNotPropagatedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposalNotPropagated,
        title: "Disposal responsibility not propagated",
        messageFormat: "Disposable '{0}' created in '{1}' is not disposed and not returned. Consider disposing or returning it",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a method creates a disposable resource but doesn't dispose it, the resource must be returned to transfer ownership to the caller.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterCompilationEndAction(async compilationEndContext =>
            {
                var builder = new CallGraphBuilder(compilationEndContext.Compilation, compilationEndContext.CancellationToken);
                var callGraph = await builder.BuildAsync().ConfigureAwait(false);
                AnalyzeCallGraph(compilationEndContext, callGraph);
            });
        });
    }

    private void AnalyzeCallGraph(CompilationAnalysisContext context, CallGraph callGraph)
    {
        foreach (var node in callGraph.Nodes)
        {
            var method = node.Method;

            // Skip if method has no body (extern, abstract, interface)
            if (method.DeclaringSyntaxReferences.Length == 0)
                continue;

            var syntaxRef = method.DeclaringSyntaxReferences[0];
            var syntax = syntaxRef.GetSyntax(context.CancellationToken);
            var semanticModel = context.Compilation.GetSemanticModel(syntax.SyntaxTree);

            if (semanticModel == null)
                continue;

            var operation = semanticModel.GetOperation(syntax, context.CancellationToken);
            if (operation == null)
                continue;

            // Track disposables created in this method
            var createdDisposables = new System.Collections.Generic.List<(string name, ITypeSymbol type, Location location)>();
            var disposedSymbols = new System.Collections.Generic.HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var returnedSymbols = new System.Collections.Generic.HashSet<ISymbol>(SymbolEqualityComparer.Default);

            // Find all disposable creations
            foreach (var descendant in operation.Descendants())
            {
                if (descendant is IObjectCreationOperation creation)
                {
                    if (DisposableHelper.IsAnyDisposableType(creation.Type))
                    {
                        // Check if assigned to local or parameter
                        if (creation.Parent is IAssignmentOperation assignment &&
                            assignment.Target is ILocalReferenceOperation localRef)
                        {
                            createdDisposables.Add((localRef.Local.Name, creation.Type, creation.Syntax.GetLocation()));
                        }
                        else if (creation.Parent is IVariableInitializerOperation initializer &&
                                 initializer.Parent is IVariableDeclaratorOperation declarator)
                        {
                            createdDisposables.Add((declarator.Symbol.Name, creation.Type, creation.Syntax.GetLocation()));
                        }
                    }
                }

                // Track disposals
                if (descendant is IInvocationOperation invocation)
                {
                    if (DisposableHelper.IsDisposalCall(invocation, out _))
                    {
                        if (invocation.Instance is ILocalReferenceOperation localRef2)
                        {
                            disposedSymbols.Add(localRef2.Local);
                        }
                    }
                }

                // Track returns
                if (descendant is IReturnOperation returnOp)
                {
                    if (returnOp.ReturnedValue is ILocalReferenceOperation localRef3)
                    {
                        returnedSymbols.Add(localRef3.Local);
                    }
                }
            }

            // Check each created disposable
            foreach (var (name, type, location) in createdDisposables)
            {
                // Find the symbol for this local
                var localSymbol = semanticModel.LookupSymbols(location.SourceSpan.Start, name: name)
                    .FirstOrDefault();

                if (localSymbol == null)
                    continue;

                // Check if it was disposed or returned
                var wasDisposed = disposedSymbols.Contains(localSymbol);
                var wasReturned = returnedSymbols.Contains(localSymbol);

                if (!wasDisposed && !wasReturned)
                {
                    // Not disposed and not returned - report diagnostic
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        location,
                        type.Name,
                        method.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
