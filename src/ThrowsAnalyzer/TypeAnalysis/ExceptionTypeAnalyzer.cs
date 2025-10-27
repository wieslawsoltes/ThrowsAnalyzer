using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer.TypeAnalysis
{
    /// <summary>
    /// Provides semantic model-based exception type analysis.
    /// Extracts and analyzes exception types from throw statements and catch clauses.
    /// </summary>
    public static class ExceptionTypeAnalyzer
    {
        /// <summary>
        /// Gets the exception type being thrown, or null if it cannot be determined.
        /// </summary>
        /// <param name="throwNode">ThrowStatementSyntax or ThrowExpressionSyntax</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <returns>ITypeSymbol for the exception, or null</returns>
        public static ITypeSymbol GetThrownExceptionType(
            SyntaxNode throwNode,
            SemanticModel semanticModel)
        {
            ExpressionSyntax expression = throwNode switch
            {
                // throw new ArgumentException();
                ThrowStatementSyntax throwStmt => throwStmt.Expression,

                // x ?? throw new InvalidOperationException()
                ThrowExpressionSyntax throwExpr => throwExpr.Expression,

                _ => null
            };

            if (expression == null)
            {
                // Bare rethrow: throw;
                // Type is unknown without additional context analysis
                return null;
            }

            // Handle different expression types
            return expression switch
            {
                // throw new ExceptionType(...)
                ObjectCreationExpressionSyntax objectCreation
                    => semanticModel.GetTypeInfo(objectCreation).Type,

                // throw exceptionVariable
                IdentifierNameSyntax identifier
                    => semanticModel.GetTypeInfo(identifier).Type,

                // throw GetException()
                InvocationExpressionSyntax invocation
                    => semanticModel.GetTypeInfo(invocation).Type,

                // throw condition ? new Ex1() : new Ex2()
                ConditionalExpressionSyntax conditional
                    => GetCommonExceptionType(conditional, semanticModel),

                // throw ex ?? new Exception()
                BinaryExpressionSyntax binary
                    => semanticModel.GetTypeInfo(binary).Type,

                _ => semanticModel.GetTypeInfo(expression).Type
            };
        }

        /// <summary>
        /// Gets the exception type(s) caught by a catch clause.
        /// </summary>
        /// <param name="catchClause">The catch clause to analyze</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <returns>ITypeSymbol for the exception type, or null for general catch</returns>
        public static ITypeSymbol GetCaughtExceptionType(
            CatchClauseSyntax catchClause,
            SemanticModel semanticModel)
        {
            // catch { } or catch (Exception) { }
            if (catchClause.Declaration == null)
            {
                // General catch - catches System.Exception
                return semanticModel.Compilation.GetTypeByMetadataName("System.Exception");
            }

            // catch (ArgumentException ex) { }
            var type = catchClause.Declaration.Type;
            var typeInfo = semanticModel.GetTypeInfo(type);
            return typeInfo.Type;
        }

        /// <summary>
        /// Determines if exceptionType inherits from or is System.Exception.
        /// </summary>
        /// <param name="typeSymbol">The type to check</param>
        /// <param name="compilation">Compilation context</param>
        /// <returns>True if the type is an exception type</returns>
        public static bool IsExceptionType(
            ITypeSymbol typeSymbol,
            Compilation compilation)
        {
            if (typeSymbol == null)
                return false;

            var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
            if (exceptionType == null)
                return false;

            // Check if typeSymbol inherits from System.Exception
            return IsAssignableTo(typeSymbol, exceptionType, compilation);
        }

        /// <summary>
        /// Checks if derivedType is assignable to baseType (inheritance check).
        /// </summary>
        /// <param name="derivedType">The potentially derived type</param>
        /// <param name="baseType">The potential base type</param>
        /// <param name="compilation">Compilation context</param>
        /// <returns>True if derivedType is assignable to baseType</returns>
        public static bool IsAssignableTo(
            ITypeSymbol derivedType,
            ITypeSymbol baseType,
            Compilation compilation)
        {
            if (derivedType == null || baseType == null)
                return false;

            // Direct match
            if (SymbolEqualityComparer.Default.Equals(derivedType, baseType))
                return true;

            // Walk up the inheritance chain
            var currentType = derivedType.BaseType;
            while (currentType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(currentType, baseType))
                    return true;
                currentType = currentType.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Gets all exception types in the inheritance hierarchy.
        /// Returns list from most derived to System.Exception.
        /// </summary>
        /// <param name="exceptionType">The exception type to analyze</param>
        /// <param name="compilation">Compilation context</param>
        /// <returns>Enumerable of types in the hierarchy</returns>
        public static IEnumerable<ITypeSymbol> GetExceptionHierarchy(
            ITypeSymbol exceptionType,
            Compilation compilation)
        {
            var hierarchy = new List<ITypeSymbol>();
            var current = exceptionType;

            while (current != null)
            {
                hierarchy.Add(current);
                current = current.BaseType;
            }

            return hierarchy;
        }

        /// <summary>
        /// Gets the common exception type from a conditional expression.
        /// Used for: throw condition ? new Ex1() : new Ex2()
        /// </summary>
        private static ITypeSymbol GetCommonExceptionType(
            ConditionalExpressionSyntax conditional,
            SemanticModel semanticModel)
        {
            // Get the type of the entire conditional expression
            var typeInfo = semanticModel.GetTypeInfo(conditional);
            return typeInfo.Type;
        }
    }
}
