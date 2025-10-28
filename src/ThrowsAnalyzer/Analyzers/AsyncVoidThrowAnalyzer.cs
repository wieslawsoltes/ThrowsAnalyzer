using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ThrowsAnalyzer.Analysis;
using CoreAsyncDetector = RoslynAnalyzer.Core.Analysis.Patterns.Async.AsyncMethodDetector;

namespace ThrowsAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer that detects async void methods that throw exceptions.
    /// Reports THROWS021: "Async void method '{0}' throws {1} which cannot be caught by callers"
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsyncVoidThrowAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS021";

        private static readonly LocalizableString Title = "Async void method throws exception";
        private static readonly LocalizableString MessageFormat = "Async void method '{0}' throws {1} which cannot be caught by callers";
        private static readonly LocalizableString Description = "Async void methods cannot have their exceptions caught by callers. Exceptions thrown in async void methods will crash the application unless caught within the method itself. Consider returning Task instead of void, or ensure all exceptions are handled within the method. Async void should only be used for event handlers.";
        private const string Category = "Exception";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethod,
                SyntaxKind.MethodDeclaration);

            context.RegisterSyntaxNodeAction(AnalyzeLocalFunction,
                SyntaxKind.LocalFunctionStatement);

            context.RegisterSyntaxNodeAction(AnalyzeLambda,
                SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.ParenthesizedLambdaExpression);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDecl = (Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax)context.Node;

            // Get method symbol
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken);
            if (methodSymbol == null)
                return;

            // Only analyze async void methods
            if (!CoreAsyncDetector.IsAsyncVoid(methodSymbol, context.Compilation))
                return;

            // Exception: Allow async void for event handlers
            if (IsEventHandler(methodSymbol))
                return;

            AnalyzeAsyncVoidMethod(context, methodSymbol, methodDecl);
        }

        private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
        {
            var localFunc = (Microsoft.CodeAnalysis.CSharp.Syntax.LocalFunctionStatementSyntax)context.Node;

            // Get method symbol
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(localFunc, context.CancellationToken);
            if (methodSymbol == null)
                return;

            // Only analyze async void local functions
            if (!CoreAsyncDetector.IsAsyncVoid(methodSymbol, context.Compilation))
                return;

            AnalyzeAsyncVoidMethod(context, methodSymbol, localFunc);
        }

        private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
        {
            var lambda = (Microsoft.CodeAnalysis.CSharp.Syntax.AnonymousFunctionExpressionSyntax)context.Node;

            // Check if this is an async lambda
            if (!CoreAsyncDetector.HasAsyncModifier(lambda))
                return;

            // Get the lambda's type info to check return type
            var typeInfo = context.SemanticModel.GetTypeInfo(lambda, context.CancellationToken);
            var lambdaType = typeInfo.ConvertedType as INamedTypeSymbol;

            if (lambdaType == null)
                return;

            // Check if it's an Action (void-returning delegate)
            if (!IsActionDelegate(lambdaType))
                return;

            // Analyze the lambda body for throws
            var body = CoreAsyncDetector.GetMethodBody(lambda);
            if (body == null)
                return;

            var throwStatements = body.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ThrowStatementSyntax>()
                .Where(t => t.Expression != null); // Skip bare rethrows

            var throwExpressions = body.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ThrowExpressionSyntax>();

            foreach (var throwNode in throwStatements.Cast<SyntaxNode>().Concat(throwExpressions))
            {
                var exceptionType = TypeAnalysis.ExceptionTypeAnalyzer.GetThrownExceptionType(
                    throwNode,
                    context.SemanticModel);

                if (exceptionType != null)
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        throwNode.GetLocation(),
                        "async lambda",
                        exceptionType.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzeAsyncVoidMethod(
            SyntaxNodeAnalysisContext context,
            IMethodSymbol methodSymbol,
            SyntaxNode methodNode)
        {
            var analyzer = new AsyncExceptionAnalyzer(context.SemanticModel, context.CancellationToken);

            // Analyze the method
            var analysisTask = analyzer.AnalyzeAsync(methodSymbol, methodNode);
            var info = Task.Run(async () => await analysisTask).GetAwaiter().GetResult();

            // Check for any throws (before or after await)
            var body = CoreAsyncDetector.GetMethodBody(methodNode);
            if (body == null)
                return;

            var throwStatements = body.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ThrowStatementSyntax>()
                .Where(t => t.Expression != null); // Skip bare rethrows

            var throwExpressions = body.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ThrowExpressionSyntax>();

            foreach (var throwNode in throwStatements.Cast<SyntaxNode>().Concat(throwExpressions))
            {
                // Check if the throw is inside a try-catch that handles it
                if (IsThrowFullyHandled(throwNode, body, context.SemanticModel))
                    continue;

                var exceptionType = TypeAnalysis.ExceptionTypeAnalyzer.GetThrownExceptionType(
                    throwNode,
                    context.SemanticModel);

                if (exceptionType != null)
                {
                    var methodName = GetMethodDisplayName(methodSymbol);

                    var diagnostic = Diagnostic.Create(
                        Rule,
                        throwNode.GetLocation(),
                        methodName,
                        exceptionType.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool IsEventHandler(IMethodSymbol method)
        {
            // Check if method has typical event handler signature
            if (method.Parameters.Length == 2)
            {
                var firstParam = method.Parameters[0];
                var secondParam = method.Parameters[1];

                // object sender, EventArgs e
                if (firstParam.Type.SpecialType == SpecialType.System_Object &&
                    secondParam.Type.Name.EndsWith("EventArgs"))
                {
                    return true;
                }
            }

            // Check if method name ends with EventHandler pattern
            if (method.Name.EndsWith("_Click") ||
                method.Name.EndsWith("_Changed") ||
                method.Name.EndsWith("_Loaded") ||
                method.Name.EndsWith("Handler"))
            {
                return true;
            }

            return false;
        }

        private static bool IsActionDelegate(INamedTypeSymbol delegateType)
        {
            var name = delegateType.OriginalDefinition.ToDisplayString();
            return name.StartsWith("System.Action");
        }

        private static bool IsThrowFullyHandled(
            SyntaxNode throwNode,
            SyntaxNode methodBody,
            SemanticModel semanticModel)
        {
            // Find the containing try statement
            var tryStatement = throwNode.Ancestors()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TryStatementSyntax>()
                .FirstOrDefault();

            if (tryStatement == null)
                return false;

            // Check if the try statement is within the method body
            if (!methodBody.Contains(tryStatement))
                return false;

            // Get the exception type being thrown
            var exceptionType = TypeAnalysis.ExceptionTypeAnalyzer.GetThrownExceptionType(
                throwNode,
                semanticModel);

            if (exceptionType == null)
                return false;

            // Check if any catch clause can handle this exception
            foreach (var catchClause in tryStatement.Catches)
            {
                var caughtType = TypeAnalysis.ExceptionTypeAnalyzer.GetCaughtExceptionType(
                    catchClause,
                    semanticModel);

                if (caughtType != null)
                {
                    if (TypeAnalysis.ExceptionTypeAnalyzer.IsAssignableTo(
                        exceptionType,
                        caughtType,
                        semanticModel.Compilation))
                    {
                        // Check if the catch clause rethrows
                        var rethrows = catchClause.Block.DescendantNodes()
                            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ThrowStatementSyntax>()
                            .Any();

                        if (!rethrows)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static string GetMethodDisplayName(IMethodSymbol method)
        {
            if (method.ContainingType != null)
            {
                return $"{method.ContainingType.Name}.{method.Name}";
            }
            return method.Name;
        }
    }
}
