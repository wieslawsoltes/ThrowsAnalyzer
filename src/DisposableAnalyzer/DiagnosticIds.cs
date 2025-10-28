namespace DisposableAnalyzer;

/// <summary>
/// Diagnostic IDs for DisposableAnalyzer rules.
/// </summary>
public static class DiagnosticIds
{
    // Basic Disposal Issues (DISP001-010)
    public const string UndisposedLocal = "DISP001";
    public const string UndisposedField = "DISP002";
    public const string DoubleDispose = "DISP003";
    public const string MissingUsingStatement = "DISP004";
    public const string UsingStatementScopeToBroad = "DISP005";
    public const string UsingDeclarationRecommended = "DISP006";
    public const string DisposableNotImplemented = "DISP007";
    public const string DisposeBoolPattern = "DISP008";
    public const string DisposableBaseCall = "DISP009";
    public const string DisposedFieldAccess = "DISP010";

    // Async Disposal Patterns (DISP011-013)
    public const string AsyncDisposableNotUsed = "DISP011";
    public const string AsyncDisposableNotImplemented = "DISP012";
    public const string DisposeAsyncPattern = "DISP013";

    // Disposal in Special Contexts (DISP014-017)
    public const string DisposableInLambda = "DISP014";
    public const string DisposableInIterator = "DISP015";
    public const string DisposableReturned = "DISP016";
    public const string DisposablePassedAsArgument = "DISP017";

    // Resource Management Anti-Patterns (DISP018-020)
    public const string DisposableInConstructor = "DISP018";
    public const string DisposableInFinalizer = "DISP019";
    public const string DisposableCollection = "DISP020";

    // Call Graph & Flow Analysis (DISP021-025)
    public const string DisposalNotPropagated = "DISP021";
    public const string DisposableCreatedNotReturned = "DISP022";
    public const string ResourceLeakAcrossMethod = "DISP023";
    public const string ConditionalOwnership = "DISP024";
    public const string DisposalInAllPaths = "DISP025";

    // Best Practices & Design Patterns (DISP026-030)
    public const string CompositeDisposableRecommended = "DISP026";
    public const string DisposableFactoryPattern = "DISP027";
    public const string DisposableWrapper = "DISP028";
    public const string DisposableStruct = "DISP029";
    public const string SuppressFinalizerPerformance = "DISP030";
}
