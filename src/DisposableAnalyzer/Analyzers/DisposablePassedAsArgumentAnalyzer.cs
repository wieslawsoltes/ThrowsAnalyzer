using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects unclear disposal responsibility when passing disposables as arguments.
/// DISP017: Disposal responsibility unclear
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposablePassedAsArgumentAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposablePassedAsArgument,
        title: "Disposal responsibility unclear when passing disposable as argument",
        messageFormat: "Passing disposable '{0}' to method '{1}'. Ensure disposal responsibility is clear",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false, // Info/opt-in analyzer
        description: "When passing disposable objects as arguments, ensure it's clear whether the caller or callee is responsible for disposal.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;

        foreach (var argument in invocation.Arguments)
        {
            // Check if argument is disposable
            var argumentType = argument.Value.Type;
            if (argumentType == null || !DisposableHelper.IsAnyDisposableType(argumentType))
                continue;

            // Skip if argument is created inline (ownership transfer is clear)
            if (argument.Value is IObjectCreationOperation)
                continue;

            // Check if the parameter suggests ownership transfer by name
            var parameter = argument.Parameter;
            if (parameter != null)
            {
                var paramName = parameter.Name.ToLowerInvariant();
                // Common patterns indicating ownership transfer
                if (paramName.Contains("take") || paramName.Contains("own") ||
                    paramName.Contains("transfer") || paramName.Contains("adopt"))
                    continue;
            }

            // Check if method name suggests ownership transfer
            var methodName = invocation.TargetMethod.Name.ToLowerInvariant();
            if (methodName.StartsWith("take") || methodName.StartsWith("adopt") ||
                methodName.StartsWith("add") || methodName.StartsWith("register"))
                continue;

            // Get the disposable variable name
            string variableName = "object";
            if (argument.Value is ILocalReferenceOperation localRef)
            {
                variableName = localRef.Local.Name;
            }
            else if (argument.Value is IFieldReferenceOperation fieldRef)
            {
                variableName = fieldRef.Field.Name;
            }
            else if (argument.Value is IParameterReferenceOperation paramRef)
            {
                variableName = paramRef.Parameter.Name;
            }

            var diagnostic = Diagnostic.Create(
                Rule,
                argument.Syntax.GetLocation(),
                variableName,
                invocation.TargetMethod.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
