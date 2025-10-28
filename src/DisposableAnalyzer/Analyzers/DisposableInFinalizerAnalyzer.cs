using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that validates finalizer implementation for types with disposable fields.
/// DISP019: Finalizer without disposal
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableInFinalizerAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MissingFinalizerRule = new(
        id: DiagnosticIds.DisposableInFinalizer,
        title: "Type with unmanaged resources should have finalizer",
        messageFormat: "Type '{0}' disposes unmanaged resources but lacks a finalizer",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Types that dispose unmanaged resources should implement a finalizer to ensure cleanup if Dispose is not called.");

    public static readonly DiagnosticDescriptor MissingDisposeCallRule = new(
        id: DiagnosticIds.DisposableInFinalizer,
        title: "Finalizer should call Dispose(false)",
        messageFormat: "Finalizer should call Dispose(false) to clean up unmanaged resources",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Finalizers should call Dispose(false) to ensure unmanaged resources are released even if Dispose() is not called.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingFinalizerRule, MissingDisposeCallRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterOperationBlockStartAction(AnalyzeOperationBlockStart);
    }

    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        if (namedType.TypeKind != TypeKind.Class)
            return;

        // Check if type has Dispose(bool) method (indicates unmanaged resources)
        var disposeBoolMethod = namedType.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(DisposableHelper.IsDisposeBoolMethod);

        if (disposeBoolMethod == null)
            return;

        // Check if type has finalizer
        bool hasFinalizer = DisposableHelper.HasFinalizer(namedType);

        // If no finalizer, suggest adding one (info level)
        if (!hasFinalizer)
        {
            var diagnostic = Diagnostic.Create(
                MissingFinalizerRule,
                namedType.Locations.FirstOrDefault(),
                namedType.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext context)
    {
        if (context.OwningSymbol is not IMethodSymbol method)
            return;

        // Only analyze finalizers
        if (method.MethodKind != MethodKind.Destructor)
            return;

        // Only check finalizers in IDisposable classes
        var containingType = method.ContainingType;
        if (!DisposableHelper.IsDisposableType(containingType))
            return;

        var hasDisposeCall = false;

        context.RegisterOperationAction(operationContext =>
        {
            if (operationContext.Operation is IInvocationOperation invocation)
            {
                // Check for Dispose(false) call
                if (invocation.TargetMethod.Name == "Dispose" &&
                    invocation.Arguments.Length == 1)
                {
                    var arg = invocation.Arguments[0];
                    if (arg.Value.ConstantValue.HasValue &&
                        arg.Value.ConstantValue.Value is bool boolValue &&
                        boolValue == false)
                    {
                        hasDisposeCall = true;
                    }
                }
            }
        }, OperationKind.Invocation);

        context.RegisterOperationBlockEndAction(blockEndContext =>
        {
            if (!hasDisposeCall)
            {
                // Report on the destructor declaration - specifically the tilde + identifier
                var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                Location location;

                if (syntax is DestructorDeclarationSyntax destructor)
                {
                    // Create span from tilde token to end of identifier
                    var tildeToken = destructor.TildeToken;
                    var identifier = destructor.Identifier;
                    var span = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(
                        tildeToken.SpanStart,
                        identifier.Span.End);
                    location = Location.Create(syntax.SyntaxTree, span);
                }
                else
                {
                    location = syntax?.GetLocation() ?? method.Locations.FirstOrDefault();
                }

                var diagnostic = Diagnostic.Create(
                    MissingDisposeCallRule,
                    location);
                blockEndContext.ReportDiagnostic(diagnostic);
            }
        });
    }
}
