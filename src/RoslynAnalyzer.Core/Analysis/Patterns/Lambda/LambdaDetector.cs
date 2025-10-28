using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalyzer.Core.Analysis.Patterns.Lambda
{
    /// <summary>
    /// Detects and analyzes lambda expressions in C# code.
    /// </summary>
    /// <remarks>
    /// This detector provides utilities for analyzing lambda expressions:
    /// - Lambda expression identification and extraction
    /// - Lambda body type detection (block vs expression)
    /// - Async lambda detection
    /// - Event handler lambda detection
    /// - Lambda context analysis (LINQ, Task.Run, callbacks, etc.)
    /// - Try-catch pattern detection in lambdas
    ///
    /// These utilities are useful for:
    /// - Building analyzers for lambda best practices
    /// - Detecting problematic patterns in lambdas
    /// - Analyzing control flow in lambda expressions
    /// - Understanding lambda usage patterns
    /// </remarks>
    public static class LambdaDetector
    {
        /// <summary>
        /// Gets all lambda expressions in a syntax node.
        /// </summary>
        /// <param name="node">The syntax node to search.</param>
        /// <returns>All lambda expressions found in the node and its descendants.</returns>
        /// <remarks>
        /// This includes both simple lambdas (x => x.Length) and parenthesized lambdas ((x, y) => x + y).
        /// </remarks>
        public static IEnumerable<LambdaExpressionSyntax> GetLambdaExpressions(SyntaxNode node)
        {
            if (node == null)
                return Enumerable.Empty<LambdaExpressionSyntax>();

            return node.DescendantNodes()
                .OfType<LambdaExpressionSyntax>();
        }

        /// <summary>
        /// Gets all simple lambda expressions (single parameter without parentheses).
        /// </summary>
        /// <param name="node">The syntax node to search.</param>
        /// <returns>All simple lambda expressions found.</returns>
        /// <remarks>
        /// Simple lambdas have a single parameter without type annotation or parentheses: x => x * 2
        /// </remarks>
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
        /// <param name="node">The syntax node to search.</param>
        /// <returns>All parenthesized lambda expressions found.</returns>
        /// <remarks>
        /// Parenthesized lambdas can have:
        /// - Multiple parameters: (x, y) => x + y
        /// - Typed parameters: (int x) => x * 2
        /// - No parameters: () => 42
        /// </remarks>
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
        /// <param name="lambda">The lambda expression.</param>
        /// <returns>The body syntax node (either BlockSyntax or an expression), or null if not available.</returns>
        /// <remarks>
        /// Lambda bodies can be:
        /// - Block: x => { return x * 2; }
        /// - Expression: x => x * 2
        /// </remarks>
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
        /// Checks if a lambda has a block body ({ }).
        /// </summary>
        /// <param name="lambda">The lambda expression.</param>
        /// <returns>True if the lambda has a block body; otherwise, false.</returns>
        /// <remarks>
        /// Block bodies are enclosed in braces and can contain multiple statements.
        /// Example: x => { var y = x * 2; return y; }
        /// </remarks>
        public static bool HasBlockBody(LambdaExpressionSyntax lambda)
        {
            var body = GetLambdaBody(lambda);
            return body is BlockSyntax;
        }

        /// <summary>
        /// Checks if a lambda has an expression body (no { }).
        /// </summary>
        /// <param name="lambda">The lambda expression.</param>
        /// <returns>True if the lambda has an expression body; otherwise, false.</returns>
        /// <remarks>
        /// Expression bodies are single expressions without braces.
        /// Example: x => x * 2
        /// </remarks>
        public static bool HasExpressionBody(LambdaExpressionSyntax lambda)
        {
            var body = GetLambdaBody(lambda);
            return body != null && !(body is BlockSyntax);
        }

        /// <summary>
        /// Checks if a lambda is async.
        /// </summary>
        /// <param name="lambda">The lambda expression.</param>
        /// <returns>True if the lambda has the async modifier; otherwise, false.</returns>
        /// <remarks>
        /// Async lambdas can use await and return Task or Task&lt;T&gt;.
        /// Example: async x => await ProcessAsync(x)
        /// </remarks>
        public static bool IsAsyncLambda(LambdaExpressionSyntax lambda)
        {
            if (lambda == null)
                return false;

            return lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
        }

        /// <summary>
        /// Gets all try-catch statements in a lambda.
        /// </summary>
        /// <param name="lambda">The lambda expression.</param>
        /// <returns>All try statements that have at least one catch clause.</returns>
        /// <remarks>
        /// Useful for detecting error handling patterns in lambdas.
        /// </remarks>
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
        /// Checks if a lambda contains any try-catch blocks.
        /// </summary>
        /// <param name="lambda">The lambda expression.</param>
        /// <returns>True if the lambda contains try-catch; otherwise, false.</returns>
        /// <remarks>
        /// Indicates whether the lambda has any error handling logic.
        /// </remarks>
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
        /// Determines if a lambda is used as an event handler.
        /// </summary>
        /// <param name="lambda">The lambda expression.</param>
        /// <param name="semanticModel">The semantic model for type resolution.</param>
        /// <returns>True if the lambda is an event handler; otherwise, false.</returns>
        /// <remarks>
        /// Event handler lambdas are detected by:
        /// - Assignment to an event (+=, -=)
        /// - Type name ending with "EventHandler" or "Handler"
        /// - (object, EventArgs) signature pattern
        ///
        /// Event handlers should generally not throw exceptions.
        /// </remarks>
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
        /// <param name="lambda">The lambda expression.</param>
        /// <param name="semanticModel">The semantic model for type resolution.</param>
        /// <returns>The lambda context (EventHandler, LinqQuery, TaskRun, Callback, General, or Unknown).</returns>
        /// <remarks>
        /// Understanding lambda context helps analyzers provide context-specific guidance.
        /// For example:
        /// - Event handlers shouldn't throw exceptions
        /// - LINQ lambdas should be side-effect free
        /// - Task.Run lambdas are fire-and-forget
        /// </remarks>
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

        /// <summary>
        /// Gets comprehensive information about a lambda expression.
        /// </summary>
        /// <param name="lambda">The lambda expression.</param>
        /// <param name="semanticModel">The semantic model for type resolution.</param>
        /// <returns>A LambdaInfo object containing lambda properties.</returns>
        /// <remarks>
        /// This is a convenience method that gathers multiple lambda properties in one call.
        /// </remarks>
        public static LambdaInfo GetLambdaInfo(
            LambdaExpressionSyntax lambda,
            SemanticModel semanticModel)
        {
            return new LambdaInfo
            {
                Lambda = lambda,
                IsAsync = IsAsyncLambda(lambda),
                HasBlockBody = HasBlockBody(lambda),
                HasExpressionBody = HasExpressionBody(lambda),
                HasTryCatch = HasTryCatch(lambda),
                Context = GetLambdaContext(lambda, semanticModel),
                IsEventHandler = IsEventHandlerLambda(lambda, semanticModel)
            };
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
            if (parent is ArgumentSyntax)
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
    }

    /// <summary>
    /// Context in which a lambda expression is used.
    /// </summary>
    /// <remarks>
    /// Different contexts have different expectations for lambda behavior:
    /// - EventHandler: Should not throw exceptions
    /// - LinqQuery: Should be pure and side-effect free
    /// - TaskRun: Fire-and-forget, exceptions may be unobserved
    /// - Callback: May be called from unexpected contexts
    /// - General: Standard lambda usage
    /// </remarks>
    public enum LambdaContext
    {
        /// <summary>
        /// Context cannot be determined.
        /// </summary>
        Unknown,

        /// <summary>
        /// Lambda is used as an event handler.
        /// </summary>
        EventHandler,

        /// <summary>
        /// Lambda is used in a LINQ query or method chain.
        /// </summary>
        LinqQuery,

        /// <summary>
        /// Lambda is passed to Task.Run() or similar fire-and-forget method.
        /// </summary>
        TaskRun,

        /// <summary>
        /// Lambda is passed as a callback parameter.
        /// </summary>
        Callback,

        /// <summary>
        /// Lambda is used in a general context.
        /// </summary>
        General
    }

    /// <summary>
    /// Contains comprehensive information about a lambda expression.
    /// </summary>
    /// <remarks>
    /// This class aggregates various lambda-related properties for convenient analysis.
    /// Use LambdaDetector.GetLambdaInfo() to create instances.
    /// </remarks>
    public class LambdaInfo
    {
        /// <summary>
        /// Gets or sets the lambda expression.
        /// </summary>
        public LambdaExpressionSyntax Lambda { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the lambda is async.
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the lambda has a block body.
        /// </summary>
        public bool HasBlockBody { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the lambda has an expression body.
        /// </summary>
        public bool HasExpressionBody { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the lambda contains try-catch.
        /// </summary>
        public bool HasTryCatch { get; set; }

        /// <summary>
        /// Gets or sets the context in which the lambda is used.
        /// </summary>
        public LambdaContext Context { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the lambda is an event handler.
        /// </summary>
        public bool IsEventHandler { get; set; }
    }
}
