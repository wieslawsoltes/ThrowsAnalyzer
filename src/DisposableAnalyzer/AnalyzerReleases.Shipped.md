## Release 1.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DISP001 | Resource Management | Warning | UndisposedLocalAnalyzer: Local disposable not disposed
DISP002 | Resource Management | Warning | UndisposedFieldAnalyzer: Disposable field not disposed in type
DISP003 | Resource Management | Warning | DoubleDisposeAnalyzer: Potential double disposal
DISP004 | Resource Management | Warning | MissingUsingStatementAnalyzer: Should use 'using' statement
DISP006 | Style | Info | UsingDeclarationRecommendedAnalyzer: Use using declaration (C# 8+)
DISP007 | Design | Warning | DisposableNotImplementedAnalyzer: Type has disposable field but doesn't implement IDisposable
DISP008 | Design | Warning | DisposeBoolPatternAnalyzer: Dispose(bool) pattern violations
DISP009 | Reliability | Warning | DisposableBaseCallAnalyzer: Missing base.Dispose() call
DISP010 | Reliability | Warning | DisposedFieldAccessAnalyzer: Access to disposed field
DISP011 | Usage | Warning | AsyncDisposableNotUsedAnalyzer: Should use await using for IAsyncDisposable
DISP012 | Design | Info | AsyncDisposableNotImplementedAnalyzer: Should implement IAsyncDisposable
DISP013 | Design | Info/Warning | DisposeAsyncPatternAnalyzer: DisposeAsync pattern violations
DISP014 | Resource Management | Warning | DisposableInLambdaAnalyzer: Disposable resource in lambda
DISP015 | Resource Management | Warning | DisposableInIteratorAnalyzer: Disposable in iterator method
DISP016 | Documentation | Info | DisposableReturnedAnalyzer: Disposable returned without transfer documentation
DISP017 | Design | Info | DisposablePassedAsArgumentAnalyzer: Disposal responsibility unclear
DISP018 | Reliability | Warning | DisposableInConstructorAnalyzer: Exception in constructor with disposable
DISP019 | Reliability | Info/Warning | DisposableInFinalizerAnalyzer: Finalizer without disposal
DISP020 | Resource Management | Warning | DisposableCollectionAnalyzer: Collection of disposables not disposed
DISP026 | Design | Info | CompositeDisposableRecommendedAnalyzer: Consider CompositeDisposable pattern
DISP027 | Design | Info | DisposableFactoryPatternAnalyzer: Factory method naming for disposables
DISP028 | Design | Warning | DisposableWrapperAnalyzer: Wrapper class should implement IDisposable
DISP029 | Performance | Info | DisposableStructAnalyzer: Disposable struct patterns
DISP030 | Performance | Warning/Info | SuppressFinalizerPerformanceAnalyzer: GC.SuppressFinalize usage

### Code Fix Providers

Provider | Fixes | Description
---------|-------|-------------
WrapInUsingCodeFixProvider | DISP001, DISP004 | Wraps disposable in using statement or declaration
ImplementIDisposableCodeFixProvider | DISP002, DISP007 | Implements IDisposable interface
AddNullCheckBeforeDisposeCodeFixProvider | DISP003 | Adds null check before disposal
ConvertToAwaitUsingCodeFixProvider | DISP011 | Converts using to await using for async disposal
ImplementIAsyncDisposableCodeFixProvider | DISP012 | Implements IAsyncDisposable interface
DocumentDisposalOwnershipCodeFixProvider | DISP016 | Adds XML documentation for disposal ownership
ExtractIteratorWrapperCodeFixProvider | DISP015 | Extracts iterator logic with proper disposal
AddExceptionSafetyCodeFixProvider | DISP018 | Adds try-finally for exception safety in constructors
RenameToFactoryPatternCodeFixProvider | DISP027 | Renames methods to factory pattern naming (Create/Build)
AddSuppressFinalizeCodeFixProvider | DISP030 | Adds or removes GC.SuppressFinalize calls
