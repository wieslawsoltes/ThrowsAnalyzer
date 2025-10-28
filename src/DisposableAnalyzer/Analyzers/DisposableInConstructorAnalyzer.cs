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
        title: "Exception in constructor with disposable",
        messageFormat: "Constructor initializes disposable field '{0}' but doesn't handle exceptions. Resources may leak if constructor fails",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When initializing disposable fields in constructors, wrap initialization in try-catch to dispose resources if construction fails.");

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

        var disposableFieldAssignments = new List<IFieldSymbol>();
        var hasTryCatch = false;

        context.RegisterOperationAction(operationContext =>
        {
            // Track disposable field assignments
            if (operationContext.Operation is IAssignmentOperation assignment)
            {
                if (assignment.Target is IFieldReferenceOperation fieldRef)
                {
                    if (DisposableHelper.IsAnyDisposableType(fieldRef.Field.Type))
                    {
                        // Check if RHS creates a disposable
                        if (assignment.Value is IObjectCreationOperation)
                        {
                            disposableFieldAssignments.Add(fieldRef.Field);
                        }
                    }
                }
            }

            // Check for try-catch blocks
            if (operationContext.Operation is ITryOperation)
            {
                hasTryCatch = true;
            }
        }, OperationKind.SimpleAssignment, OperationKind.Try);

        context.RegisterOperationBlockEndAction(blockEndContext =>
        {
            // If we have disposable field assignments and no try-catch, warn
            if (disposableFieldAssignments.Count > 0 && !hasTryCatch)
            {
                foreach (var field in disposableFieldAssignments)
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        method.Locations.FirstOrDefault(),
                        field.Name);
                    blockEndContext.ReportDiagnostic(diagnostic);
                }
            }
        });
    }
}
