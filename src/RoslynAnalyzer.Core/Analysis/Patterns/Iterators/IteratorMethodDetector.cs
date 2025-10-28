using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalyzer.Core.Analysis.Patterns.Iterators
{
    /// <summary>
    /// Detects and analyzes iterator methods (methods using yield return/yield break).
    /// </summary>
    /// <remarks>
    /// This detector provides utilities for analyzing C# iterator methods:
    /// - Iterator method identification (yield return/yield break usage)
    /// - IEnumerable/IEnumerator return type checking
    /// - Yield statement location tracking
    /// - Try-finally block analysis in iterators
    /// - Synchronous vs deferred execution detection
    ///
    /// Iterator methods use compiler-generated state machines and have special semantics:
    /// - Code doesn't execute until enumeration begins
    /// - Exceptions aren't thrown until MoveNext() is called
    /// - Finally blocks execute during Dispose()
    ///
    /// These utilities are useful for:
    /// - Building analyzers for iterator best practices
    /// - Detecting problematic patterns in iterators
    /// - Analyzing control flow in iterators
    /// - Finding synchronous validation before first yield
    /// </remarks>
    public static class IteratorMethodDetector
    {
        /// <summary>
        /// Checks if a method is an iterator method (uses yield return or yield break).
        /// </summary>
        /// <param name="method">The method symbol to check.</param>
        /// <param name="methodNode">The method syntax node.</param>
        /// <returns>True if the method contains yield statements; otherwise, false.</returns>
        /// <remarks>
        /// Iterator methods are identified by the presence of yield return or yield break statements.
        /// The compiler generates a state machine for these methods.
        ///
        /// Note: A method can return IEnumerable without being an iterator (by returning a collection).
        /// This method specifically checks for the yield syntax.
        /// </remarks>
        public static bool IsIteratorMethod(IMethodSymbol method, SyntaxNode methodNode)
        {
            var body = GetMethodBody(methodNode);
            if (body == null)
                return false;

            return HasYieldStatements(body);
        }

        /// <summary>
        /// Checks if a method returns IEnumerable or IEnumerator (generic or non-generic).
        /// </summary>
        /// <param name="method">The method symbol to check.</param>
        /// <param name="compilation">The compilation context for type resolution.</param>
        /// <returns>True if the return type is an enumerable or enumerator interface; otherwise, false.</returns>
        /// <remarks>
        /// This checks for four interface types:
        /// - System.Collections.IEnumerable
        /// - System.Collections.IEnumerator
        /// - System.Collections.Generic.IEnumerable&lt;T&gt;
        /// - System.Collections.Generic.IEnumerator&lt;T&gt;
        ///
        /// Methods returning these types can be iterators but don't have to be.
        /// Use IsIteratorMethod() to check for actual yield usage.
        /// </remarks>
        public static bool ReturnsEnumerable(IMethodSymbol method, Compilation compilation)
        {
            var returnType = method?.ReturnType;
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
        /// Gets all yield return statements in a method body.
        /// </summary>
        /// <param name="methodBody">The method body to analyze.</param>
        /// <returns>An enumerable of all yield return statements.</returns>
        /// <remarks>
        /// Yield return statements produce values in the iteration.
        /// Each yield return pauses execution and returns control to the caller.
        /// </remarks>
        public static IEnumerable<YieldStatementSyntax> GetYieldReturnStatements(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return Enumerable.Empty<YieldStatementSyntax>();

            return methodBody.DescendantNodes()
                .OfType<YieldStatementSyntax>()
                .Where(y => y.IsKind(SyntaxKind.YieldReturnStatement));
        }

        /// <summary>
        /// Gets all yield break statements in a method body.
        /// </summary>
        /// <param name="methodBody">The method body to analyze.</param>
        /// <returns>An enumerable of all yield break statements.</returns>
        /// <remarks>
        /// Yield break statements terminate the iteration early.
        /// They're equivalent to returning from the method but for iterators.
        /// </remarks>
        public static IEnumerable<YieldStatementSyntax> GetYieldBreakStatements(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return Enumerable.Empty<YieldStatementSyntax>();

            return methodBody.DescendantNodes()
                .OfType<YieldStatementSyntax>()
                .Where(y => y.IsKind(SyntaxKind.YieldBreakStatement));
        }

        /// <summary>
        /// Checks if a method body contains any yield statements (return or break).
        /// </summary>
        /// <param name="methodBody">The method body to check.</param>
        /// <returns>True if any yield statements are found; otherwise, false.</returns>
        /// <remarks>
        /// This is a quick check for iterator methods without needing to enumerate all statements.
        /// </remarks>
        public static bool HasYieldStatements(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return false;

            return methodBody.DescendantNodes()
                .OfType<YieldStatementSyntax>()
                .Any();
        }

        /// <summary>
        /// Checks if a syntax node occurs before the first yield statement in a method.
        /// </summary>
        /// <param name="node">The syntax node to check (e.g., validation code, argument checks).</param>
        /// <param name="methodBody">The method body containing the node.</param>
        /// <returns>
        /// True if the node occurs before the first yield, or if there are no yields in the method;
        /// otherwise, false.
        /// </returns>
        /// <remarks>
        /// This is useful for detecting:
        /// - Argument validation that executes immediately (before deferred execution)
        /// - Synchronous operations before the iterator state machine begins
        /// - Early exceptions that are thrown at call time rather than enumeration time
        ///
        /// Code before the first yield executes when the method is called.
        /// Code after the first yield executes when MoveNext() is called during enumeration.
        /// </remarks>
        public static bool IsBeforeFirstYield(SyntaxNode node, SyntaxNode methodBody)
        {
            if (methodBody == null)
                return false;

            var yieldStatements = methodBody.DescendantNodes()
                .OfType<YieldStatementSyntax>()
                .ToList();

            if (yieldStatements.Count == 0)
            {
                // No yield statements - all code runs immediately
                return true;
            }

            // Find the first yield
            var firstYield = yieldStatements.OrderBy(y => y.SpanStart).First();

            // Compare positions
            var nodePosition = node.SpanStart;
            var yieldPosition = firstYield.SpanStart;

            return nodePosition < yieldPosition;
        }

        /// <summary>
        /// Checks if a try statement contains yield statements in its try block.
        /// </summary>
        /// <param name="tryStatement">The try statement to check.</param>
        /// <returns>True if the try block contains yield statements; otherwise, false.</returns>
        /// <remarks>
        /// Try-finally blocks with yields have special semantics:
        /// - The finally block executes when Dispose() is called on the enumerator
        /// - This happens automatically in foreach loops
        /// - Manual enumeration must call Dispose() to ensure finally blocks execute
        ///
        /// This is useful for detecting resource management patterns in iterators.
        /// </remarks>
        public static bool HasYieldInTryBlock(TryStatementSyntax tryStatement)
        {
            if (tryStatement?.Block == null)
                return false;

            return tryStatement.Block.DescendantNodes()
                .OfType<YieldStatementSyntax>()
                .Any();
        }

        /// <summary>
        /// Gets all throw statements in a method body (excludes bare rethrows).
        /// </summary>
        /// <param name="methodBody">The method body to search.</param>
        /// <returns>All throw statements with an expression (excludes bare throws).</returns>
        /// <remarks>
        /// This method finds all throw statements that throw a new exception.
        /// Bare rethrows (just "throw;" without an expression) are excluded.
        ///
        /// Useful for:
        /// - Analyzing exception patterns
        /// - Detecting validation code
        /// - Finding error handling logic
        /// </remarks>
        public static IEnumerable<ThrowStatementSyntax> GetThrowStatements(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return Enumerable.Empty<ThrowStatementSyntax>();

            return methodBody.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Where(t => t.Expression != null); // Exclude bare rethrows
        }

        /// <summary>
        /// Gets all try-finally statements in a method body.
        /// </summary>
        /// <param name="methodBody">The method body to search.</param>
        /// <returns>All try statements that have a finally block.</returns>
        /// <remarks>
        /// This finds try-finally and try-catch-finally blocks (any try with a finally).
        /// Useful for:
        /// - Detecting resource cleanup patterns
        /// - Analyzing iterator finally blocks
        /// - Finding deterministic cleanup code
        /// </remarks>
        public static IEnumerable<TryStatementSyntax> GetTryFinallyStatements(SyntaxNode methodBody)
        {
            if (methodBody == null)
                return Enumerable.Empty<TryStatementSyntax>();

            return methodBody.DescendantNodes()
                .OfType<TryStatementSyntax>()
                .Where(t => t.Finally != null);
        }

        /// <summary>
        /// Gets the method body for analysis, handling both block and expression bodies.
        /// </summary>
        /// <param name="methodNode">The method syntax node.</param>
        /// <returns>The body syntax node, or null if the method has no body.</returns>
        /// <remarks>
        /// This handles:
        /// - Regular methods: IEnumerable&lt;int&gt; M() { yield return 1; } or IEnumerable&lt;int&gt; M() => GetItems();
        /// - Local functions: IEnumerable&lt;int&gt; Local() { yield return 1; }
        ///
        /// Note: Expression-bodied methods cannot be iterators (no place for yield).
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

            return null;
        }

        /// <summary>
        /// Gets comprehensive information about an iterator method.
        /// </summary>
        /// <param name="method">The method symbol to analyze.</param>
        /// <param name="methodNode">The method syntax node.</param>
        /// <param name="compilation">The compilation context.</param>
        /// <returns>An IteratorMethodInfo object containing all iterator-related information.</returns>
        /// <remarks>
        /// This is a convenience method that gathers all iterator-related information in one call.
        /// Useful for analyzers that need multiple pieces of information about iterator methods.
        /// </remarks>
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
                YieldBreakCount = body != null ? GetYieldBreakStatements(body).Count() : 0
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
    /// Contains comprehensive information about an iterator method.
    /// </summary>
    /// <remarks>
    /// This class aggregates various iterator-related properties for convenient analysis.
    /// Use IteratorMethodDetector.GetIteratorMethodInfo() to create instances.
    /// </remarks>
    public class IteratorMethodInfo
    {
        /// <summary>
        /// Gets or sets the method symbol.
        /// </summary>
        public IMethodSymbol Method { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method uses yield return or yield break.
        /// </summary>
        public bool IsIterator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method returns IEnumerable or IEnumerator.
        /// </summary>
        public bool ReturnsEnumerable { get; set; }

        /// <summary>
        /// Gets or sets the number of yield return statements.
        /// </summary>
        public int YieldReturnCount { get; set; }

        /// <summary>
        /// Gets or sets the number of yield break statements.
        /// </summary>
        public int YieldBreakCount { get; set; }
    }
}
