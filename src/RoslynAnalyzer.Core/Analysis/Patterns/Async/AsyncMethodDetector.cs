using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalyzer.Core.Analysis.Patterns.Async
{
    /// <summary>
    /// Detects and analyzes async/await patterns in C# code.
    /// </summary>
    /// <remarks>
    /// This detector provides utilities for analyzing asynchronous methods:
    /// - Async method identification (async modifier, Task return type)
    /// - Async void detection (problematic pattern)
    /// - Await expression location tracking
    /// - Unawaited Task detection
    /// - Synchronous vs asynchronous operation detection
    ///
    /// These utilities are useful for:
    /// - Building analyzers for async best practices
    /// - Detecting async void methods (fire-and-forget pattern)
    /// - Finding unawaited Tasks
    /// - Analyzing control flow in async methods
    /// - Detecting synchronous code before first await
    /// </remarks>
    public static class AsyncMethodDetector
    {
        /// <summary>
        /// Checks if a method has the async modifier.
        /// </summary>
        /// <param name="method">The method symbol to check.</param>
        /// <returns>True if the method is declared with the async modifier; otherwise, false.</returns>
        /// <remarks>
        /// This checks the IsAsync property on the method symbol, which is set by the compiler
        /// when the async keyword is present in the method declaration.
        ///
        /// Note: A method can return Task without being async, but an async method must have
        /// the async modifier even if it doesn't await anything.
        /// </remarks>
        public static bool IsAsyncMethod(IMethodSymbol method)
        {
            return method?.IsAsync == true;
        }

        /// <summary>
        /// Checks if a method returns Task or Task&lt;T&gt;.
        /// </summary>
        /// <param name="method">The method symbol to check.</param>
        /// <param name="compilation">The compilation context for type resolution.</param>
        /// <returns>True if the method returns Task or Task&lt;T&gt;; otherwise, false.</returns>
        /// <remarks>
        /// This method checks the return type against System.Threading.Tasks.Task and
        /// System.Threading.Tasks.Task&lt;T&gt; using OriginalDefinition to handle generic types correctly.
        ///
        /// Methods returning Task don't have to be async - they can return Task.FromResult,
        /// Task.CompletedTask, or manually created Task instances.
        /// </remarks>
        public static bool ReturnsTask(IMethodSymbol method, Compilation compilation)
        {
            var returnType = method?.ReturnType;
            if (returnType == null)
                return false;

            var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

            if (taskType == null)
                return false;

            // Check if return type is Task
            if (SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, taskType))
                return true;

            // Check if return type is Task<T>
            if (taskOfTType != null &&
                SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, taskOfTType))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a method is async void (fire-and-forget pattern).
        /// </summary>
        /// <param name="method">The method symbol to check.</param>
        /// <param name="compilation">The compilation context.</param>
        /// <returns>True if the method is async void; otherwise, false.</returns>
        /// <remarks>
        /// Async void methods are generally considered a problematic pattern because:
        /// - Exceptions cannot be caught by callers
        /// - No way to await completion
        /// - Can cause application crashes if unhandled exceptions occur
        ///
        /// They should only be used for top-level event handlers.
        /// Most analyzers flag async void as a code smell.
        /// </remarks>
        public static bool IsAsyncVoid(IMethodSymbol method, Compilation compilation)
        {
            return method != null &&
                   method.IsAsync &&
                   method.ReturnType.SpecialType == SpecialType.System_Void;
        }

        /// <summary>
        /// Gets the first await expression in a method body.
        /// </summary>
        /// <param name="methodBody">The method body syntax node to analyze.</param>
        /// <returns>The first AwaitExpressionSyntax found, or null if none exist.</returns>
        /// <remarks>
        /// This is useful for:
        /// - Detecting code that runs synchronously before the first await
        /// - Analyzing method execution flow
        /// - Finding methods with async modifier but no awaits (compiler warning CS1998)
        ///
        /// Code before the first await executes synchronously on the caller's thread.
        /// </remarks>
        public static AwaitExpressionSyntax GetFirstAwaitExpression(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return null;

            return methodBody.DescendantNodes()
                .OfType<AwaitExpressionSyntax>()
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets all await expressions in a method body.
        /// </summary>
        /// <param name="methodBody">The method body syntax node to analyze.</param>
        /// <returns>An enumerable of all AwaitExpressionSyntax nodes found.</returns>
        /// <remarks>
        /// This returns all await expressions in document order.
        /// Useful for analyzing async control flow and await patterns.
        /// </remarks>
        public static IEnumerable<AwaitExpressionSyntax> GetAllAwaitExpressions(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return Enumerable.Empty<AwaitExpressionSyntax>();

            return methodBody.DescendantNodes()
                .OfType<AwaitExpressionSyntax>();
        }

        /// <summary>
        /// Checks if a syntax node occurs before the first await expression in a method.
        /// </summary>
        /// <param name="node">The syntax node to check (e.g., throw statement, assignment).</param>
        /// <param name="methodBody">The method body containing both nodes.</param>
        /// <returns>
        /// True if the node occurs before the first await, or if there are no awaits in the method;
        /// otherwise, false.
        /// </returns>
        /// <remarks>
        /// This is useful for detecting:
        /// - Synchronous operations before async execution begins
        /// - Exceptions thrown before the method goes async
        /// - Validation code that runs synchronously
        ///
        /// Code before the first await executes on the calling thread synchronously.
        /// This affects exception handling and performance characteristics.
        /// </remarks>
        public static bool IsBeforeFirstAwait(
            SyntaxNode node,
            SyntaxNode methodBody)
        {
            if (methodBody == null)
                return false;

            var firstAwait = GetFirstAwaitExpression(methodBody);
            if (firstAwait == null)
            {
                // No await in method - all code runs synchronously
                return true;
            }

            // Compare source positions
            var nodePosition = node.SpanStart;
            var awaitPosition = firstAwait.SpanStart;

            return nodePosition < awaitPosition;
        }

        /// <summary>
        /// Gets all Task-returning method invocations that are not awaited.
        /// </summary>
        /// <param name="methodBody">The method body to analyze.</param>
        /// <param name="semanticModel">The semantic model for type resolution.</param>
        /// <returns>An enumerable of InvocationExpressionSyntax for unawaited Task-returning calls.</returns>
        /// <remarks>
        /// This detects "fire-and-forget" calls which can be problematic:
        /// - Exceptions are lost (not propagated to caller)
        /// - No way to know when operation completes
        /// - Can cause subtle timing bugs
        ///
        /// This method skips invocations where the result is used (assigned, returned, or passed as argument),
        /// focusing on cases where the Task is completely ignored.
        /// </remarks>
        public static IEnumerable<InvocationExpressionSyntax> GetUnawaitedTaskInvocations(
            SyntaxNode methodBody,
            SemanticModel semanticModel)
        {
            if (methodBody == null)
                yield break;

            var invocations = methodBody.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                // Skip if this invocation is awaited
                if (invocation.Parent is AwaitExpressionSyntax)
                    continue;

                // Skip if used in assignment, return, or argument
                if (IsInvocationResultUsed(invocation))
                    continue;

                // Check if the invocation returns Task
                var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    if (ReturnsTask(methodSymbol, semanticModel.Compilation))
                    {
                        yield return invocation;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if an invocation's result is used (not discarded).
        /// </summary>
        /// <param name="invocation">The invocation expression to check.</param>
        /// <returns>True if the result is assigned, returned, or passed as an argument; otherwise, false.</returns>
        /// <remarks>
        /// This checks various contexts where the invocation result is captured:
        /// - Assignment: var task = Method();
        /// - Variable declaration: var task = Method();
        /// - Return statement: return Method();
        /// - Argument: OtherMethod(Method());
        /// - Conditional expression: condition ? Method() : null;
        ///
        /// If none of these apply, the result is discarded (statement expression).
        /// </remarks>
        private static bool IsInvocationResultUsed(InvocationExpressionSyntax invocation)
        {
            var parent = invocation.Parent;

            // Check if used in assignment
            if (parent is AssignmentExpressionSyntax assignment &&
                assignment.Right == invocation)
                return true;

            // Check if used in variable declaration
            if (parent is EqualsValueClauseSyntax equalsValue &&
                equalsValue.Value == invocation)
                return true;

            // Check if used in return statement
            if (parent is ReturnStatementSyntax returnStmt &&
                returnStmt.Expression == invocation)
                return true;

            // Check if used as argument
            if (parent is ArgumentSyntax)
                return true;

            // Check if used in conditional expression
            if (parent is ConditionalExpressionSyntax)
                return true;

            return false;
        }

        /// <summary>
        /// Gets the method body for analysis, handling both block and expression bodies.
        /// </summary>
        /// <param name="methodNode">The method syntax node (method, local function, or lambda).</param>
        /// <returns>The body syntax node, or null if the method has no body.</returns>
        /// <remarks>
        /// This handles:
        /// - Regular methods: void M() { } or int M() => 42;
        /// - Local functions: void Local() { } or int Local() => 42;
        /// - Lambda expressions: x => x * 2 or x => { return x * 2; }
        ///
        /// Returns the BlockSyntax for block bodies or the expression for expression bodies.
        /// </remarks>
        public static SyntaxNode GetMethodBody(SyntaxNode methodNode)
        {
            if (methodNode is MethodDeclarationSyntax methodDecl)
            {
                return methodDecl.Body ?? (SyntaxNode)methodDecl.ExpressionBody?.Expression;
            }
            else if (methodNode is LocalFunctionStatementSyntax localFunc)
            {
                return localFunc.Body ?? (SyntaxNode)localFunc.ExpressionBody?.Expression;
            }
            else if (methodNode is AnonymousFunctionExpressionSyntax lambda)
            {
                if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
                {
                    return simpleLambda.Block ?? (SyntaxNode)simpleLambda.ExpressionBody;
                }
                else if (lambda is ParenthesizedLambdaExpressionSyntax parenLambda)
                {
                    return parenLambda.Block ?? (SyntaxNode)parenLambda.ExpressionBody;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a method has the async modifier in its syntax declaration.
        /// </summary>
        /// <param name="methodNode">The method syntax node to check.</param>
        /// <returns>True if the async keyword is present; otherwise, false.</returns>
        /// <remarks>
        /// This is a syntax-level check (doesn't require semantic model).
        /// Works for methods, local functions, and lambda expressions.
        ///
        /// For semantic checks, use IsAsyncMethod(IMethodSymbol) instead.
        /// </remarks>
        public static bool HasAsyncModifier(SyntaxNode methodNode)
        {
            if (methodNode is MethodDeclarationSyntax methodDecl)
            {
                return methodDecl.Modifiers.Any(SyntaxKind.AsyncKeyword);
            }
            else if (methodNode is LocalFunctionStatementSyntax localFunc)
            {
                return localFunc.Modifiers.Any(SyntaxKind.AsyncKeyword);
            }
            else if (methodNode is AnonymousFunctionExpressionSyntax lambda)
            {
                return lambda.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword;
            }

            return false;
        }

        /// <summary>
        /// Gets comprehensive information about an async method.
        /// </summary>
        /// <param name="method">The method symbol to analyze.</param>
        /// <param name="methodNode">The method syntax node.</param>
        /// <param name="semanticModel">The semantic model for additional analysis.</param>
        /// <returns>An AsyncMethodInfo object containing all async-related information.</returns>
        /// <remarks>
        /// This is a convenience method that gathers all async-related information in one call.
        /// Useful for analyzers that need multiple pieces of information about async methods.
        /// </remarks>
        public static AsyncMethodInfo GetAsyncMethodInfo(
            IMethodSymbol method,
            SyntaxNode methodNode,
            SemanticModel semanticModel)
        {
            var compilation = semanticModel.Compilation;
            var body = GetMethodBody(methodNode);

            return new AsyncMethodInfo
            {
                Method = method,
                IsAsync = IsAsyncMethod(method),
                IsAsyncVoid = IsAsyncVoid(method, compilation),
                ReturnsTask = ReturnsTask(method, compilation),
                HasAwaitExpressions = body != null && GetAllAwaitExpressions(body).Any(),
                FirstAwaitExpression = body != null ? GetFirstAwaitExpression(body) : null
            };
        }
    }

    /// <summary>
    /// Contains comprehensive information about an async method.
    /// </summary>
    /// <remarks>
    /// This class aggregates various async-related properties for convenient analysis.
    /// Use AsyncMethodDetector.GetAsyncMethodInfo() to create instances.
    /// </remarks>
    public class AsyncMethodInfo
    {
        /// <summary>
        /// Gets or sets the method symbol.
        /// </summary>
        public IMethodSymbol Method { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method has the async modifier.
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method is async void.
        /// </summary>
        /// <remarks>
        /// Async void is generally problematic and should only be used for event handlers.
        /// </remarks>
        public bool IsAsyncVoid { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method returns Task or Task&lt;T&gt;.
        /// </summary>
        public bool ReturnsTask { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method contains any await expressions.
        /// </summary>
        /// <remarks>
        /// If false while IsAsync is true, the compiler will generate warning CS1998.
        /// </remarks>
        public bool HasAwaitExpressions { get; set; }

        /// <summary>
        /// Gets or sets the first await expression in the method, or null if none.
        /// </summary>
        /// <remarks>
        /// Code before this expression executes synchronously on the calling thread.
        /// </remarks>
        public AwaitExpressionSyntax FirstAwaitExpression { get; set; }
    }
}
