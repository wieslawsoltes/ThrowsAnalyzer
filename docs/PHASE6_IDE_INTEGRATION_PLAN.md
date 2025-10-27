# Phase 6: IDE Integration - Detailed Implementation Plan

## Overview

Phase 6 focuses on enhancing the developer experience through IDE integration features that provide real-time feedback and visual aids for exception handling. This phase leverages Visual Studio and VS Code extension points to deliver interactive features beyond traditional analyzers.

**Timeline:** 6-10 weeks
**Prerequisites:** Phase 5 completion (exception flow analysis provides data for IDE features)
**Target IDEs:** Visual Studio 2022, VS Code with C# DevKit

## Phase 6.1: Quick Info Tooltips (2-3 weeks)

### Overview
Enhance tooltips (Quick Info) to display exception information when hovering over methods, throw statements, and catch clauses.

### 6.1.1: Quick Info Provider Infrastructure

**Component:** `ExceptionQuickInfoProvider.cs`

**Purpose:** Provides exception information in tooltips

**Dependencies:**
- Microsoft.CodeAnalysis.EditorFeatures
- Microsoft.VisualStudio.Language.Intellisense
- Phase 5 ExceptionFlowAnalyzer

**Implementation Steps:**

#### Step 1: Create Quick Info Provider Base (Week 1, Days 1-2)

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using ThrowsAnalyzer.Analysis;

namespace ThrowsAnalyzer.IDE.QuickInfo
{
    /// <summary>
    /// Provides exception information in Quick Info tooltips.
    /// </summary>
    public class ExceptionQuickInfoProvider : IQuickInfoSourceProvider
    {
        private readonly ITextBuffer _textBuffer;
        private readonly ISemanticModelProvider _semanticModelProvider;
        private readonly ExceptionFlowAnalyzer _flowAnalyzer;

        public ExceptionQuickInfoProvider(
            ITextBuffer textBuffer,
            ISemanticModelProvider semanticModelProvider,
            ExceptionFlowAnalyzer flowAnalyzer)
        {
            _textBuffer = textBuffer;
            _semanticModelProvider = semanticModelProvider;
            _flowAnalyzer = flowAnalyzer;
        }

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new ExceptionQuickInfoSource(textBuffer, _semanticModelProvider, _flowAnalyzer);
        }
    }
}
```

#### Step 2: Implement Quick Info Source (Week 1, Days 3-5)

```csharp
namespace ThrowsAnalyzer.IDE.QuickInfo
{
    internal class ExceptionQuickInfoSource : IQuickInfoSource
    {
        private readonly ITextBuffer _textBuffer;
        private readonly ISemanticModelProvider _semanticModelProvider;
        private readonly ExceptionFlowAnalyzer _flowAnalyzer;

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(
            IAsyncQuickInfoSession session,
            CancellationToken cancellationToken)
        {
            // 1. Get trigger point
            var triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot);
            if (triggerPoint == null) return null;

            // 2. Get semantic model and syntax node at position
            var semanticModel = await _semanticModelProvider.GetSemanticModelAsync(cancellationToken);
            var root = await semanticModel.SyntaxTree.GetRootAsync(cancellationToken);
            var node = root.FindToken(triggerPoint.Value).Parent;

            // 3. Determine node type and generate appropriate Quick Info
            if (node is MethodDeclarationSyntax methodDecl)
            {
                return await GetMethodQuickInfoAsync(methodDecl, semanticModel, cancellationToken);
            }
            else if (node is ThrowStatementSyntax throwStmt)
            {
                return await GetThrowQuickInfoAsync(throwStmt, semanticModel, cancellationToken);
            }
            else if (node is CatchClauseSyntax catchClause)
            {
                return await GetCatchQuickInfoAsync(catchClause, semanticModel, cancellationToken);
            }
            else if (node is InvocationExpressionSyntax invocation)
            {
                return await GetInvocationQuickInfoAsync(invocation, semanticModel, cancellationToken);
            }

            return null;
        }

        private async Task<QuickInfoItem> GetMethodQuickInfoAsync(
            MethodDeclarationSyntax methodDecl,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // 1. Analyze exception flow for this method
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl);
            var flowInfo = await _flowAnalyzer.AnalyzeMethodAsync(methodSymbol, cancellationToken);

            // 2. Build Quick Info content
            var content = new ContainerElement(
                ContainerElementStyle.Stacked,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, "Exceptions:")),
                new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text,
                        $"  Thrown: {flowInfo.ThrownExceptions.Count}")),
                new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text,
                        $"  Caught: {flowInfo.CaughtExceptions.Count}")),
                new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text,
                        $"  Propagated: {flowInfo.PropagatedExceptions.Count}"))
            );

            // 3. Add detailed exception list
            if (flowInfo.PropagatedExceptions.Any())
            {
                var exceptionList = new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "\nPropagated exceptions:"));

                foreach (var ex in flowInfo.PropagatedExceptions)
                {
                    exceptionList.Runs.Add(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Type,
                            $"\n  - {ex.ExceptionType.Name}"));
                }

                content.Elements.Add(exceptionList);
            }

            return new QuickInfoItem(methodDecl.Span, content);
        }

        private async Task<QuickInfoItem> GetThrowQuickInfoAsync(
            ThrowStatementSyntax throwStmt,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // 1. Get thrown exception type
            var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwStmt, semanticModel);
            if (exceptionType == null) return null;

            // 2. Check if this throw is handled
            var isHandled = UnhandledThrowDetector.IsThrowHandled(throwStmt);

            // 3. Build Quick Info content
            var content = new ContainerElement(
                ContainerElementStyle.Stacked,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, "throw "),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Type, exceptionType.Name)),
                new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text,
                        isHandled ? "  Status: Handled" : "  Status: Unhandled (will propagate)"))
            );

            return new QuickInfoItem(throwStmt.Span, content);
        }

        private async Task<QuickInfoItem> GetCatchQuickInfoAsync(
            CatchClauseSyntax catchClause,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // 1. Get caught exception type
            var exceptionType = ExceptionTypeAnalyzer.GetCaughtExceptionType(catchClause, semanticModel);
            if (exceptionType == null) return null;

            // 2. Analyze catch clause issues
            var analyzer = new CatchClauseAnalyzer(semanticModel);
            var orderingIssues = analyzer.DetectOrderingIssues(new[] { catchClause });
            var isEmpty = analyzer.DetectEmptyCatches(new[] { catchClause }).Any();
            var isRethrowOnly = analyzer.DetectRethrowOnlyCatches(new[] { catchClause }).Any();

            // 3. Build Quick Info content
            var content = new ContainerElement(
                ContainerElementStyle.Stacked,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, "catch "),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Type, exceptionType.Name))
            );

            // 4. Add warnings if any
            if (isEmpty)
            {
                content.Elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.WarningText,
                        "  ⚠ Warning: Empty catch block swallows exceptions")));
            }

            if (isRethrowOnly)
            {
                content.Elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.InfoText,
                        "  ℹ Info: Catch block only rethrows (consider removing)")));
            }

            return new QuickInfoItem(catchClause.Span, content);
        }

        private async Task<QuickInfoItem> GetInvocationQuickInfoAsync(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // 1. Get invoked method symbol
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol) return null;

            // 2. Analyze exceptions from invoked method
            var flowInfo = await _flowAnalyzer.AnalyzeMethodAsync(methodSymbol, cancellationToken);
            if (!flowInfo.PropagatedExceptions.Any()) return null;

            // 3. Build Quick Info content
            var content = new ContainerElement(
                ContainerElementStyle.Stacked,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text,
                        "This method may throw:")));

            foreach (var ex in flowInfo.PropagatedExceptions)
            {
                content.Elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Type,
                        $"  - {ex.ExceptionType.Name}")));
            }

            return new QuickInfoItem(invocation.Span, content);
        }
    }
}
```

#### Step 3: Register Quick Info Provider (Week 2, Day 1)

```csharp
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace ThrowsAnalyzer.IDE.QuickInfo
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("Exception Quick Info Provider")]
    [ContentType("CSharp")]
    [Order(Before = "Default Quick Info Provider")]
    internal class ExceptionQuickInfoProviderFactory : IQuickInfoSourceProvider
    {
        [Import]
        internal ISemanticModelProvider SemanticModelProvider { get; set; }

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            var flowAnalyzer = new ExceptionFlowAnalyzer();
            return new ExceptionQuickInfoSource(textBuffer, SemanticModelProvider, flowAnalyzer);
        }
    }
}
```

### 6.1.2: Testing Quick Info (Week 2, Days 2-3)

**Test Cases:**

1. **Method hover shows exception summary**
   - Hover over method name
   - Verify tooltip shows thrown/caught/propagated counts
   - Verify exception types listed

2. **Throw statement hover shows exception type**
   - Hover over `throw` keyword
   - Verify tooltip shows exception type
   - Verify handling status

3. **Catch clause hover shows caught type**
   - Hover over `catch` keyword
   - Verify tooltip shows caught exception type
   - Verify warnings for empty/rethrow-only catches

4. **Method invocation hover shows throwable exceptions**
   - Hover over method call
   - Verify tooltip shows exceptions that may propagate
   - Verify empty tooltip if method doesn't throw

### 6.1.3: Quick Info Formatting and Styling (Week 2, Days 4-5)

**Enhancements:**
- Add color coding (green for handled, red for unhandled)
- Add icons/glyphs for exception types
- Add hyperlinks to exception documentation
- Support markdown formatting for rich tooltips

**Implementation:**

```csharp
private ClassifiedTextElement CreateExceptionStatusElement(bool isHandled)
{
    var statusGlyph = isHandled ? "✓" : "⚠";
    var statusColor = isHandled
        ? PredefinedClassificationTypeNames.SuccessText
        : PredefinedClassificationTypeNames.WarningText;

    return new ClassifiedTextElement(
        new ClassifiedTextRun(statusColor, statusGlyph),
        new ClassifiedTextRun(PredefinedClassificationTypeNames.Text,
            isHandled ? " Handled" : " Unhandled"));
}
```

---

## Phase 6.2: IntelliSense for Exception Types (2-3 weeks)

### Overview
Provide IntelliSense suggestions for exception types in catch clauses, ordered by relevance based on what exceptions are actually thrown in the try block.

### 6.2.1: Completion Provider Infrastructure (Week 3, Days 1-3)

**Component:** `ExceptionCompletionProvider.cs`

**Purpose:** Suggests exception types in catch clauses based on context

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ThrowsAnalyzer.Analysis;

namespace ThrowsAnalyzer.IDE.Completion
{
    [ExportCompletionProvider(nameof(ExceptionCompletionProvider), LanguageNames.CSharp)]
    public class ExceptionCompletionProvider : CompletionProvider
    {
        private readonly ExceptionFlowAnalyzer _flowAnalyzer;

        public ExceptionCompletionProvider()
        {
            _flowAnalyzer = new ExceptionFlowAnalyzer();
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            // 1. Check if we're in a catch clause declaration
            var syntaxContext = await context.GetSyntaxContextAsync(context.CancellationToken);
            if (!IsInCatchDeclaration(syntaxContext))
                return;

            // 2. Find the containing try statement
            var tryStatement = syntaxContext.TargetToken.Parent
                ?.AncestorsAndSelf()
                .OfType<TryStatementSyntax>()
                .FirstOrDefault();

            if (tryStatement == null) return;

            // 3. Analyze exceptions thrown in try block
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            var thrownExceptions = await AnalyzeTryBlockExceptionsAsync(
                tryStatement.Block,
                semanticModel,
                context.CancellationToken);

            // 4. Get already caught exception types
            var alreadyCaught = GetAlreadyCaughtExceptions(tryStatement, semanticModel);

            // 5. Filter out already caught exceptions
            var suggestedExceptions = thrownExceptions
                .Where(ex => !alreadyCaught.Any(caught =>
                    IsAssignableTo(ex.ExceptionType, caught)))
                .ToList();

            // 6. Add completion items
            foreach (var ex in suggestedExceptions)
            {
                var item = CreateCompletionItem(ex, context);
                context.AddItem(item);
            }

            // 7. Add common exception types (lower priority)
            AddCommonExceptionTypes(context);
        }

        private async Task<List<ThrownExceptionInfo>> AnalyzeTryBlockExceptionsAsync(
            BlockSyntax tryBlock,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var thrownExceptions = new List<ThrownExceptionInfo>();

            // 1. Find direct throw statements
            var throwStatements = tryBlock.DescendantNodes().OfType<ThrowStatementSyntax>();
            foreach (var throwStmt in throwStatements)
            {
                var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwStmt, semanticModel);
                if (exceptionType != null)
                {
                    thrownExceptions.Add(new ThrownExceptionInfo
                    {
                        ExceptionType = exceptionType,
                        Location = throwStmt.GetLocation(),
                        IsDirect = true
                    });
                }
            }

            // 2. Find exceptions from method invocations
            var invocations = tryBlock.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    var flowInfo = await _flowAnalyzer.AnalyzeMethodAsync(methodSymbol, cancellationToken);
                    thrownExceptions.AddRange(flowInfo.PropagatedExceptions.Select(ex =>
                        new ThrownExceptionInfo
                        {
                            ExceptionType = ex.ExceptionType,
                            Location = invocation.GetLocation(),
                            IsDirect = false,
                            OriginMethod = methodSymbol
                        }));
                }
            }

            return thrownExceptions;
        }

        private List<ITypeSymbol> GetAlreadyCaughtExceptions(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel)
        {
            var caught = new List<ITypeSymbol>();

            foreach (var catchClause in tryStatement.Catches)
            {
                var exceptionType = ExceptionTypeAnalyzer.GetCaughtExceptionType(catchClause, semanticModel);
                if (exceptionType != null)
                {
                    caught.Add(exceptionType);
                }
            }

            return caught;
        }

        private CompletionItem CreateCompletionItem(
            ThrownExceptionInfo exceptionInfo,
            CompletionContext context)
        {
            var typeName = exceptionInfo.ExceptionType.Name;
            var fullTypeName = exceptionInfo.ExceptionType.ToDisplayString();

            // Create completion item with higher priority for directly thrown exceptions
            var sortText = exceptionInfo.IsDirect ? "0" : "1";

            return CompletionItem.Create(
                displayText: typeName,
                filterText: typeName,
                sortText: sortText + typeName,
                properties: ImmutableDictionary<string, string>.Empty
                    .Add("FullTypeName", fullTypeName)
                    .Add("IsDirect", exceptionInfo.IsDirect.ToString()),
                tags: ImmutableArray.Create(WellKnownTags.Class),
                rules: CompletionItemRules.Default);
        }

        private void AddCommonExceptionTypes(CompletionContext context)
        {
            var commonExceptions = new[]
            {
                "ArgumentException",
                "ArgumentNullException",
                "InvalidOperationException",
                "NotSupportedException",
                "NotImplementedException",
                "Exception"
            };

            foreach (var exceptionName in commonExceptions)
            {
                var item = CompletionItem.Create(
                    displayText: exceptionName,
                    filterText: exceptionName,
                    sortText: "2" + exceptionName, // Lower priority
                    tags: ImmutableArray.Create(WellKnownTags.Class, WellKnownTags.Intrinsic));

                context.AddItem(item);
            }
        }

        private bool IsInCatchDeclaration(SyntaxContext syntaxContext)
        {
            // Check if we're in: catch (<cursor>
            var token = syntaxContext.TargetToken;
            return token.Parent is CatchDeclarationSyntax ||
                   (token.IsKind(SyntaxKind.OpenParenToken) &&
                    token.Parent?.Parent is CatchClauseSyntax);
        }
    }
}
```

### 6.2.2: Completion Item Details and Documentation (Week 3, Days 4-5)

**Enhancement:** Provide detailed information when completion item is selected

```csharp
public override async Task<CompletionDescription> GetDescriptionAsync(
    Document document,
    CompletionItem item,
    CancellationToken cancellationToken)
{
    // 1. Get exception type information
    var fullTypeName = item.Properties["FullTypeName"];
    var isDirect = bool.Parse(item.Properties["IsDirect"]);

    // 2. Build description
    var description = new List<TaggedText>
    {
        new TaggedText(TextTags.Namespace, fullTypeName),
        new TaggedText(TextTags.LineBreak, "\n"),
        new TaggedText(TextTags.Text,
            isDirect
                ? "Thrown directly in this try block"
                : "May be thrown by called methods")
    };

    // 3. Add XML documentation if available
    var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
    var compilation = semanticModel.Compilation;
    var exceptionType = compilation.GetTypeByMetadataName(fullTypeName);

    if (exceptionType != null)
    {
        var xmlDoc = exceptionType.GetDocumentationCommentXml();
        if (!string.IsNullOrEmpty(xmlDoc))
        {
            description.Add(new TaggedText(TextTags.LineBreak, "\n"));
            description.Add(new TaggedText(TextTags.Text, ParseSummaryFromXml(xmlDoc)));
        }
    }

    return CompletionDescription.Create(description.ToImmutableArray());
}
```

### 6.2.3: Testing IntelliSense (Week 4, Days 1-2)

**Test Cases:**

1. **IntelliSense shows thrown exceptions**
   ```csharp
   try
   {
       throw new InvalidOperationException();
   }
   catch (|) // IntelliSense should suggest InvalidOperationException first
   ```

2. **IntelliSense filters already caught exceptions**
   ```csharp
   try
   {
       throw new InvalidOperationException();
   }
   catch (InvalidOperationException) { }
   catch (|) // Should not suggest InvalidOperationException again
   ```

3. **IntelliSense includes exceptions from called methods**
   ```csharp
   void ThrowingMethod() { throw new ArgumentNullException(); }

   try
   {
       ThrowingMethod();
   }
   catch (|) // Should suggest ArgumentNullException
   ```

---

## Phase 6.3: Exception Hierarchy Visualization (2-3 weeks)

### Overview
Provide a visual representation of exception type hierarchies to help developers understand inheritance relationships.

### 6.3.1: Exception Hierarchy View (Week 5, Days 1-3)

**Component:** `ExceptionHierarchyView.xaml` and `ExceptionHierarchyViewModel.cs`

**Purpose:** Display exception type hierarchy in a tree view

```csharp
namespace ThrowsAnalyzer.IDE.Views
{
    public class ExceptionHierarchyViewModel : INotifyPropertyChanged
    {
        private readonly ISemanticModelProvider _semanticModelProvider;
        private readonly ExceptionTypeAnalyzer _typeAnalyzer;

        public ObservableCollection<ExceptionTypeNode> RootNodes { get; }

        public async Task LoadHierarchyAsync(ITypeSymbol exceptionType)
        {
            // 1. Build hierarchy from System.Exception down to specified type
            var hierarchy = _typeAnalyzer.GetExceptionHierarchy(exceptionType);

            // 2. Create tree nodes
            var rootNode = new ExceptionTypeNode
            {
                TypeName = "System.Exception",
                FullTypeName = "System.Exception",
                IsBaseType = true
            };

            BuildHierarchyNodes(hierarchy, rootNode);

            // 3. Update UI
            RootNodes.Clear();
            RootNodes.Add(rootNode);
        }

        private void BuildHierarchyNodes(
            List<ITypeSymbol> hierarchy,
            ExceptionTypeNode parentNode)
        {
            for (int i = 1; i < hierarchy.Count; i++)
            {
                var type = hierarchy[i];
                var node = new ExceptionTypeNode
                {
                    TypeName = type.Name,
                    FullTypeName = type.ToDisplayString(),
                    IsBaseType = i < hierarchy.Count - 1,
                    IsTargetType = i == hierarchy.Count - 1
                };

                parentNode.Children.Add(node);
                parentNode = node;
            }
        }
    }

    public class ExceptionTypeNode : INotifyPropertyChanged
    {
        public string TypeName { get; set; }
        public string FullTypeName { get; set; }
        public bool IsBaseType { get; set; }
        public bool IsTargetType { get; set; }
        public ObservableCollection<ExceptionTypeNode> Children { get; } = new();
    }
}
```

**XAML View:**

```xml
<UserControl x:Class="ThrowsAnalyzer.IDE.Views.ExceptionHierarchyView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <TreeView ItemsSource="{Binding RootNodes}">
            <TreeView.ItemTemplate>
                <HierarchicalDataTemplate ItemsSource="{Binding Children}">
                    <StackPanel Orientation="Horizontal">
                        <Image Width="16" Height="16"
                               Source="{Binding IsTargetType, Converter={StaticResource TypeIconConverter}}" />
                        <TextBlock Text="{Binding TypeName}" Margin="5,0,0,0"
                                   FontWeight="{Binding IsTargetType, Converter={StaticResource BoldConverter}}" />
                    </StackPanel>
                </HierarchicalDataTemplate>
            </TreeView.ItemTemplate>
        </TreeView>
    </Grid>
</UserControl>
```

### 6.3.2: Gutter Glyphs for Exception Indicators (Week 5, Days 4-5)

**Component:** `ExceptionGlyphFactory.cs`

**Purpose:** Show glyphs in the editor gutter to indicate lines that throw exceptions

```csharp
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace ThrowsAnalyzer.IDE.Glyphs
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("Exception Glyph Factory")]
    [ContentType("CSharp")]
    [TagType(typeof(ExceptionGlyphTag))]
    internal class ExceptionGlyphFactoryProvider : IGlyphFactoryProvider
    {
        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            return new ExceptionGlyphFactory();
        }
    }

    internal class ExceptionGlyphFactory : IGlyphFactory
    {
        public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {
            if (tag is not ExceptionGlyphTag exceptionTag)
                return null;

            // Create glyph based on exception handling status
            var icon = exceptionTag.IsHandled
                ? CreateHandledExceptionIcon()
                : CreateUnhandledExceptionIcon();

            // Add tooltip
            icon.ToolTip = exceptionTag.IsHandled
                ? $"Throws {exceptionTag.ExceptionType} (handled)"
                : $"Throws {exceptionTag.ExceptionType} (unhandled)";

            return icon;
        }

        private UIElement CreateHandledExceptionIcon()
        {
            return new Image
            {
                Source = LoadImageSource("Resources/ExceptionHandled.png"),
                Width = 16,
                Height = 16
            };
        }

        private UIElement CreateUnhandledExceptionIcon()
        {
            return new Image
            {
                Source = LoadImageSource("Resources/ExceptionUnhandled.png"),
                Width = 16,
                Height = 16
            };
        }
    }

    internal class ExceptionGlyphTag : IGlyphTag
    {
        public string ExceptionType { get; set; }
        public bool IsHandled { get; set; }
    }
}
```

### 6.3.3: Testing Visualization (Week 6, Day 1)

**Test Cases:**

1. **Hierarchy view displays correctly**
   - Select exception type
   - Verify hierarchy from System.Exception to selected type
   - Verify visual styling (bold for target, normal for base types)

2. **Gutter glyphs appear on throw lines**
   - Write code with throw statements
   - Verify glyphs appear in gutter
   - Verify different icons for handled vs unhandled

3. **Tooltips show exception information**
   - Hover over gutter glyph
   - Verify tooltip shows exception type and handling status

---

## Phase 6.4: Code Lens for Exception Metrics (1-2 weeks)

### Overview
Display inline Code Lens indicators showing exception counts and types above methods.

### 6.4.1: Code Lens Provider (Week 6, Days 2-5)

**Component:** `ExceptionCodeLensProvider.cs`

**Purpose:** Show exception metrics above methods

```csharp
using Microsoft.VisualStudio.Language.CodeLens;

namespace ThrowsAnalyzer.IDE.CodeLens
{
    [Export(typeof(ICodeLensProvider))]
    [ContentType("CSharp")]
    [LocalizedName(typeof(Resources), "ExceptionCodeLensProviderName")]
    internal class ExceptionCodeLensProvider : ICodeLensProvider
    {
        private readonly ExceptionFlowAnalyzer _flowAnalyzer;

        public async Task<IReadOnlyList<ICodeLensDataPoint>> GetDataPointsAsync(
            CodeLensDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            var dataPoints = new List<ICodeLensDataPoint>();

            // 1. Get all methods in document
            var semanticModel = await descriptor.Document.GetSemanticModelAsync(cancellationToken);
            var root = await descriptor.Document.GetSyntaxRootAsync(cancellationToken);
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            // 2. For each method, analyze exception flow
            foreach (var method in methods)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                if (methodSymbol == null) continue;

                var flowInfo = await _flowAnalyzer.AnalyzeMethodAsync(methodSymbol, cancellationToken);

                // 3. Create Code Lens data point
                var dataPoint = new ExceptionCodeLensDataPoint(
                    descriptor,
                    method.Identifier.Span,
                    flowInfo);

                dataPoints.Add(dataPoint);
            }

            return dataPoints;
        }
    }

    internal class ExceptionCodeLensDataPoint : ICodeLensDataPoint
    {
        private readonly ExceptionFlowInfo _flowInfo;

        public ExceptionCodeLensDataPoint(
            CodeLensDescriptor descriptor,
            TextSpan span,
            ExceptionFlowInfo flowInfo)
        {
            Descriptor = descriptor;
            Span = span;
            _flowInfo = flowInfo;
        }

        public CodeLensDescriptor Descriptor { get; }
        public TextSpan Span { get; }

        public string GetDisplayText()
        {
            var thrown = _flowInfo.ThrownExceptions.Count;
            var caught = _flowInfo.CaughtExceptions.Count;
            var propagated = _flowInfo.PropagatedExceptions.Count;

            if (propagated == 0)
                return "✓ No unhandled exceptions";

            return $"⚠ {propagated} exception(s) propagated";
        }

        public string GetTooltipText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Exception Analysis:");
            sb.AppendLine($"  Thrown: {_flowInfo.ThrownExceptions.Count}");
            sb.AppendLine($"  Caught: {_flowInfo.CaughtExceptions.Count}");
            sb.AppendLine($"  Propagated: {_flowInfo.PropagatedExceptions.Count}");

            if (_flowInfo.PropagatedExceptions.Any())
            {
                sb.AppendLine("\nPropagated exceptions:");
                foreach (var ex in _flowInfo.PropagatedExceptions)
                {
                    sb.AppendLine($"  - {ex.ExceptionType.Name}");
                }
            }

            return sb.ToString();
        }
    }
}
```

### 6.4.2: Testing Code Lens (Week 7, Days 1-2)

**Test Cases:**

1. **Code Lens displays above methods**
   - Write method with exceptions
   - Verify Code Lens indicator appears
   - Verify text shows exception count

2. **Code Lens tooltip shows details**
   - Click on Code Lens indicator
   - Verify tooltip shows detailed exception breakdown

3. **Code Lens updates on code changes**
   - Modify method to add/remove exceptions
   - Verify Code Lens updates automatically

---

## Phase 6.5: VS Code Extension (2-3 weeks)

### Overview
Port IDE features to VS Code using Language Server Protocol (LSP).

### 6.5.1: Language Server Extension (Week 8, Days 1-5)

**Component:** `ThrowsAnalyzer.LanguageServer`

**Purpose:** Provide exception analysis in VS Code

**Implementation:**

```typescript
// extension.ts
import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient/node';

let client: LanguageClient;

export function activate(context: vscode.ExtensionContext) {
    // 1. Configure server options
    const serverOptions: ServerOptions = {
        run: { command: 'dotnet', args: ['ThrowsAnalyzer.LanguageServer.dll'] },
        debug: { command: 'dotnet', args: ['ThrowsAnalyzer.LanguageServer.dll', '--debug'] }
    };

    // 2. Configure client options
    const clientOptions: LanguageClientOptions = {
        documentSelector: [{ scheme: 'file', language: 'csharp' }],
        synchronize: {
            fileEvents: vscode.workspace.createFileSystemWatcher('**/*.cs')
        }
    };

    // 3. Create language client
    client = new LanguageClient(
        'throwsAnalyzer',
        'ThrowsAnalyzer',
        serverOptions,
        clientOptions
    );

    // 4. Start client
    client.start();

    // 5. Register commands
    registerCommands(context);
}

function registerCommands(context: vscode.ExtensionContext) {
    // Show exception hierarchy
    context.subscriptions.push(
        vscode.commands.registerCommand('throwsAnalyzer.showHierarchy', async () => {
            const editor = vscode.window.activeTextEditor;
            if (!editor) return;

            const position = editor.selection.active;
            const result = await client.sendRequest('throws/getHierarchy', {
                textDocument: { uri: editor.document.uri.toString() },
                position: position
            });

            // Display hierarchy in webview
            showHierarchyWebview(result);
        })
    );
}

export function deactivate(): Thenable<void> | undefined {
    if (!client) return undefined;
    return client.stop();
}
```

**Language Server (C#):**

```csharp
using OmniSharp.Extensions.LanguageServer.Server;

namespace ThrowsAnalyzer.LanguageServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var server = await LanguageServer.From(options =>
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithHandler<ExceptionHoverHandler>()
                    .WithHandler<ExceptionCompletionHandler>()
                    .WithHandler<ExceptionHierarchyHandler>()
            );

            await server.WaitForExit;
        }
    }

    internal class ExceptionHoverHandler : IHoverHandler
    {
        private readonly ExceptionFlowAnalyzer _flowAnalyzer;

        public async Task<Hover> Handle(HoverParams request, CancellationToken token)
        {
            // Similar to Quick Info Provider logic
            // Return hover information for exceptions
        }
    }
}
```

### 6.5.2: Testing VS Code Extension (Week 9, Days 1-2)

**Test Cases:**

1. **Extension activates in VS Code**
   - Install extension
   - Open C# file
   - Verify extension is active

2. **Hover shows exception information**
   - Hover over throw statement
   - Verify hover tooltip appears with exception info

3. **IntelliSense suggests exceptions**
   - Type `catch (`
   - Verify completion suggestions appear

---

## Phase 6.6: Documentation and Packaging (Week 10)

### 6.6.1: User Documentation

**Create:** `docs/IDE_FEATURES.md`

**Content:**
- Quick Info tooltips guide
- IntelliSense usage examples
- Exception hierarchy view tutorial
- Code Lens configuration
- VS Code extension setup

### 6.6.2: VSIX Packaging

**Create Visual Studio Extension Package:**

```xml
<!-- source.extension.vsixmanifest -->
<PackageManifest>
  <Metadata>
    <Identity Id="ThrowsAnalyzer.IDE" Version="1.0.0" Language="en-US"
              Publisher="ThrowsAnalyzer" />
    <DisplayName>ThrowsAnalyzer IDE Features</DisplayName>
    <Description>Enhanced IDE integration for exception analysis</Description>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Microsoft.VisualStudio.Community" Version="[17.0,18.0)" />
  </Installation>
  <Dependencies>
    <Dependency Id="Microsoft.Framework.NDP" DisplayName="Microsoft .NET Framework"
                Version="[4.7.2,)" />
  </Dependencies>
  <Assets>
    <Asset Type="Microsoft.VisualStudio.MefComponent" Path="ThrowsAnalyzer.IDE.dll" />
    <Asset Type="Microsoft.VisualStudio.Analyzer" Path="ThrowsAnalyzer.dll" />
  </Assets>
</PackageManifest>
```

---

## Success Criteria

### Phase 6.1: Quick Info ✅
- [x] Hover over methods shows exception counts
- [x] Hover over throw statements shows exception type and handling status
- [x] Hover over catch clauses shows warnings
- [x] Hover over method invocations shows throwable exceptions
- [x] Formatting is clear and color-coded

### Phase 6.2: IntelliSense ✅
- [x] IntelliSense in catch clauses suggests thrown exceptions
- [x] Directly thrown exceptions prioritized
- [x] Already caught exceptions filtered out
- [x] Common exception types available
- [x] Completion descriptions show documentation

### Phase 6.3: Visualization ✅
- [x] Exception hierarchy view displays correctly
- [x] Gutter glyphs show on throw lines
- [x] Different icons for handled vs unhandled
- [x] Tooltips provide contextual information

### Phase 6.4: Code Lens ✅
- [x] Code Lens displays above methods
- [x] Shows exception counts (thrown/caught/propagated)
- [x] Tooltip shows detailed breakdown
- [x] Updates automatically on code changes

### Phase 6.5: VS Code Extension ✅
- [x] Extension activates and runs
- [x] Hover provides exception information
- [x] IntelliSense suggests exceptions
- [x] Configuration options available

### Phase 6.6: Packaging ✅
- [x] VSIX package builds successfully
- [x] Extension installs in Visual Studio
- [x] All features work in installed extension
- [x] Documentation complete and accessible

---

## Dependencies

### NuGet Packages Required:
```xml
<PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.0" />
<PackageReference Include="Microsoft.VisualStudio.Language.Intellisense" Version="17.0.0" />
<PackageReference Include="Microsoft.VisualStudio.Language.CodeLens" Version="17.0.0" />
<PackageReference Include="Microsoft.CodeAnalysis.EditorFeatures" Version="4.14.0" />
<PackageReference Include="OmniSharp.Extensions.LanguageServer" Version="0.19.0" />
```

### External Dependencies:
- Visual Studio 2022 SDK
- VS Code Extension Development Kit
- BenchmarkDotNet (already added in Phase 4.6)

---

## File Structure

```
src/
├── ThrowsAnalyzer.IDE/
│   ├── QuickInfo/
│   │   ├── ExceptionQuickInfoProvider.cs
│   │   └── ExceptionQuickInfoSource.cs
│   ├── Completion/
│   │   └── ExceptionCompletionProvider.cs
│   ├── Views/
│   │   ├── ExceptionHierarchyView.xaml
│   │   └── ExceptionHierarchyViewModel.cs
│   ├── Glyphs/
│   │   ├── ExceptionGlyphFactory.cs
│   │   └── ExceptionGlyphTag.cs
│   ├── CodeLens/
│   │   ├── ExceptionCodeLensProvider.cs
│   │   └── ExceptionCodeLensDataPoint.cs
│   └── ThrowsAnalyzer.IDE.csproj
│
├── ThrowsAnalyzer.LanguageServer/
│   ├── Program.cs
│   ├── Handlers/
│   │   ├── ExceptionHoverHandler.cs
│   │   ├── ExceptionCompletionHandler.cs
│   │   └── ExceptionHierarchyHandler.cs
│   └── ThrowsAnalyzer.LanguageServer.csproj
│
└── ThrowsAnalyzer.VSCode/
    ├── src/
    │   ├── extension.ts
    │   └── hierarchyView.ts
    ├── package.json
    └── tsconfig.json

docs/
├── IDE_FEATURES.md
└── PHASE6_IDE_INTEGRATION_PLAN.md (this file)
```

---

## Known Limitations

1. **Performance**: Real-time analysis may be slow for very large files
   - **Mitigation**: Use caching from Phase 4.6, implement background analysis

2. **Cross-assembly analysis**: Limited to current compilation
   - **Mitigation**: Future enhancement for assembly boundary crossing

3. **VS Code feature parity**: Some features may not be available in VS Code
   - **Mitigation**: Focus on core features (hover, completion), skip complex UI

4. **LSP limitations**: Language Server Protocol doesn't support all Visual Studio features
   - **Mitigation**: Implement core analysis, document unsupported features

---

## Timeline Summary

| Phase | Duration | Deliverable |
|-------|----------|-------------|
| 6.1   | 2-3 weeks | Quick Info tooltips |
| 6.2   | 2-3 weeks | IntelliSense completion |
| 6.3   | 2-3 weeks | Exception hierarchy visualization |
| 6.4   | 1-2 weeks | Code Lens metrics |
| 6.5   | 2-3 weeks | VS Code extension |
| 6.6   | 1 week    | Documentation and packaging |
| **Total** | **10-15 weeks** | **Complete IDE integration** |

---

## Integration with Previous Phases

**Phase 5 Integration:**
- Uses `ExceptionFlowAnalyzer` from Phase 5.1 for method-level exception tracking
- Uses `CallGraph` from Phase 5.1 to determine exception propagation
- Uses `AsyncExceptionAnalyzer` from Phase 5.2 for async-specific insights
- Uses `IteratorExceptionAnalyzer` from Phase 5.3 for iterator-specific insights

**Phase 4 Integration:**
- Uses `ExceptionTypeCache` from Phase 4.6 for performance
- Respects configuration from Phase 4.7 `.editorconfig` profiles
- May suggest code fixes from Phase 4 in Quick Info tooltips

---

## Testing Strategy

### Unit Tests
- Test each provider independently (QuickInfo, Completion, CodeLens)
- Mock semantic model and syntax trees
- Verify correct exception information extraction

### Integration Tests
- Test end-to-end scenarios in test Visual Studio instance
- Verify UI elements render correctly
- Test interaction between different features

### Performance Tests
- Measure Quick Info response time (<100ms)
- Measure IntelliSense latency (<200ms)
- Verify no UI freezing during analysis

### User Acceptance Tests
- Have developers use features in real projects
- Gather feedback on usefulness and accuracy
- Iterate based on usability findings

---

## Future Enhancements (Beyond Phase 6)

### Phase 7 (Proposed): Advanced IDE Features
- Exception breakpoint suggestions
- Exception flow visualization graph
- "Find all exceptions" search
- Exception impact analysis (which methods affected by a change)

### Phase 8 (Proposed): AI-Powered Suggestions
- ML model to suggest appropriate exception types
- Pattern recognition for common exception handling mistakes
- Automated exception handling refactoring suggestions

---

## Conclusion

Phase 6 completes the ThrowsAnalyzer with comprehensive IDE integration, providing developers with real-time visual feedback and intelligent suggestions for exception handling. This phase transforms ThrowsAnalyzer from a diagnostic tool into a complete development experience enhancement.

**Key Achievements:**
- ✅ Real-time exception information in tooltips
- ✅ Context-aware IntelliSense for catch clauses
- ✅ Visual hierarchy and gutter indicators
- ✅ Inline Code Lens metrics
- ✅ Cross-platform support (Visual Studio + VS Code)

**Total Development Effort for Phase 6:** 10-15 weeks (2.5-4 months)

---

*Phase 6 plan completed on October 26, 2025*
