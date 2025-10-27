# Exception Type Analysis - Detailed Implementation Plan

## Executive Summary

This document provides a comprehensive implementation plan for adding semantic model-based exception type analysis to ThrowsAnalyzer. This enhancement will enable type-aware exception detection, catch clause validation, exception hierarchy analysis, and advanced diagnostics that go beyond syntax-only analysis.

## Current Architecture Overview

### Existing Components

**Detectors (Syntax-Based):**
- `ThrowStatementDetector` - Detects throw statements/expressions syntactically
- `TryCatchDetector` - Detects try-catch blocks syntactically
- `UnhandledThrowDetector` - Detects throws outside try blocks (syntax-only)

**Analyzers:**
- `MethodThrowsAnalyzer` (THROWS001) - Reports methods with throws
- `UnhandledThrowsAnalyzer` (THROWS002) - Reports unhandled throws
- `TryCatchAnalyzer` (THROWS003) - Reports try-catch blocks

**Member Support:**
- Full support for 10 executable member types (methods, constructors, properties, operators, lambdas, local functions, etc.)
- Generic `ExecutableMemberHelper` for member detection
- Configuration system for granular control

**Limitations:**
- No exception type information (can't distinguish `ArgumentException` from `InvalidOperationException`)
- No type hierarchy awareness (can't detect if `catch (Exception)` catches all)
- No detection of catch clause ordering issues
- No detection of unreachable catch clauses
- No validation of exception types being thrown vs caught

## Goals and Objectives

### Primary Goals
1. **Type-Aware Exception Detection** - Identify specific exception types being thrown
2. **Catch Clause Validation** - Detect ordering issues, dead clauses, and overly broad catches
3. **Exception Hierarchy Analysis** - Understand inheritance relationships between exception types
4. **Type-Based Diagnostics** - Enable new analyzers that require type information

### Non-Goals (Out of Scope)
- Cross-method exception flow analysis (Phase 3 feature)
- Exception propagation through call graphs (requires interprocedural analysis)
- Runtime exception tracking
- Exception documentation validation (XML doc comments)

## Implementation Phases

### Phase 1: Foundation - Exception Type Detection (2-3 weeks)

#### 1.1 Core Type Detection Infrastructure

**New Component: `ExceptionTypeAnalyzer`**

```csharp
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
        public static ITypeSymbol? GetThrownExceptionType(
            SyntaxNode throwNode,
            SemanticModel semanticModel)
        {
            // Implementation details below
        }

        /// <summary>
        /// Gets the exception type(s) caught by a catch clause.
        /// </summary>
        public static ITypeSymbol? GetCaughtExceptionType(
            CatchClauseSyntax catchClause,
            SemanticModel semanticModel)
        {
            // Implementation details below
        }

        /// <summary>
        /// Determines if exceptionType inherits from or is System.Exception.
        /// </summary>
        public static bool IsExceptionType(
            ITypeSymbol typeSymbol,
            Compilation compilation)
        {
            // Implementation details below
        }

        /// <summary>
        /// Checks if derivedType is assignable to baseType (inheritance check).
        /// </summary>
        public static bool IsAssignableTo(
            ITypeSymbol derivedType,
            ITypeSymbol baseType,
            Compilation compilation)
        {
            // Implementation details below
        }

        /// <summary>
        /// Gets all exception types in the inheritance hierarchy.
        /// Returns list from most derived to System.Exception.
        /// </summary>
        public static IEnumerable<ITypeSymbol> GetExceptionHierarchy(
            ITypeSymbol exceptionType,
            Compilation compilation)
        {
            // Implementation details below
        }
    }
}
```

**Implementation Details:**

```csharp
public static ITypeSymbol? GetThrownExceptionType(
    SyntaxNode throwNode,
    SemanticModel semanticModel)
{
    ExpressionSyntax? expression = throwNode switch
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

public static ITypeSymbol? GetCaughtExceptionType(
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
```

#### 1.2 Enhanced Throw Detection with Type Information

**New Component: `TypedThrowInfo`**

```csharp
namespace ThrowsAnalyzer.TypeAnalysis
{
    /// <summary>
    /// Contains information about a throw statement including its type.
    /// </summary>
    public class TypedThrowInfo
    {
        public SyntaxNode ThrowNode { get; init; }
        public ITypeSymbol? ExceptionType { get; init; }
        public bool IsRethrow { get; init; }
        public Location Location { get; init; }

        public string ExceptionTypeName => ExceptionType?.ToDisplayString()
            ?? (IsRethrow ? "(rethrow)" : "(unknown)");
    }
}
```

**Enhanced Detector: `TypedThrowDetector`**

```csharp
namespace ThrowsAnalyzer.TypeAnalysis
{
    /// <summary>
    /// Enhanced throw detector that includes type information.
    /// Builds on ThrowStatementDetector with semantic analysis.
    /// </summary>
    public static class TypedThrowDetector
    {
        /// <summary>
        /// Gets all throws in a member with type information.
        /// </summary>
        public static IEnumerable<TypedThrowInfo> GetTypedThrows(
            SyntaxNode node,
            SemanticModel semanticModel)
        {
            var executableBlocks = ExecutableMemberHelper.GetExecutableBlocks(node);
            var throws = new List<TypedThrowInfo>();

            foreach (var block in executableBlocks)
            {
                var throwNodes = GetThrowNodes(block);

                foreach (var throwNode in throwNodes)
                {
                    var exceptionType = ExceptionTypeAnalyzer
                        .GetThrownExceptionType(throwNode, semanticModel);

                    var isRethrow = IsRethrow(throwNode);

                    throws.Add(new TypedThrowInfo
                    {
                        ThrowNode = throwNode,
                        ExceptionType = exceptionType,
                        IsRethrow = isRethrow,
                        Location = throwNode.GetLocation()
                    });
                }
            }

            return throws;
        }

        private static IEnumerable<SyntaxNode> GetThrowNodes(SyntaxNode node)
        {
            // Get throw statements and expressions, excluding nested members
            return node.DescendantNodes(n => !IsNestedExecutableMember(n))
                .Where(n => n is ThrowStatementSyntax or ThrowExpressionSyntax);
        }

        private static bool IsRethrow(SyntaxNode throwNode)
        {
            // throw; (bare rethrow has null expression)
            return throwNode is ThrowStatementSyntax stmt && stmt.Expression == null;
        }

        private static bool IsNestedExecutableMember(SyntaxNode node)
        {
            return node is LocalFunctionStatementSyntax
                or SimpleLambdaExpressionSyntax
                or ParenthesizedLambdaExpressionSyntax
                or AnonymousMethodExpressionSyntax;
        }
    }
}
```

#### 1.3 Testing Strategy for Phase 1

**Test Categories:**

1. **Type Resolution Tests**
   - Resolve `throw new ArgumentException()`
   - Resolve `throw ex` where ex is a variable
   - Resolve `throw GetException()` where method returns exception
   - Resolve conditional throws: `throw condition ? new Ex1() : new Ex2()`
   - Handle unknown types gracefully

2. **Rethrow Detection Tests**
   - Detect bare rethrow: `throw;`
   - Detect rethrow with variable: `throw ex;`
   - Distinguish from new throw

3. **Exception Hierarchy Tests**
   - Verify `ArgumentException` inherits from `Exception`
   - Verify `InvalidOperationException` inherits from `SystemException`
   - Handle custom exception types
   - Handle types that don't inherit from Exception

4. **Edge Cases**
   - Null exception expressions
   - Generic exception types: `throw new CustomException<T>()`
   - Exception types from external assemblies
   - Malformed or incomplete code

**Test Example:**

```csharp
[Fact]
public async Task GetThrownExceptionType_SimpleObjectCreation_ReturnsCorrectType()
{
    var source = @"
using System;

class TestClass
{
    void Method()
    {
        throw new ArgumentException();
    }
}";

    var (semanticModel, throwNode) = await GetSemanticModelAndThrowNode(source);
    var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwNode, semanticModel);

    Assert.NotNull(exceptionType);
    Assert.Equal("System.ArgumentException", exceptionType.ToDisplayString());
}
```

### Phase 2: Catch Clause Analysis (2-3 weeks)

#### 2.1 Catch Clause Type Detector

**New Component: `CatchClauseInfo`**

```csharp
namespace ThrowsAnalyzer.TypeAnalysis
{
    /// <summary>
    /// Contains information about a catch clause including its type.
    /// </summary>
    public class CatchClauseInfo
    {
        public CatchClauseSyntax CatchClause { get; init; }
        public ITypeSymbol? ExceptionType { get; init; }
        public bool IsGeneralCatch { get; init; }
        public bool HasFilter { get; init; }
        public CatchFilterClauseSyntax? Filter { get; init; }
        public Location Location { get; init; }

        public string ExceptionTypeName => ExceptionType?.ToDisplayString()
            ?? "(general catch)";
    }
}
```

**New Component: `CatchClauseAnalyzer`**

```csharp
namespace ThrowsAnalyzer.TypeAnalysis
{
    /// <summary>
    /// Analyzes catch clauses with type information.
    /// Detects ordering issues, dead clauses, and overly broad catches.
    /// </summary>
    public static class CatchClauseAnalyzer
    {
        /// <summary>
        /// Gets all catch clauses in a try statement with type information.
        /// </summary>
        public static IEnumerable<CatchClauseInfo> GetCatchClauses(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel)
        {
            var catches = new List<CatchClauseInfo>();

            foreach (var catchClause in tryStatement.Catches)
            {
                var exceptionType = ExceptionTypeAnalyzer
                    .GetCaughtExceptionType(catchClause, semanticModel);

                var isGeneralCatch = catchClause.Declaration == null;
                var hasFilter = catchClause.Filter != null;

                catches.Add(new CatchClauseInfo
                {
                    CatchClause = catchClause,
                    ExceptionType = exceptionType,
                    IsGeneralCatch = isGeneralCatch,
                    HasFilter = hasFilter,
                    Filter = catchClause.Filter,
                    Location = catchClause.GetLocation()
                });
            }

            return catches;
        }

        /// <summary>
        /// Detects catch clause ordering issues.
        /// Returns clauses that are unreachable due to previous broader catches.
        /// </summary>
        public static IEnumerable<CatchClauseOrderingIssue> DetectOrderingIssues(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel)
        {
            var catches = GetCatchClauses(tryStatement, semanticModel).ToList();
            var issues = new List<CatchClauseOrderingIssue>();

            for (int i = 0; i < catches.Count; i++)
            {
                var current = catches[i];

                // Check if any previous catch already handles this type
                for (int j = 0; j < i; j++)
                {
                    var previous = catches[j];

                    // Skip if either has filter - filters change reachability
                    if (current.HasFilter || previous.HasFilter)
                        continue;

                    if (IsCaughtBy(current, previous, semanticModel.Compilation))
                    {
                        issues.Add(new CatchClauseOrderingIssue
                        {
                            UnreachableClause = current,
                            MaskedByClause = previous,
                            Reason = $"This catch is unreachable because " +
                                    $"'{previous.ExceptionTypeName}' is caught first"
                        });
                        break;
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Checks if currentCatch is made unreachable by previousCatch.
        /// </summary>
        private static bool IsCaughtBy(
            CatchClauseInfo currentCatch,
            CatchClauseInfo previousCatch,
            Compilation compilation)
        {
            // General catch catches everything
            if (previousCatch.IsGeneralCatch)
                return true;

            // If current is general catch but previous is not, it's reachable
            if (currentCatch.IsGeneralCatch)
                return false;

            // Check type hierarchy
            if (currentCatch.ExceptionType != null && previousCatch.ExceptionType != null)
            {
                return ExceptionTypeAnalyzer.IsAssignableTo(
                    currentCatch.ExceptionType,
                    previousCatch.ExceptionType,
                    compilation);
            }

            return false;
        }

        /// <summary>
        /// Detects empty catch blocks (exception swallowing).
        /// </summary>
        public static IEnumerable<CatchClauseInfo> DetectEmptyCatches(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel)
        {
            var catches = GetCatchClauses(tryStatement, semanticModel);

            return catches.Where(c => IsEmptyCatch(c.CatchClause));
        }

        private static bool IsEmptyCatch(CatchClauseSyntax catchClause)
        {
            var block = catchClause.Block;

            // No statements or only comments
            return block.Statements.Count == 0;
        }

        /// <summary>
        /// Detects catch blocks that only rethrow (unnecessary catch).
        /// </summary>
        public static IEnumerable<CatchClauseInfo> DetectRethrowOnlyCatches(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel)
        {
            var catches = GetCatchClauses(tryStatement, semanticModel);

            return catches.Where(c => IsRethrowOnly(c.CatchClause));
        }

        private static bool IsRethrowOnly(CatchClauseSyntax catchClause)
        {
            var block = catchClause.Block;

            // Only one statement and it's a bare rethrow
            if (block.Statements.Count == 1)
            {
                var stmt = block.Statements[0];
                if (stmt is ThrowStatementSyntax throwStmt && throwStmt.Expression == null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Detects overly broad catches (catching Exception or SystemException).
        /// </summary>
        public static IEnumerable<CatchClauseInfo> DetectOverlyBroadCatches(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel)
        {
            var catches = GetCatchClauses(tryStatement, semanticModel);
            var compilation = semanticModel.Compilation;

            var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
            var systemExceptionType = compilation.GetTypeByMetadataName("System.SystemException");

            return catches.Where(c =>
            {
                if (c.ExceptionType == null)
                    return false;

                return SymbolEqualityComparer.Default.Equals(c.ExceptionType, exceptionType)
                    || SymbolEqualityComparer.Default.Equals(c.ExceptionType, systemExceptionType);
            });
        }
    }
}
```

#### 2.2 New Diagnostic Rules

**THROWS004: Rethrow Anti-Pattern**

```csharp
// In MethodThrowsDiagnosticsBuilder.cs

public const string DiagnosticId004 = "THROWS004";

private static readonly LocalizableString Title004 =
    "Rethrow modifies stack trace";
private static readonly LocalizableString MessageFormat004 =
    "Method '{0}' rethrows exception with 'throw ex;' which modifies stack trace. Use 'throw;' instead";
private static readonly LocalizableString Description004 =
    "Detects rethrows that use 'throw ex;' instead of 'throw;', which preserves the original stack trace.";

public static readonly DiagnosticDescriptor RethrowAntiPattern = new DiagnosticDescriptor(
    DiagnosticId004,
    Title004,
    MessageFormat004,
    Category,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: Description004);
```

**THROWS007: Catch Clause Ordering Issue**

```csharp
public const string DiagnosticId007 = "THROWS007";

private static readonly LocalizableString Title007 =
    "Unreachable catch clause";
private static readonly LocalizableString MessageFormat007 =
    "Catch clause for '{0}' is unreachable because '{1}' is caught first";
private static readonly LocalizableString Description007 =
    "Detects catch clauses that are unreachable due to catch ordering.";

public static readonly DiagnosticDescriptor CatchClauseOrdering = new DiagnosticDescriptor(
    DiagnosticId007,
    Title007,
    MessageFormat007,
    Category,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: Description007);
```

**THROWS008: Empty Catch Block**

```csharp
public const string DiagnosticId008 = "THROWS008";

private static readonly LocalizableString Title008 =
    "Empty catch block swallows exceptions";
private static readonly LocalizableString MessageFormat008 =
    "Method '{0}' has empty catch block that swallows exceptions";
private static readonly LocalizableString Description008 =
    "Detects empty catch blocks that suppress exceptions without handling them.";

public static readonly DiagnosticDescriptor EmptyCatchBlock = new DiagnosticDescriptor(
    DiagnosticId008,
    Title008,
    MessageFormat008,
    Category,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: Description008);
```

**THROWS009: Catch Block Only Rethrows**

```csharp
public const string DiagnosticId009 = "THROWS009";

private static readonly LocalizableString Title009 =
    "Catch block only rethrows exception";
private static readonly LocalizableString MessageFormat009 =
    "Method '{0}' has catch block that only rethrows - consider removing unnecessary catch";
private static readonly LocalizableString Description009 =
    "Detects catch blocks that only rethrow without doing any work.";

public static readonly DiagnosticDescriptor RethrowOnlyCatch = new DiagnosticDescriptor(
    DiagnosticId009,
    Title009,
    MessageFormat009,
    Category,
    DiagnosticSeverity.Info,
    isEnabledByDefault: true,
    description: Description009);
```

**THROWS010: Overly Broad Exception Catch**

```csharp
public const string DiagnosticId010 = "THROWS010";

private static readonly LocalizableString Title010 =
    "Overly broad exception catch";
private static readonly LocalizableString MessageFormat010 =
    "Method '{0}' catches '{1}' which is too broad - consider catching specific exception types";
private static readonly LocalizableString Description010 =
    "Detects catch clauses for System.Exception or System.SystemException.";

public static readonly DiagnosticDescriptor OverlyBroadCatch = new DiagnosticDescriptor(
    DiagnosticId010,
    Title010,
    MessageFormat010,
    Category,
    DiagnosticSeverity.Info,
    isEnabledByDefault: true,
    description: Description010);
```

#### 2.3 New Analyzers

**RethrowAntiPatternAnalyzer**

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RethrowAntiPatternAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.RethrowAntiPattern);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeThrowStatement,
            SyntaxKind.ThrowStatement);
    }

    private static void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
    {
        var throwStmt = (ThrowStatementSyntax)context.Node;

        // Check if this is inside a catch block
        var catchClause = throwStmt.Ancestors()
            .OfType<CatchClauseSyntax>()
            .FirstOrDefault();

        if (catchClause == null)
            return;

        // Check if it's rethrowing the caught exception variable
        if (throwStmt.Expression is IdentifierNameSyntax identifier)
        {
            var catchDeclaration = catchClause.Declaration;
            if (catchDeclaration?.Identifier.Text == identifier.Identifier.Text)
            {
                // This is "throw ex;" anti-pattern
                var memberNode = GetContainingMember(throwStmt);
                if (memberNode != null)
                {
                    var memberName = ExecutableMemberHelper.GetMemberDisplayName(memberNode);
                    var diagnostic = Diagnostic.Create(
                        MethodThrowsDiagnosticsBuilder.RethrowAntiPattern,
                        throwStmt.GetLocation(),
                        memberName);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
```

**CatchClauseOrderingAnalyzer**

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CatchClauseOrderingAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            MethodThrowsDiagnosticsBuilder.CatchClauseOrdering,
            MethodThrowsDiagnosticsBuilder.EmptyCatchBlock,
            MethodThrowsDiagnosticsBuilder.RethrowOnlyCatch,
            MethodThrowsDiagnosticsBuilder.OverlyBroadCatch);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeTryStatement,
            SyntaxKind.TryStatement);
    }

    private static void AnalyzeTryStatement(SyntaxNodeAnalysisContext context)
    {
        var tryStmt = (TryStatementSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Get containing member for error messages
        var memberNode = GetContainingMember(tryStmt);
        if (memberNode == null)
            return;

        var memberName = ExecutableMemberHelper.GetMemberDisplayName(memberNode);

        // Check ordering issues
        var orderingIssues = CatchClauseAnalyzer
            .DetectOrderingIssues(tryStmt, semanticModel);

        foreach (var issue in orderingIssues)
        {
            var diagnostic = Diagnostic.Create(
                MethodThrowsDiagnosticsBuilder.CatchClauseOrdering,
                issue.UnreachableClause.Location,
                issue.UnreachableClause.ExceptionTypeName,
                issue.MaskedByClause.ExceptionTypeName);

            context.ReportDiagnostic(diagnostic);
        }

        // Check empty catches
        var emptyCatches = CatchClauseAnalyzer
            .DetectEmptyCatches(tryStmt, semanticModel);

        foreach (var catchInfo in emptyCatches)
        {
            var diagnostic = Diagnostic.Create(
                MethodThrowsDiagnosticsBuilder.EmptyCatchBlock,
                catchInfo.Location,
                memberName);

            context.ReportDiagnostic(diagnostic);
        }

        // Check rethrow-only catches
        var rethrowOnlyCatches = CatchClauseAnalyzer
            .DetectRethrowOnlyCatches(tryStmt, semanticModel);

        foreach (var catchInfo in rethrowOnlyCatches)
        {
            var diagnostic = Diagnostic.Create(
                MethodThrowsDiagnosticsBuilder.RethrowOnlyCatch,
                catchInfo.Location,
                memberName);

            context.ReportDiagnostic(diagnostic);
        }

        // Check overly broad catches
        var broadCatches = CatchClauseAnalyzer
            .DetectOverlyBroadCatches(tryStmt, semanticModel);

        foreach (var catchInfo in broadCatches)
        {
            var diagnostic = Diagnostic.Create(
                MethodThrowsDiagnosticsBuilder.OverlyBroadCatch,
                catchInfo.Location,
                memberName,
                catchInfo.ExceptionTypeName);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

#### 2.4 Testing Strategy for Phase 2

**Test Categories:**

1. **Catch Clause Type Resolution**
   - Resolve `catch (ArgumentException ex)`
   - Resolve `catch (Exception)`
   - Resolve `catch` (general catch)
   - Handle exception types from external assemblies

2. **Ordering Detection**
   - Detect `catch (Exception)` before `catch (ArgumentException)`
   - Detect `catch (SystemException)` before `catch (InvalidOperationException)`
   - Allow correct ordering
   - Handle filters correctly (filters change reachability)

3. **Empty Catch Detection**
   - Detect `catch { }` with no statements
   - Allow catch with logging statements
   - Handle catch with comments only

4. **Rethrow-Only Detection**
   - Detect `catch { throw; }`
   - Allow catch with logging + rethrow
   - Detect anti-pattern: `catch (Exception ex) { throw ex; }`

5. **Overly Broad Catch Detection**
   - Detect `catch (Exception)`
   - Detect `catch (SystemException)`
   - Allow specific exception types

### Phase 3: Configuration and Documentation (1 week)

#### 3.1 Configuration Options

Add to `.editorconfig`:

```ini
# ========================================
# ThrowsAnalyzer - Type Analysis (Advanced)
# ========================================

# Enable/disable rethrow anti-pattern analyzer (THROWS004)
throws_analyzer_enable_rethrow_antipattern = true

# Enable/disable catch clause ordering analyzer (THROWS007)
throws_analyzer_enable_catch_ordering = true

# Enable/disable empty catch block analyzer (THROWS008)
throws_analyzer_enable_empty_catch = true

# Enable/disable rethrow-only catch analyzer (THROWS009)
throws_analyzer_enable_rethrow_only_catch = true

# Enable/disable overly broad catch analyzer (THROWS010)
throws_analyzer_enable_overly_broad_catch = true

# Diagnostic severity configuration
dotnet_diagnostic.THROWS004.severity = warning
dotnet_diagnostic.THROWS007.severity = warning
dotnet_diagnostic.THROWS008.severity = warning
dotnet_diagnostic.THROWS009.severity = suggestion
dotnet_diagnostic.THROWS010.severity = suggestion
```

#### 3.2 Documentation Updates

**README.md additions:**

```markdown
### Advanced Type Analysis

ThrowsAnalyzer now includes semantic model-based exception type analysis:

- **THROWS004**: Detects rethrow anti-pattern (`throw ex;` instead of `throw;`)
- **THROWS007**: Detects unreachable catch clauses due to ordering
- **THROWS008**: Detects empty catch blocks (exception swallowing)
- **THROWS009**: Detects catch blocks that only rethrow
- **THROWS010**: Detects overly broad exception catches

These analyzers understand exception type hierarchies and provide more precise diagnostics.
```

**ANALYSIS.md updates:**

- Update "Current Implementation Analysis" section
- Mark type analysis features as implemented
- Update implementation coverage percentage

## Performance Considerations

### Semantic Model Access

**Issue:** Semantic model access is more expensive than syntax-only analysis.

**Mitigation:**
1. Cache semantic model per document
2. Only analyze when syntax indicates potential issues (pre-filter)
3. Use `context.SemanticModel` provided by Roslyn (already cached)
4. Avoid redundant type lookups

### Type Hierarchy Traversal

**Issue:** Walking inheritance chains can be expensive for deep hierarchies.

**Mitigation:**
1. Use `SymbolEqualityComparer` for fast comparisons
2. Cache common exception types (Exception, SystemException, etc.)
3. Limit hierarchy depth to reasonable bounds
4. Early exit when match is found

### Compilation Symbol Lookups

**Issue:** `GetTypeByMetadataName` can be slow.

**Mitigation:**
1. Cache well-known type symbols at analyzer initialization
2. Reuse compilation instance across analyses
3. Only lookup types when absolutely necessary

## Migration and Compatibility

### Backward Compatibility

- All existing analyzers (THROWS001, THROWS002, THROWS003) remain unchanged
- New analyzers are additive (THROWS004+)
- Configuration options are additive
- No breaking changes to public API

### Migration Path

1. **Phase 1 Release (v1.1)**: Foundation + Type Detection
   - Users get type detection infrastructure
   - No new diagnostics yet (non-breaking)

2. **Phase 2 Release (v1.2)**: New Analyzers
   - New diagnostic rules enabled by default
   - Users can disable via configuration
   - Documentation includes migration guide

3. **Phase 3 Release (v1.3)**: Refinements
   - Performance optimizations
   - Additional edge cases
   - Enhanced documentation

## Testing Strategy Summary

### Unit Tests (per component)
- ~20 tests for ExceptionTypeAnalyzer
- ~25 tests for TypedThrowDetector
- ~30 tests for CatchClauseAnalyzer
- ~20 tests per new analyzer (THROWS004-010)
- **Total: ~135 new unit tests**

### Integration Tests
- End-to-end scenarios with real code
- Performance benchmarks
- Cross-assembly exception type resolution

### Sample Projects
- Add examples to LibraryManagement sample
- Demonstrate each new diagnostic
- Show configuration options

## Success Metrics

### Functional Metrics
- [ ] All exception types correctly resolved in unit tests
- [ ] Catch clause ordering correctly detected
- [ ] Rethrow anti-patterns correctly identified
- [ ] All 135+ new tests passing
- [ ] Zero false positives in sample projects

### Performance Metrics
- [ ] Analysis time increase < 20% vs syntax-only
- [ ] Memory usage increase < 15%
- [ ] No analyzer timeouts on large codebases

### Quality Metrics
- [ ] Zero breaking changes to existing code
- [ ] All new diagnostics have code fixes (Phase 4)
- [ ] Documentation complete and accurate
- [ ] Sample code demonstrates all features

## Future Enhancements (Post-Implementation)

### Phase 4: Code Fixes (2-3 weeks)

Code fixes provide automated refactoring options that appear in the IDE when diagnostics are reported. This phase implements code fix providers for all diagnostic rules.

#### 4.1 Code Fix Infrastructure

**New Base Class: `ThrowsAnalyzerCodeFixProvider`**

```csharp
namespace ThrowsAnalyzer.CodeFixes
{
    /// <summary>
    /// Base class for all ThrowsAnalyzer code fix providers.
    /// Provides common utilities for code fix implementations.
    /// </summary>
    public abstract class ThrowsAnalyzerCodeFixProvider : CodeFixProvider
    {
        /// <summary>
        /// Gets the title for the code fix action.
        /// </summary>
        protected abstract string Title { get; }

        /// <summary>
        /// Creates a code action with consistent naming and behavior.
        /// </summary>
        protected CodeAction CreateCodeAction(
            string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey)
        {
            return CodeAction.Create(
                title: title,
                createChangedDocument: createChangedDocument,
                equivalenceKey: equivalenceKey);
        }
    }
}
```

#### 4.2 Code Fixes for Basic Analyzers (THROWS001-003)

**THROWS001: Method Contains Throw Statement**

Code fixes:
1. **Wrap in try-catch block** - Surrounds the throw statement(s) with a try-catch block
2. **Add XML documentation comment** - Adds `/// <exception cref="ExceptionType">` documentation

```csharp
// Before:
void Method()
{
    throw new ArgumentException("Invalid");
}

// After (Option 1 - Wrap in try-catch):
void Method()
{
    try
    {
        throw new ArgumentException("Invalid");
    }
    catch (ArgumentException ex)
    {
        // TODO: Handle exception
        throw;
    }
}

// After (Option 2 - Add documentation):
/// <exception cref="System.ArgumentException">Invalid</exception>
void Method()
{
    throw new ArgumentException("Invalid");
}
```

**Implementation:**

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MethodThrowsCodeFixProvider))]
public class MethodThrowsCodeFixProvider : ThrowsAnalyzerCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId001);

    protected override string Title => "Wrap throws in try-catch";

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

        // Register code fixes:
        // 1. Wrap in try-catch
        context.RegisterCodeFix(
            CreateCodeAction(
                "Wrap in try-catch block",
                c => WrapInTryCatchAsync(context.Document, diagnostic, c),
                nameof(WrapInTryCatchAsync)),
            diagnostic);

        // 2. Add XML documentation
        context.RegisterCodeFix(
            CreateCodeAction(
                "Add exception documentation",
                c => AddExceptionDocumentationAsync(context.Document, diagnostic, c),
                nameof(AddExceptionDocumentationAsync)),
            diagnostic);
    }

    private async Task<Document> WrapInTryCatchAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        // Implementation: Find throw statements, wrap method body in try-catch
    }

    private async Task<Document> AddExceptionDocumentationAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        // Implementation: Add /// <exception cref="..."> documentation
    }
}
```

**THROWS002: Unhandled Throw Statement**

Code fixes:
1. **Wrap in try-catch block** - Surrounds unhandled throws with try-catch
2. **Move to existing try block** - If there's a try-catch nearby, moves the code inside

```csharp
// Before:
void Method()
{
    ValidateInput();  // Contains throw
    ProcessData();
}

// After (Wrap in try-catch):
void Method()
{
    try
    {
        ValidateInput();
    }
    catch (ArgumentException ex)
    {
        // TODO: Handle exception
        throw;
    }
    ProcessData();
}
```

**THROWS003: Method Contains Try-Catch Block**

Code fixes:
1. **Remove try-catch and propagate** - Removes the try-catch and lets exceptions propagate
2. **Add logging to catch blocks** - Adds basic logging statement to empty catches

```csharp
// Before:
void Method()
{
    try
    {
        DoSomething();
    }
    catch (Exception)
    {
        // Empty catch
    }
}

// After (Option 1 - Remove and propagate):
void Method()
{
    DoSomething();
}

// After (Option 2 - Add logging):
void Method()
{
    try
    {
        DoSomething();
    }
    catch (Exception ex)
    {
        // Log exception
        Console.WriteLine($"Error: {ex.Message}");
        throw;
    }
}
```

#### 4.3 Code Fixes for Type-Aware Analyzers (THROWS004-010)

**THROWS004: Rethrow Anti-Pattern**

Code fix: **Replace `throw ex;` with `throw;`**

```csharp
// Before:
catch (Exception ex)
{
    throw ex;
}

// After:
catch (Exception ex)
{
    throw;
}
```

**Implementation:**

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RethrowAntiPatternCodeFixProvider))]
public class RethrowAntiPatternCodeFixProvider : ThrowsAnalyzerCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId004);

    protected override string Title => "Replace with bare rethrow";

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();

        context.RegisterCodeFix(
            CreateCodeAction(
                "Replace 'throw ex;' with 'throw;'",
                c => ReplaceWithBareRethrowAsync(context.Document, diagnostic, c),
                nameof(ReplaceWithBareRethrowAsync)),
            diagnostic);
    }

    private async Task<Document> ReplaceWithBareRethrowAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        var throwStatement = root.FindNode(diagnostic.Location.SourceSpan) as ThrowStatementSyntax;

        if (throwStatement?.Expression == null)
            return document;

        // Create new throw statement without expression
        var newThrow = SyntaxFactory.ThrowStatement()
            .WithThrowKeyword(throwStatement.ThrowKeyword)
            .WithSemicolonToken(throwStatement.SemicolonToken)
            .WithTriviaFrom(throwStatement);

        var newRoot = root.ReplaceNode(throwStatement, newThrow);
        return document.WithSyntaxRoot(newRoot);
    }
}
```

**THROWS007: Unreachable Catch Clause**

Code fix: **Reorder catch clauses** - Automatically reorders catches from most specific to most general

```csharp
// Before:
try { }
catch (Exception) { }
catch (ArgumentException) { }  // Unreachable

// After:
try { }
catch (ArgumentException) { }
catch (Exception) { }
```

**Implementation:**

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CatchClauseOrderingCodeFixProvider))]
public class CatchClauseOrderingCodeFixProvider : ThrowsAnalyzerCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId007);

    protected override string Title => "Reorder catch clauses";

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();

        context.RegisterCodeFix(
            CreateCodeAction(
                "Reorder catch clauses (specific to general)",
                c => ReorderCatchClausesAsync(context.Document, diagnostic, c),
                nameof(ReorderCatchClausesAsync)),
            diagnostic);
    }

    private async Task<Document> ReorderCatchClausesAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        // Find try statement
        // Sort catch clauses by exception hierarchy (most derived first)
        // Replace try statement with reordered version
    }
}
```

**THROWS008: Empty Catch Block**

Code fixes:
1. **Remove empty catch** - Removes the catch clause entirely
2. **Add logging** - Adds basic exception logging
3. **Add comment** - Adds TODO comment explaining why catch is empty

```csharp
// Before:
catch (Exception) { }

// After (Option 1 - Remove):
// Catch removed, exception propagates

// After (Option 2 - Add logging):
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

// After (Option 3 - Add comment):
catch (Exception)
{
    // TODO: Handle exception appropriately
}
```

**THROWS009: Catch Block Only Rethrows**

Code fix: **Remove unnecessary catch** - Removes the catch block since it only rethrows

```csharp
// Before:
try
{
    DoSomething();
}
catch (Exception)
{
    throw;
}

// After:
DoSomething();
```

**THROWS010: Overly Broad Exception Catch**

Code fixes:
1. **Replace with specific exception** - Suggests common specific exceptions based on context
2. **Add filter clause** - Adds `when (condition)` to make the catch more specific

```csharp
// Before:
catch (Exception ex) { }

// After (Option 1 - Specific exception):
catch (InvalidOperationException ex) { }

// After (Option 2 - Add filter):
catch (Exception ex) when (ex is InvalidOperationException) { }
```

#### 4.4 Code Fix Testing Strategy

**Test Categories:**

1. **Transformation Tests** - Verify code fixes produce correct output
   - Before/after comparisons
   - Preserve formatting and trivia
   - Handle edge cases (comments, nested structures)

2. **Multiple Fix Tests** - Test behavior when multiple fixes available
   - User can choose appropriate fix
   - Equivalence keys work correctly

3. **No-Op Tests** - Verify fixes don't apply when inappropriate
   - Invalid syntax
   - Missing context
   - Already correct code

**Test Example:**

```csharp
[TestMethod]
public async Task RethrowAntiPattern_CodeFix_ReplacesWithBareRethrow()
{
    var before = """
        using System;
        class Test
        {
            void Method()
            {
                try { }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
        """;

    var after = """
        using System;
        class Test
        {
            void Method()
            {
                try { }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }
        """;

    await VerifyCodeFixAsync(before, after);
}
```

#### 4.5 Code Fix Configuration

Add configuration options for code fix behavior:

```ini
[*.cs]

# Code fix preferences
throws_analyzer_codefix_prefer_logging = true
throws_analyzer_codefix_logging_method = Console.WriteLine
throws_analyzer_codefix_add_todo_comments = true
throws_analyzer_codefix_preserve_empty_catches = false
```

#### 4.6 Implementation Checklist

- [ ] Create base `ThrowsAnalyzerCodeFixProvider` class
- [ ] Implement `MethodThrowsCodeFixProvider` (THROWS001)
- [ ] Implement `UnhandledThrowsCodeFixProvider` (THROWS002)
- [ ] Implement `TryCatchCodeFixProvider` (THROWS003)
- [ ] Implement `RethrowAntiPatternCodeFixProvider` (THROWS004)
- [ ] Implement `CatchClauseOrderingCodeFixProvider` (THROWS007)
- [ ] Implement `EmptyCatchCodeFixProvider` (THROWS008)
- [ ] Implement `RethrowOnlyCatchCodeFixProvider` (THROWS009)
- [ ] Implement `OverlyBroadCatchCodeFixProvider` (THROWS010)
- [ ] Add unit tests for all code fix providers (~50 tests)
- [ ] Update documentation with code fix examples
- [ ] Test in Visual Studio and VS Code
- [ ] Performance testing (code fixes should be fast)

#### 4.7 Success Metrics

- [ ] All code fixes produce syntactically valid code
- [ ] Code fixes preserve formatting and comments
- [ ] All 50+ code fix tests passing
- [ ] Code fixes work in Visual Studio IDE
- [ ] Code fixes work in VS Code with C# extension
- [ ] Average fix application time < 100ms

### Phase 5: Advanced Analysis (4-6 weeks) ✅ COMPLETE

Phase 5 implements specialized exception analysis for advanced scenarios including cross-method flow, async/await patterns, iterators, and lambdas.

#### 5.1: Exception Flow Analysis (2 weeks) ✅ COMPLETE

**Deliverables:**
- Call graph construction for exception flow tracking
- Exception propagation tracker across method boundaries
- Three new diagnostics:
  - **THROWS017**: Unhandled method call exception
  - **THROWS018**: Deep exception propagation
  - **THROWS019**: Undocumented public exception

**Components Created:**
- `CallGraph` and `CallGraphBuilder` - Bidirectional call graph
- `ExceptionPropagationTracker` - Cross-method exception flow
- `UnhandledMethodCallAnalyzer` - THROWS017
- `DeepExceptionPropagationAnalyzer` - THROWS018
- `UndocumentedPublicExceptionAnalyzer` - THROWS019

#### 5.2: Async Exception Analysis (2 weeks) ✅ COMPLETE

**Deliverables:**
- Async method detection infrastructure
- Async exception pattern analyzer
- Three new diagnostics:
  - **THROWS020**: Async synchronous throw
  - **THROWS021**: Async void exception
  - **THROWS022**: Unobserved Task exception

**Components Created:**
- `AsyncMethodDetector` - Async pattern detection
- `AsyncExceptionAnalyzer` - Async exception analysis
- `AsyncSynchronousThrowAnalyzer` - THROWS020
- `AsyncVoidThrowAnalyzer` - THROWS021
- `UnobservedTaskExceptionAnalyzer` - THROWS022

#### 5.3: Iterator Exception Analysis (1-2 weeks) ✅ COMPLETE

**Deliverables:**
- Iterator method detection infrastructure
- Iterator exception pattern analyzer
- Two new diagnostics:
  - **THROWS023**: Deferred iterator exception
  - **THROWS024**: Iterator try-finally timing

**Components Created:**
- `IteratorMethodDetector` - Iterator pattern detection
- `IteratorExceptionAnalyzer` - Iterator exception analysis
- `IteratorDeferredExceptionAnalyzer` - THROWS023
- `IteratorTryFinallyAnalyzer` - THROWS024

#### 5.4: Best Practices & Design Patterns (1-2 weeks) ✅ COMPLETE

**Deliverables:**
- Best practices analyzers for exception anti-patterns
- Design pattern suggestion analyzers
- Four new diagnostics:
  - **THROWS027**: Exception used for control flow
  - **THROWS028**: Custom exception naming convention
  - **THROWS029**: Exception in hot path
  - **THROWS030**: Result pattern suggestion

**Components Created:**
- `ExceptionControlFlowAnalyzer` - THROWS027
- `CustomExceptionNamingAnalyzer` - THROWS028
- `ExceptionInHotPathAnalyzer` - THROWS029
- `ResultPatternSuggestionAnalyzer` - THROWS030

#### 5.5: Lambda Exception Analysis (1-2 weeks) ✅ COMPLETE

**Deliverables:**
- Lambda expression detection infrastructure
- Lambda exception pattern analyzer
- Two new diagnostics:
  - **THROWS025**: Lambda uncaught exception
  - **THROWS026**: Event handler lambda exception

**Components Created:**
- `LambdaExceptionDetector` - Lambda pattern detection
- `LambdaExceptionAnalyzer` - Lambda exception analysis
- `LambdaUncaughtExceptionAnalyzer` - THROWS025
- `EventHandlerLambdaExceptionAnalyzer` - THROWS026

### Phase 6: Code Fixes for Advanced Analysis (4-6 weeks)

Phase 6 implements code fix providers for all advanced analysis diagnostics from Phase 5.

#### 6.1: Exception Flow Code Fixes (1-2 weeks)

**THROWS017: Unhandled Method Call Exception**

Code fixes:
1. **Wrap call in try-catch** - Surrounds the method call with appropriate exception handling
2. **Add exception to method signature** - Propagates by documenting the exception
3. **Propagate to caller** - Adds documentation that this method may throw

```csharp
// Before:
void Method()
{
    CallThatThrows(); // THROWS017: May throw InvalidOperationException
}

// After (Option 1 - Wrap in try-catch):
void Method()
{
    try
    {
        CallThatThrows();
    }
    catch (InvalidOperationException ex)
    {
        // TODO: Handle exception
        throw;
    }
}

// After (Option 2 - Document propagation):
/// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
void Method()
{
    CallThatThrows();
}
```

**THROWS018: Deep Exception Propagation**

Code fixes:
1. **Add exception handling at intermediate level** - Suggests adding try-catch in middle of call chain
2. **Document propagation chain** - Adds XML comments to all methods in chain

```csharp
// Before (3-level deep propagation):
void Level1() => Level2();
void Level2() => Level3();
void Level3() => throw new Exception();

// After (Add handling at Level2):
void Level1() => Level2();
void Level2()
{
    try
    {
        Level3();
    }
    catch (Exception ex)
    {
        // Handle at intermediate level
        LogError(ex);
        throw;
    }
}
void Level3() => throw new Exception();
```

**THROWS019: Undocumented Public Exception**

Code fix: **Add exception documentation** - Generates complete XML documentation

```csharp
// Before:
public void ProcessData(string data)
{
    if (string.IsNullOrEmpty(data))
        throw new ArgumentException("Data cannot be empty");

    if (!IsValid(data))
        throw new InvalidOperationException("Invalid data format");
}

// After:
/// <summary>
/// Processes the specified data.
/// </summary>
/// <param name="data">The data to process.</param>
/// <exception cref="System.ArgumentException">Thrown when data is null or empty</exception>
/// <exception cref="System.InvalidOperationException">Thrown when data format is invalid</exception>
public void ProcessData(string data)
{
    if (string.IsNullOrEmpty(data))
        throw new ArgumentException("Data cannot be empty");

    if (!IsValid(data))
        throw new InvalidOperationException("Invalid data format");
}
```

#### 6.2: Async Exception Code Fixes (1-2 weeks)

**THROWS020: Async Synchronous Throw**

Code fixes:
1. **Move validation before async** - Extracts synchronous validation to wrapper method
2. **Add Task.Yield before throw** - Forces async execution before throw

```csharp
// Before:
async Task ProcessAsync(string data)
{
    if (data == null)
        throw new ArgumentNullException(); // THROWS020

    await DoWorkAsync(data);
}

// After (Option 1 - Wrapper pattern):
Task ProcessAsync(string data)
{
    if (data == null)
        throw new ArgumentNullException(); // ✅ Synchronous validation

    return ProcessInternalAsync(data);
}

async Task ProcessInternalAsync(string data)
{
    await DoWorkAsync(data);
}

// After (Option 2 - Force async):
async Task ProcessAsync(string data)
{
    await Task.Yield(); // Force async

    if (data == null)
        throw new ArgumentNullException(); // ✅ Now async

    await DoWorkAsync(data);
}
```

**THROWS021: Async Void Exception**

Code fixes:
1. **Change to async Task** - Converts async void to async Task
2. **Wrap all code in try-catch** - Handles all exceptions within async void

```csharp
// Before:
async void Button_Click(object sender, EventArgs e)
{
    await ProcessAsync();
    throw new InvalidOperationException(); // THROWS021
}

// After (Option 1 - Change to async Task):
async Task Button_Click(object sender, EventArgs e)
{
    await ProcessAsync();
    throw new InvalidOperationException();
}

// After (Option 2 - Wrap in try-catch):
async void Button_Click(object sender, EventArgs e)
{
    try
    {
        await ProcessAsync();
        throw new InvalidOperationException();
    }
    catch (Exception ex)
    {
        // Log error
        LogError(ex);
        MessageBox.Show("An error occurred");
    }
}
```

**THROWS022: Unobserved Task Exception**

Code fixes:
1. **Add await** - Converts fire-and-forget to awaited call
2. **Assign to variable** - Captures Task for later observation
3. **Add continuation for error handling** - Uses ContinueWith for exception handling

```csharp
// Before:
void Method()
{
    TaskReturningMethod(); // THROWS022
}

// After (Option 1 - Add await):
async Task Method()
{
    await TaskReturningMethod();
}

// After (Option 2 - Assign):
void Method()
{
    var task = TaskReturningMethod();
    // Task can be awaited or observed later
}

// After (Option 3 - Add continuation):
void Method()
{
    TaskReturningMethod().ContinueWith(t =>
    {
        if (t.IsFaulted)
            LogError(t.Exception);
    });
}
```

#### 6.3: Iterator Exception Code Fixes (1 week)

**THROWS023: Deferred Iterator Exception**

Code fixes:
1. **Move validation before yield** - Extracts validation to before first yield
2. **Use wrapper method pattern** - Separates validation from iteration

```csharp
// Before:
IEnumerable<int> GetValues(int[] items)
{
    yield return 1;

    if (items == null)
        throw new ArgumentNullException(); // THROWS023

    foreach (var item in items)
        yield return item;
}

// After (Option 1 - Move before yield):
IEnumerable<int> GetValues(int[] items)
{
    if (items == null)
        throw new ArgumentNullException(); // ✅ Before first yield

    yield return 1;

    foreach (var item in items)
        yield return item;
}

// After (Option 2 - Wrapper pattern):
IEnumerable<int> GetValues(int[] items)
{
    if (items == null)
        throw new ArgumentNullException(); // ✅ Immediate validation

    return GetValuesIterator(items);
}

IEnumerable<int> GetValuesIterator(int[] items)
{
    yield return 1;

    foreach (var item in items)
        yield return item;
}
```

**THROWS024: Iterator Try-Finally Timing**

Code fix: **Add disposal comment/documentation** - Documents the disposal behavior

```csharp
// Before:
IEnumerable<int> ReadData()
{
    var reader = OpenReader();
    try
    {
        yield return ReadNext();
    }
    finally
    {
        reader.Dispose(); // THROWS024
    }
}

// After:
/// <summary>
/// Reads data from the source.
/// </summary>
/// <remarks>
/// The reader is disposed when enumeration completes or the enumerator is disposed.
/// Always use with 'using' or 'foreach' to ensure proper disposal.
/// </remarks>
IEnumerable<int> ReadData()
{
    var reader = OpenReader();
    try
    {
        yield return ReadNext();
    }
    finally
    {
        reader.Dispose(); // Executes on disposal
    }
}
```

#### 6.4: Best Practices Code Fixes (1 week)

**THROWS027: Exception Used for Control Flow**

Code fixes:
1. **Convert to return value** - Replaces exception-based control flow with return value
2. **Convert to Result&lt;T&gt;** - Implements Result pattern for error handling

```csharp
// Before:
void ProcessData()
{
    try
    {
        if (!IsValid())
            throw new InvalidOperationException("Not valid");
        // Process...
    }
    catch (InvalidOperationException)
    {
        // Handle invalid state
        return;
    }
}

// After (Option 1 - Return value):
void ProcessData()
{
    if (!IsValid())
        return; // Early return instead of exception
    // Process...
}

// After (Option 2 - Result pattern):
Result ProcessData()
{
    if (!IsValid())
        return Result.Failure("Not valid");
    // Process...
    return Result.Success();
}
```

**THROWS028: Custom Exception Naming Convention**

Code fix: **Rename exception type** - Adds "Exception" suffix to type name

```csharp
// Before:
class InvalidState : Exception { }

// After:
class InvalidStateException : Exception { }
```

**THROWS029: Exception in Hot Path**

Code fixes:
1. **Move validation outside loop** - Extracts validation to before loop
2. **Use Try pattern** - Converts to TryParse/TryGet pattern
3. **Use return value** - Returns error codes instead of throwing

```csharp
// Before:
void ProcessItems(List<int> items)
{
    foreach (var item in items)
    {
        if (item < 0)
            throw new ArgumentException(); // THROWS029
        Process(item);
    }
}

// After (Option 1 - Validate before loop):
void ProcessItems(List<int> items)
{
    if (items.Any(i => i < 0))
        throw new ArgumentException("Invalid items");

    foreach (var item in items)
        Process(item);
}

// After (Option 2 - Try pattern):
bool TryProcessItems(List<int> items, out string error)
{
    error = null;
    foreach (var item in items)
    {
        if (item < 0)
        {
            error = "Invalid item";
            return false;
        }
        Process(item);
    }
    return true;
}
```

**THROWS030: Result Pattern Suggestion**

Code fix: **Implement Result&lt;T&gt; pattern** - Converts exception-based API to Result pattern

```csharp
// Before:
void ValidateInput(string input)
{
    if (string.IsNullOrEmpty(input))
        throw new ArgumentException("Input cannot be empty");
}

// After:
Result ValidateInput(string input)
{
    if (string.IsNullOrEmpty(input))
        return Result.Failure("Input cannot be empty");

    return Result.Success();
}

// Or with generic Result<T>:
Result<ValidatedInput> ValidateInput(string input)
{
    if (string.IsNullOrEmpty(input))
        return Result.Failure<ValidatedInput>("Input cannot be empty");

    return Result.Success(new ValidatedInput(input));
}
```

#### 6.5: Lambda Exception Code Fixes (1 week)

**THROWS025: Lambda Uncaught Exception**

Code fixes:
1. **Wrap lambda body in try-catch** - Adds exception handling within lambda
2. **Use defensive coding** - Rewrites to avoid exceptions where possible

```csharp
// Before:
var result = items.Where(x =>
{
    if (x < 0)
        throw new InvalidOperationException(); // THROWS025
    return x > 1;
});

// After (Option 1 - Wrap in try-catch):
var result = items.Where(x =>
{
    try
    {
        if (x < 0)
            throw new InvalidOperationException();
        return x > 1;
    }
    catch (InvalidOperationException)
    {
        return false; // Handle gracefully
    }
});

// After (Option 2 - Defensive coding):
var result = items.Where(x => x >= 0 && x > 1);
```

**THROWS026: Event Handler Lambda Exception**

Code fix: **Wrap in try-catch with logging** - Adds comprehensive exception handling

```csharp
// Before:
MyEvent += (sender, e) =>
{
    ProcessEvent(e);
    throw new InvalidOperationException(); // THROWS026
};

// After:
MyEvent += (sender, e) =>
{
    try
    {
        ProcessEvent(e);
        throw new InvalidOperationException();
    }
    catch (Exception ex)
    {
        // Log error
        LogError(ex);
        // Optionally show user message
        MessageBox.Show($"Error: {ex.Message}");
    }
};
```

#### 6.5: Code Fix Testing Strategy

**Test Categories:**

1. **Advanced Flow Tests** - Test exception flow fixes
   - Multi-level call chains
   - Documentation generation
   - Wrapper method extraction

2. **Async Pattern Tests** - Test async-specific fixes
   - Async void to async Task conversion
   - Wrapper method extraction for validation
   - Task observation patterns

3. **Iterator Pattern Tests** - Test iterator-specific fixes
   - Validation relocation
   - Wrapper method generation
   - Disposal documentation

4. **Lambda Pattern Tests** - Test lambda-specific fixes
   - Try-catch injection
   - Event handler exception handling
   - Defensive coding transformations

**Test Example:**

```csharp
[TestMethod]
public async Task AsyncVoidException_CodeFix_ConvertsToAsyncTask()
{
    var before = """
        using System;
        using System.Threading.Tasks;

        class Test
        {
            async void Method()
            {
                await Task.Delay(1);
                throw new InvalidOperationException();
            }
        }
        """;

    var after = """
        using System;
        using System.Threading.Tasks;

        class Test
        {
            async Task Method()
            {
                await Task.Delay(1);
                throw new InvalidOperationException();
            }
        }
        """;

    await VerifyCodeFixAsync<AsyncVoidExceptionCodeFixProvider>(before, after);
}
```

#### 6.6: Implementation Checklist

**Exception Flow Code Fixes:**
- [ ] Create `UnhandledMethodCallCodeFixProvider` (THROWS017)
- [ ] Create `DeepExceptionPropagationCodeFixProvider` (THROWS018)
- [ ] Create `UndocumentedPublicExceptionCodeFixProvider` (THROWS019)

**Async Exception Code Fixes:**
- [ ] Create `AsyncSynchronousThrowCodeFixProvider` (THROWS020)
- [ ] Create `AsyncVoidExceptionCodeFixProvider` (THROWS021)
- [ ] Create `UnobservedTaskExceptionCodeFixProvider` (THROWS022)

**Iterator Exception Code Fixes:**
- [ ] Create `DeferredIteratorExceptionCodeFixProvider` (THROWS023)
- [ ] Create `IteratorTryFinallyCodeFixProvider` (THROWS024)

**Best Practices Code Fixes:**
- [ ] Create `ExceptionControlFlowCodeFixProvider` (THROWS027)
- [ ] Create `CustomExceptionNamingCodeFixProvider` (THROWS028)
- [ ] Create `ExceptionInHotPathCodeFixProvider` (THROWS029)
- [ ] Create `ResultPatternCodeFixProvider` (THROWS030)

**Lambda Exception Code Fixes:**
- [ ] Create `LambdaUncaughtExceptionCodeFixProvider` (THROWS025)
- [ ] Create `EventHandlerLambdaExceptionCodeFixProvider` (THROWS026)

**Testing and Integration:**
- [ ] Add unit tests for all code fix providers (~80 tests)
- [ ] Update documentation with code fix examples
- [ ] Test in Visual Studio and VS Code
- [ ] Performance testing (code fixes should be fast)

#### 6.7: Success Metrics

- [ ] All code fixes produce syntactically valid code
- [ ] Code fixes preserve formatting and comments
- [ ] All 80+ code fix tests passing
- [ ] Code fixes work in Visual Studio IDE
- [ ] Code fixes work in VS Code with C# extension
- [ ] Average fix application time < 100ms
- [ ] Complex fixes (wrapper methods, refactorings) correctly maintain semantics

### Phase 7: IDE Integration & Polish (2-3 weeks)

**Deliverables:**
- Quick info tooltips showing exception types and propagation
- IntelliSense improvements for exception types in catch clauses
- Exception hierarchy visualization in IDE
- Code lens integration showing exception flow
- Performance optimizations for large codebases
- Final documentation and samples

**Components:**
- Enhanced IDE tooltips with exception information
- IntelliSense provider for exception types
- Code lens provider for exception flow visualization
- Performance profiling and optimization
- Comprehensive user documentation
- Sample projects demonstrating all features

## Risk Assessment

### High Risk
- **Semantic model performance**: Mitigation through caching and pre-filtering
- **Type resolution edge cases**: Extensive testing with diverse code patterns

### Medium Risk
- **Configuration complexity**: Clear documentation and examples
- **False positives**: Conservative diagnostic rules, easy to disable

### Low Risk
- **Breaking changes**: Strict versioning and backward compatibility
- **Maintenance burden**: Well-architected, testable code

## Timeline Estimate

- **Phase 1**: 2-3 weeks (Foundation - Type Detection) ✅ COMPLETE
- **Phase 2**: 2-3 weeks (Analyzers - THROWS004-010) ✅ COMPLETE
- **Phase 3**: 1 week (Configuration & Documentation) ✅ COMPLETE
- **Testing**: 1-2 weeks (Comprehensive Unit Tests) ✅ COMPLETE
- **Phase 4**: 2-3 weeks (Code Fixes for THROWS001-010)
- **Phase 5**: 5-8 weeks (Advanced Analysis - THROWS017-030) ✅ COMPLETE
  - **5.1**: 2 weeks (Exception Flow Analysis) ✅ COMPLETE
  - **5.2**: 2 weeks (Async Exception Analysis) ✅ COMPLETE
  - **5.3**: 1-2 weeks (Iterator Exception Analysis) ✅ COMPLETE
  - **5.4**: 1-2 weeks (Best Practices & Design Patterns) ✅ COMPLETE
  - **5.5**: 1-2 weeks (Lambda Exception Analysis) ✅ COMPLETE
- **Phase 6**: 5-7 weeks (Code Fixes for Advanced Analysis - THROWS017-030)
- **Phase 7**: 2-3 weeks (IDE Integration & Polish)
- **Total Phases 1-3**: 6-9 weeks ✅ COMPLETE
- **Total with Phase 4**: 8-12 weeks
- **Total with Phase 5**: 13-20 weeks ✅ COMPLETE
- **Total with Phase 6**: 18-27 weeks
- **Total with Phase 7**: 20-30 weeks

## Conclusion

This implementation plan provides a comprehensive roadmap for adding semantic model-based exception type analysis to ThrowsAnalyzer. The phased approach ensures:

1. **Solid foundation** with reusable type analysis infrastructure
2. **Incremental delivery** of user-facing features
3. **Backward compatibility** with existing code
4. **Extensibility** for future enhancements
5. **Quality assurance** through extensive testing

The result will be a significantly more powerful analyzer that can detect sophisticated exception handling issues while maintaining excellent performance and usability.
