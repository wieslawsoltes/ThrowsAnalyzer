using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Detects and analyzes iterator methods (methods using yield return/yield break).
    /// </summary>
    public static class IteratorMethodDetector
    {
        /// <summary>
        /// Checks if a method is an iterator method (uses yield return or yield break).
        /// </summary>
        public static bool IsIteratorMethod(IMethodSymbol method, SyntaxNode methodNode)
        {
            var body = GetMethodBody(methodNode);
            if (body == null)
                return false;

            return HasYieldStatements(body);
        }

        /// <summary>
        /// Checks if a method returns IEnumerable or IEnumerator.
        /// </summary>
        public static bool ReturnsEnumerable(IMethodSymbol method, Compilation compilation)
        {
            var returnType = method.ReturnType;
            if (returnType == null)
                return false;

            var ienumerableType = compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
            var ienumeratorType = compilation.GetTypeByMetadataName("System.Collections.IEnumerator");
            var ienumerableOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
            var ienumeratorOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerator`1");

            // Check if return type implements IEnumerable or IEnumerator
            if (ienumerableType != null && ImplementsInterface(returnType, ienumerableType))
                return true;

            if (ienumeratorType != null && ImplementsInterface(returnType, ienumeratorType))
                return true;

            if (ienumerableOfT != null && ImplementsGenericInterface(returnType, ienumerableOfT))
                return true;

            if (ienumeratorOfT != null && ImplementsGenericInterface(returnType, ienumeratorOfT))
                return true;

            return false;
        }

        /// <summary>
        /// Gets all yield return statements in a method.
        /// </summary>
        public static IEnumerable<YieldStatementSyntax> GetYieldReturnStatements(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return Enumerable.Empty<YieldStatementSyntax>();

            return methodBody.DescendantNodes()
                .OfType<YieldStatementSyntax>()
                .Where(y => y.IsKind(SyntaxKind.YieldReturnStatement));
        }

        /// <summary>
        /// Gets all yield break statements in a method.
        /// </summary>
        public static IEnumerable<YieldStatementSyntax> GetYieldBreakStatements(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return Enumerable.Empty<YieldStatementSyntax>();

            return methodBody.DescendantNodes()
                .OfType<YieldStatementSyntax>()
                .Where(y => y.IsKind(SyntaxKind.YieldBreakStatement));
        }

        /// <summary>
        /// Checks if method body contains any yield statements.
        /// </summary>
        public static bool HasYieldStatements(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return false;

            return methodBody.DescendantNodes()
                .OfType<YieldStatementSyntax>()
                .Any();
        }

        /// <summary>
        /// Gets all throw statements in an iterator method.
        /// </summary>
        public static IEnumerable<ThrowStatementSyntax> GetThrowStatements(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return Enumerable.Empty<ThrowStatementSyntax>();

            return methodBody.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Where(t => t.Expression != null); // Exclude bare rethrows
        }

        /// <summary>
        /// Gets all try-finally statements in an iterator method.
        /// </summary>
        public static IEnumerable<TryStatementSyntax> GetTryFinallyStatements(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return Enumerable.Empty<TryStatementSyntax>();

            return methodBody.DescendantNodes()
                .OfType<TryStatementSyntax>()
                .Where(t => t.Finally != null);
        }

        /// <summary>
        /// Checks if a throw statement is before the first yield.
        /// </summary>
        public static bool IsThrowBeforeFirstYield(SyntaxNode throwNode, SyntaxNode methodBody)
        {
            if (methodBody == null)
                return false;

            var yieldStatements = methodBody.DescendantNodes()
                .OfType<YieldStatementSyntax>()
                .ToList();

            if (yieldStatements.Count == 0)
            {
                // No yield statements - all throws are "before first yield"
                return true;
            }

            // Find the first yield
            var firstYield = yieldStatements.OrderBy(y => y.SpanStart).First();

            // Compare positions
            var throwPosition = throwNode.SpanStart;
            var yieldPosition = firstYield.SpanStart;

            return throwPosition < yieldPosition;
        }

        /// <summary>
        /// Checks if a try-finally contains yield statements in the try block.
        /// </summary>
        public static bool HasYieldInTryBlock(TryStatementSyntax tryStatement)
        {
            if (tryStatement.Block == null)
                return false;

            return tryStatement.Block.DescendantNodes()
                .OfType<YieldStatementSyntax>()
                .Any();
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

            return null;
        }

        /// <summary>
        /// Gets comprehensive information about an iterator method.
        /// </summary>
        public static IteratorMethodInfo GetIteratorMethodInfo(
            IMethodSymbol method,
            SyntaxNode methodNode,
            Compilation compilation)
        {
            var body = GetMethodBody(methodNode);

            return new IteratorMethodInfo
            {
                Method = method,
                IsIterator = IsIteratorMethod(method, methodNode),
                ReturnsEnumerable = ReturnsEnumerable(method, compilation),
                YieldReturnCount = body != null ? GetYieldReturnStatements(body).Count() : 0,
                YieldBreakCount = body != null ? GetYieldBreakStatements(body).Count() : 0,
                ThrowCount = body != null ? GetThrowStatements(body).Count() : 0,
                TryFinallyCount = body != null ? GetTryFinallyStatements(body).Count() : 0
            };
        }

        private static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol interfaceType)
        {
            if (SymbolEqualityComparer.Default.Equals(type, interfaceType))
                return true;

            return type.AllInterfaces.Any(i =>
                SymbolEqualityComparer.Default.Equals(i, interfaceType));
        }

        private static bool ImplementsGenericInterface(ITypeSymbol type, INamedTypeSymbol genericInterfaceType)
        {
            if (type is INamedTypeSymbol namedType)
            {
                if (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, genericInterfaceType))
                    return true;
            }

            return type.AllInterfaces.Any(i =>
                SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, genericInterfaceType));
        }
    }

    /// <summary>
    /// Contains information about an iterator method.
    /// </summary>
    public class IteratorMethodInfo
    {
        /// <summary>
        /// The method symbol.
        /// </summary>
        public IMethodSymbol Method { get; set; }

        /// <summary>
        /// True if the method uses yield return or yield break.
        /// </summary>
        public bool IsIterator { get; set; }

        /// <summary>
        /// True if the method returns IEnumerable or IEnumerator.
        /// </summary>
        public bool ReturnsEnumerable { get; set; }

        /// <summary>
        /// Number of yield return statements.
        /// </summary>
        public int YieldReturnCount { get; set; }

        /// <summary>
        /// Number of yield break statements.
        /// </summary>
        public int YieldBreakCount { get; set; }

        /// <summary>
        /// Number of throw statements.
        /// </summary>
        public int ThrowCount { get; set; }

        /// <summary>
        /// Number of try-finally statements.
        /// </summary>
        public int TryFinallyCount { get; set; }
    }
}
