using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects exception handling issues with disposable fields in constructors.
/// DISP018: Exception in constructor with disposable
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableInConstructorAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableInConstructor,
        title: "Disposable created in constructor is not disposed",
        messageFormat: "Disposable '{0}' created in constructor is not stored or disposed",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Disposables created in constructors should either be stored for later disposal or wrapped in a using/try-finally to avoid leaks.");

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

        // Only analyze constructors
        if (method.MethodKind != MethodKind.Constructor)
            return;

        var trackedLocals = new Dictionary<ILocalSymbol, LocalInfo>(SymbolEqualityComparer.Default);

        context.RegisterOperationAction(operationContext =>
        {
            var creation = (IObjectCreationOperation)operationContext.Operation;

            if (!DisposableHelper.IsAnyDisposableType(creation.Type))
                return;

            if (DisposableHelper.IsInUsingStatement(creation.Syntax))
                return;

            if (IsAssignedToInstanceMember(creation))
                return;

            if (IsPartOfBaseConstructorInitializer(creation))
                return;

            var local = GetAssignedLocal(creation);
            if (local != null)
            {
                trackedLocals[local] = new LocalInfo(creation.Syntax.GetLocation());
            }
        }, OperationKind.ObjectCreation);

        // Track explicit disposal calls
        context.RegisterOperationAction(operationContext =>
        {
            var operation = operationContext.Operation;
            if (DisposableHelper.IsDisposalCall(operation, out _))
            {
                var local = GetTargetLocal(operation);
                if (local != null && trackedLocals.TryGetValue(local, out var info))
                {
                    info.IsDisposed = true;
                    trackedLocals[local] = info;
                }
            }
        }, OperationKind.Invocation, OperationKind.ConditionalAccess);

        // Track simple escape scenarios (assignment to field/property or return)
        context.RegisterOperationAction(operationContext =>
        {
            switch (operationContext.Operation)
            {
                case IAssignmentOperation assignment:
                    if (assignment.Value is ILocalReferenceOperation localRef &&
                        trackedLocals.TryGetValue(localRef.Local, out var info) &&
                        (assignment.Target is IFieldReferenceOperation or IPropertyReferenceOperation))
                    {
                        info.Escapes = true;
                        trackedLocals[localRef.Local] = info;
                    }
                    break;

                case IReturnOperation returnOp:
                    if (returnOp.ReturnedValue is ILocalReferenceOperation returnedLocal &&
                        trackedLocals.TryGetValue(returnedLocal.Local, out var info2))
                    {
                        info2.Escapes = true;
                        trackedLocals[returnedLocal.Local] = info2;
                    }
                    break;
            }
        }, OperationKind.SimpleAssignment, OperationKind.Return);

        context.RegisterOperationBlockEndAction(blockEndContext =>
        {
            if (trackedLocals.Count == 0)
                return;

            foreach (var kvp in trackedLocals)
            {
                var local = kvp.Key;
                var info = kvp.Value;

                if (info.IsDisposed || info.Escapes)
                    continue;

                var diagnostic = Diagnostic.Create(
                    Rule,
                    info.Location,
                    local.Type.Name);
                blockEndContext.ReportDiagnostic(diagnostic);
            }
        });
    }

    private static ILocalSymbol? GetAssignedLocal(IObjectCreationOperation creation)
    {
        var parent = creation.Parent;
        while (parent != null)
        {
            switch (parent)
            {
                case IVariableDeclaratorOperation declarator:
                    return declarator.Symbol as ILocalSymbol;
                case IVariableInitializerOperation initializer when initializer.Parent is IVariableDeclaratorOperation declarator:
                    return declarator.Symbol as ILocalSymbol;
                case IAssignmentOperation assignment when assignment.Target is ILocalReferenceOperation localRef:
                    return localRef.Local;
            }
            parent = parent.Parent;
        }
        return null;
    }

    private static bool IsAssignedToInstanceMember(IObjectCreationOperation creation)
    {
        var parent = creation.Parent;
        while (parent != null)
        {
            if (parent is IAssignmentOperation assignment)
            {
                if (assignment.Target is IFieldReferenceOperation fieldRef &&
                    fieldRef.Instance is IInstanceReferenceOperation)
                {
                    return true;
                }

                if (assignment.Target is IPropertyReferenceOperation propertyRef &&
                    propertyRef.Instance is IInstanceReferenceOperation)
                {
                    return true;
                }
            }

            parent = parent.Parent;
        }

        return false;
    }

    private static bool IsPartOfBaseConstructorInitializer(IObjectCreationOperation creation)
    {
        var parent = creation.Parent;
        while (parent != null)
        {
            if (parent is IArgumentOperation argument &&
                argument.Parent is IInvocationOperation invocation &&
                invocation.TargetMethod.MethodKind == MethodKind.Constructor &&
                invocation.Parent is IConstructorBodyOperation)
            {
                return true;
            }
            parent = parent.Parent;
        }

        return false;
    }

    private static ILocalSymbol? GetTargetLocal(IOperation operation)
    {
        switch (operation)
        {
            case IInvocationOperation invocation when invocation.Instance is ILocalReferenceOperation localRef:
                return localRef.Local;
            case IConditionalAccessOperation conditional when conditional.Operation is ILocalReferenceOperation localRef &&
                                                              conditional.WhenNotNull is IInvocationOperation:
                return localRef.Local;
        }

        return null;
    }

    private struct LocalInfo
    {
        public LocalInfo(Location location)
        {
            Location = location;
            IsDisposed = false;
            Escapes = false;
        }

        public Location Location { get; }
        public bool IsDisposed { get; set; }
        public bool Escapes { get; set; }
    }
}
