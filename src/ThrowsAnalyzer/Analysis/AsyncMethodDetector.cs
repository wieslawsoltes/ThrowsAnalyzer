using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Detects and analyzes async methods and their exception handling patterns.
    /// </summary>
    public static class AsyncMethodDetector
    {
        /// <summary>
        /// Checks if a method is async.
        /// </summary>
        public static bool IsAsyncMethod(IMethodSymbol method)
        {
            return method.IsAsync;
        }

        /// <summary>
        /// Checks if a method returns Task or Task<T>.
        /// </summary>
        public static bool ReturnsTask(IMethodSymbol method, Compilation compilation)
        {
            var returnType = method.ReturnType;
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
        /// Checks if a method is async void.
        /// </summary>
        public static bool IsAsyncVoid(IMethodSymbol method, Compilation compilation)
        {
            return method.IsAsync &&
                   method.ReturnType.SpecialType == SpecialType.System_Void;
        }

        /// <summary>
        /// Gets the first await expression in a method, or null if none.
        /// </summary>
        public static AwaitExpressionSyntax GetFirstAwaitExpression(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return null;

            return methodBody.DescendantNodes()
                .OfType<AwaitExpressionSyntax>()
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets all await expressions in a method.
        /// </summary>
        public static IEnumerable<AwaitExpressionSyntax> GetAllAwaitExpressions(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return Enumerable.Empty<AwaitExpressionSyntax>();

            return methodBody.DescendantNodes()
                .OfType<AwaitExpressionSyntax>();
        }

        /// <summary>
        /// Checks if a throw statement occurs before the first await.
        /// </summary>
        public static bool IsThrowBeforeFirstAwait(
            SyntaxNode throwNode,
            SyntaxNode methodBody)
        {
            if (methodBody == null)
                return false;

            var firstAwait = GetFirstAwaitExpression(methodBody);
            if (firstAwait == null)
            {
                // No await in method - all throws are "before first await"
                return true;
            }

            // Compare positions
            var throwPosition = throwNode.SpanStart;
            var awaitPosition = firstAwait.SpanStart;

            return throwPosition < awaitPosition;
        }

        /// <summary>
        /// Gets all Task-returning method invocations that are not awaited.
        /// </summary>
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
        /// Checks if an invocation's result is used (assigned, returned, or passed as argument).
        /// </summary>
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
        /// Checks if a method has the async modifier in its syntax.
        /// </summary>
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
        /// Gets information about a task-returning method that may throw.
        /// </summary>
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
    /// Contains information about an async method.
    /// </summary>
    public class AsyncMethodInfo
    {
        /// <summary>
        /// The method symbol.
        /// </summary>
        public IMethodSymbol Method { get; set; }

        /// <summary>
        /// True if the method has the async modifier.
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// True if the method is async void.
        /// </summary>
        public bool IsAsyncVoid { get; set; }

        /// <summary>
        /// True if the method returns Task or Task<T>.
        /// </summary>
        public bool ReturnsTask { get; set; }

        /// <summary>
        /// True if the method contains any await expressions.
        /// </summary>
        public bool HasAwaitExpressions { get; set; }

        /// <summary>
        /// The first await expression in the method, or null if none.
        /// </summary>
        public AwaitExpressionSyntax FirstAwaitExpression { get; set; }
    }
}
