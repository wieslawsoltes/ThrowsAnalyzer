using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Detects and analyzes lambda expressions and their exception patterns.
    /// </summary>
    public static class LambdaExceptionDetector
    {
        /// <summary>
        /// Gets all lambda expressions in a syntax node.
        /// </summary>
        public static IEnumerable<LambdaExpressionSyntax> GetLambdaExpressions(SyntaxNode node)
        {
            if (node == null)
                return Enumerable.Empty<LambdaExpressionSyntax>();

            return node.DescendantNodes()
                .OfType<LambdaExpressionSyntax>();
        }

        /// <summary>
        /// Gets all simple lambda expressions (single parameter).
        /// </summary>
        public static IEnumerable<SimpleLambdaExpressionSyntax> GetSimpleLambdas(SyntaxNode node)
        {
            if (node == null)
                return Enumerable.Empty<SimpleLambdaExpressionSyntax>();

            return node.DescendantNodes()
                .OfType<SimpleLambdaExpressionSyntax>();
        }

        /// <summary>
        /// Gets all parenthesized lambda expressions (multiple or typed parameters).
        /// </summary>
        public static IEnumerable<ParenthesizedLambdaExpressionSyntax> GetParenthesizedLambdas(SyntaxNode node)
        {
            if (node == null)
                return Enumerable.Empty<ParenthesizedLambdaExpressionSyntax>();

            return node.DescendantNodes()
                .OfType<ParenthesizedLambdaExpressionSyntax>();
        }

        /// <summary>
        /// Gets the body of a lambda expression.
        /// </summary>
        public static SyntaxNode GetLambdaBody(LambdaExpressionSyntax lambda)
        {
            if (lambda == null)
                return null;

            if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
                return simpleLambda.Body;

            if (lambda is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
                return parenthesizedLambda.Body;

            return null;
        }

        /// <summary>
        /// Checks if a lambda has a block body.
        /// </summary>
        public static bool HasBlockBody(LambdaExpressionSyntax lambda)
        {
            var body = GetLambdaBody(lambda);
            return body is BlockSyntax;
        }

        /// <summary>
        /// Checks if a lambda has an expression body.
        /// </summary>
        public static bool HasExpressionBody(LambdaExpressionSyntax lambda)
        {
            var body = GetLambdaBody(lambda);
            return body != null && !(body is BlockSyntax);
        }

        /// <summary>
        /// Gets all throw statements in a lambda.
        /// </summary>
        public static IEnumerable<ThrowStatementSyntax> GetThrowStatements(LambdaExpressionSyntax lambda)
        {
            var body = GetLambdaBody(lambda);
            if (body == null)
                return Enumerable.Empty<ThrowStatementSyntax>();

            return body.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Where(t => t.Expression != null); // Exclude bare rethrows
        }

        /// <summary>
        /// Gets all throw expressions in a lambda.
        /// </summary>
        public static IEnumerable<ThrowExpressionSyntax> GetThrowExpressions(LambdaExpressionSyntax lambda)
        {
            var body = GetLambdaBody(lambda);
            if (body == null)
                return Enumerable.Empty<ThrowExpressionSyntax>();

            return body.DescendantNodes()
                .OfType<ThrowExpressionSyntax>();
        }

        /// <summary>
        /// Checks if a lambda contains any try-catch blocks.
        /// </summary>
        public static bool HasTryCatch(LambdaExpressionSyntax lambda)
        {
            var body = GetLambdaBody(lambda);
            if (body == null)
                return false;

            return body.DescendantNodes()
                .OfType<TryStatementSyntax>()
                .Any(t => t.Catches.Any());
        }

        /// <summary>
        /// Gets all try-catch statements in a lambda.
        /// </summary>
        public static IEnumerable<TryStatementSyntax> GetTryCatchStatements(LambdaExpressionSyntax lambda)
        {
            var body = GetLambdaBody(lambda);
            if (body == null)
                return Enumerable.Empty<TryStatementSyntax>();

            return body.DescendantNodes()
                .OfType<TryStatementSyntax>()
                .Where(t => t.Catches.Any());
        }

        /// <summary>
        /// Checks if a lambda is async.
        /// </summary>
        public static bool IsAsyncLambda(LambdaExpressionSyntax lambda)
        {
            if (lambda == null)
                return false;

            return lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
        }

        /// <summary>
        /// Determines if a lambda is used as an event handler.
        /// </summary>
        public static bool IsEventHandlerLambda(LambdaExpressionSyntax lambda, SemanticModel semanticModel)
        {
            if (lambda == null || semanticModel == null)
                return false;

            // Check if lambda is being assigned to an event
            var parent = lambda.Parent;

            // Lambda in += or -= operation
            if (parent is AssignmentExpressionSyntax assignment)
            {
                if (assignment.IsKind(SyntaxKind.AddAssignmentExpression) ||
                    assignment.IsKind(SyntaxKind.SubtractAssignmentExpression))
                {
                    var leftSymbol = semanticModel.GetSymbolInfo(assignment.Left).Symbol;
                    if (leftSymbol is IEventSymbol)
                        return true;
                }
            }

            // Check if lambda type is an event handler delegate
            var typeInfo = semanticModel.GetTypeInfo(lambda);
            if (typeInfo.ConvertedType is INamedTypeSymbol namedType)
            {
                var typeName = namedType.Name;
                if (typeName.EndsWith("EventHandler") || typeName.EndsWith("Handler"))
                    return true;

                // Check for (object, EventArgs) signature
                if (namedType.DelegateInvokeMethod != null)
                {
                    var parameters = namedType.DelegateInvokeMethod.Parameters;
                    if (parameters.Length == 2)
                    {
                        var firstParam = parameters[0].Type;
                        var secondParam = parameters[1].Type;

                        if (firstParam.SpecialType == SpecialType.System_Object &&
                            IsEventArgsType(secondParam, semanticModel.Compilation))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines the context in which a lambda is used.
        /// </summary>
        public static LambdaContext GetLambdaContext(LambdaExpressionSyntax lambda, SemanticModel semanticModel)
        {
            if (lambda == null)
                return LambdaContext.Unknown;

            // Check for event handler
            if (IsEventHandlerLambda(lambda, semanticModel))
                return LambdaContext.EventHandler;

            // Check for LINQ query
            if (IsInLinqQuery(lambda))
                return LambdaContext.LinqQuery;

            // Check for Task.Run or similar
            if (IsInTaskRun(lambda, semanticModel))
                return LambdaContext.TaskRun;

            // Check for callback parameter
            if (IsCallbackParameter(lambda, semanticModel))
                return LambdaContext.Callback;

            return LambdaContext.General;
        }

        private static bool IsEventArgsType(ITypeSymbol type, Compilation compilation)
        {
            if (type == null)
                return false;

            var eventArgsType = compilation.GetTypeByMetadataName("System.EventArgs");
            if (eventArgsType == null)
                return false;

            // Check if type is EventArgs or derives from it
            var current = type;
            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, eventArgsType))
                    return true;

                current = current.BaseType;
            }

            return false;
        }

        private static bool IsInLinqQuery(LambdaExpressionSyntax lambda)
        {
            var parent = lambda.Parent;
            while (parent != null)
            {
                // Check for LINQ query syntax
                if (parent is QueryExpressionSyntax)
                    return true;

                // Check for LINQ method calls (Where, Select, etc.)
                if (parent is InvocationExpressionSyntax invocation)
                {
                    if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                    {
                        var methodName = memberAccess.Name.Identifier.Text;
                        if (IsLinqMethod(methodName))
                            return true;
                    }
                }

                parent = parent.Parent;
            }

            return false;
        }

        private static bool IsLinqMethod(string methodName)
        {
            return methodName == "Where" || methodName == "Select" || methodName == "SelectMany" ||
                   methodName == "OrderBy" || methodName == "OrderByDescending" ||
                   methodName == "GroupBy" || methodName == "Join" || methodName == "GroupJoin" ||
                   methodName == "Any" || methodName == "All" || methodName == "First" ||
                   methodName == "FirstOrDefault" || methodName == "Single" || methodName == "SingleOrDefault" ||
                   methodName == "Last" || methodName == "LastOrDefault" || methodName == "Aggregate" ||
                   methodName == "Count" || methodName == "Sum" || methodName == "Average" ||
                   methodName == "Min" || methodName == "Max";
        }

        private static bool IsInTaskRun(LambdaExpressionSyntax lambda, SemanticModel semanticModel)
        {
            var parent = lambda.Parent;
            if (parent is ArgumentSyntax argument)
            {
                var argumentList = argument.Parent as ArgumentListSyntax;
                if (argumentList?.Parent is InvocationExpressionSyntax invocation)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                    if (symbolInfo.Symbol is IMethodSymbol method)
                    {
                        return method.ContainingType.Name == "Task" && method.Name == "Run";
                    }
                }
            }

            return false;
        }

        private static bool IsCallbackParameter(LambdaExpressionSyntax lambda, SemanticModel semanticModel)
        {
            var parent = lambda.Parent;
            if (parent is ArgumentSyntax argument)
            {
                var typeInfo = semanticModel.GetTypeInfo(lambda);
                if (typeInfo.ConvertedType is INamedTypeSymbol namedType)
                {
                    var typeName = namedType.Name.ToLower();
                    return typeName.Contains("callback") || typeName.Contains("action") || typeName.Contains("func");
                }
            }

            return false;
        }

        /// <summary>
        /// Gets comprehensive information about a lambda expression.
        /// </summary>
        public static LambdaExceptionInfo GetLambdaExceptionInfo(
            LambdaExpressionSyntax lambda,
            SemanticModel semanticModel)
        {
            return new LambdaExceptionInfo
            {
                Lambda = lambda,
                IsAsync = IsAsyncLambda(lambda),
                HasBlockBody = HasBlockBody(lambda),
                HasExpressionBody = HasExpressionBody(lambda),
                ThrowCount = GetThrowStatements(lambda).Count() + GetThrowExpressions(lambda).Count(),
                HasTryCatch = HasTryCatch(lambda),
                Context = GetLambdaContext(lambda, semanticModel),
                IsEventHandler = IsEventHandlerLambda(lambda, semanticModel)
            };
        }
    }

    /// <summary>
    /// Context in which a lambda is used.
    /// </summary>
    public enum LambdaContext
    {
        Unknown,
        EventHandler,
        LinqQuery,
        TaskRun,
        Callback,
        General
    }

    /// <summary>
    /// Contains information about a lambda expression.
    /// </summary>
    public class LambdaExceptionInfo
    {
        /// <summary>
        /// The lambda expression.
        /// </summary>
        public LambdaExpressionSyntax Lambda { get; set; }

        /// <summary>
        /// True if the lambda is async.
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// True if the lambda has a block body.
        /// </summary>
        public bool HasBlockBody { get; set; }

        /// <summary>
        /// True if the lambda has an expression body.
        /// </summary>
        public bool HasExpressionBody { get; set; }

        /// <summary>
        /// Number of throw statements/expressions.
        /// </summary>
        public int ThrowCount { get; set; }

        /// <summary>
        /// True if the lambda contains try-catch.
        /// </summary>
        public bool HasTryCatch { get; set; }

        /// <summary>
        /// The context in which the lambda is used.
        /// </summary>
        public LambdaContext Context { get; set; }

        /// <summary>
        /// True if the lambda is an event handler.
        /// </summary>
        public bool IsEventHandler { get; set; }
    }
}
