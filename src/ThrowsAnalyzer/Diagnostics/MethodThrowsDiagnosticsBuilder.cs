using Microsoft.CodeAnalysis;

namespace ThrowsAnalyzer
{
    public static class MethodThrowsDiagnosticsBuilder
    {
        // THROWS001: Method contains throw statement
        public const string DiagnosticId001 = "THROWS001";
        private const string Category = "Usage";

        private static readonly LocalizableString Title001 = "Method contains throw statement";
        private static readonly LocalizableString MessageFormat001 = "Method '{0}' contains throw statement(s)";
        private static readonly LocalizableString Description001 = "Detects methods that contain throw statements.";

        public static readonly DiagnosticDescriptor MethodContainsThrowStatement = new DiagnosticDescriptor(
            DiagnosticId001,
            Title001,
            MessageFormat001,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description001);

        // THROWS002: Method contains unhandled throw statement
        public const string DiagnosticId002 = "THROWS002";

        private static readonly LocalizableString Title002 = "Method contains unhandled throw statement";
        private static readonly LocalizableString MessageFormat002 = "Method '{0}' contains throw statement(s) without try/catch handling";
        private static readonly LocalizableString Description002 = "Detects methods that contain throw statements not wrapped in try/catch blocks.";

        public static readonly DiagnosticDescriptor MethodContainsUnhandledThrow = new DiagnosticDescriptor(
            DiagnosticId002,
            Title002,
            MessageFormat002,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description002);

        // THROWS003: Method contains try/catch block
        public const string DiagnosticId003 = "THROWS003";

        private static readonly LocalizableString Title003 = "Method contains try/catch block";
        private static readonly LocalizableString MessageFormat003 = "Method '{0}' contains try/catch block(s)";
        private static readonly LocalizableString Description003 = "Detects methods that contain try/catch blocks.";

        public static readonly DiagnosticDescriptor MethodContainsTryCatch = new DiagnosticDescriptor(
            DiagnosticId003,
            Title003,
            MessageFormat003,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description003);

        // THROWS004: Rethrow modifies stack trace
        public const string DiagnosticId004 = "THROWS004";

        private static readonly LocalizableString Title004 = "Rethrow modifies stack trace";
        private static readonly LocalizableString MessageFormat004 = "Method '{0}' rethrows exception with 'throw ex;' which modifies stack trace - use 'throw;' instead";
        private static readonly LocalizableString Description004 = "Detects rethrows that use 'throw ex;' instead of 'throw;', which preserves the original stack trace.";

        public static readonly DiagnosticDescriptor RethrowAntiPattern = new DiagnosticDescriptor(
            DiagnosticId004,
            Title004,
            MessageFormat004,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description004);

        // THROWS007: Unreachable catch clause
        public const string DiagnosticId007 = "THROWS007";

        private static readonly LocalizableString Title007 = "Unreachable catch clause";
        private static readonly LocalizableString MessageFormat007 = "Catch clause for '{0}' is unreachable because '{1}' is caught first";
        private static readonly LocalizableString Description007 = "Detects catch clauses that are unreachable due to catch ordering.";

        public static readonly DiagnosticDescriptor CatchClauseOrdering = new DiagnosticDescriptor(
            DiagnosticId007,
            Title007,
            MessageFormat007,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description007);

        // THROWS008: Empty catch block swallows exceptions
        public const string DiagnosticId008 = "THROWS008";

        private static readonly LocalizableString Title008 = "Empty catch block swallows exceptions";
        private static readonly LocalizableString MessageFormat008 = "Method '{0}' has empty catch block that swallows exceptions";
        private static readonly LocalizableString Description008 = "Detects empty catch blocks that suppress exceptions without handling them.";

        public static readonly DiagnosticDescriptor EmptyCatchBlock = new DiagnosticDescriptor(
            DiagnosticId008,
            Title008,
            MessageFormat008,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description008);

        // THROWS009: Catch block only rethrows exception
        public const string DiagnosticId009 = "THROWS009";

        private static readonly LocalizableString Title009 = "Catch block only rethrows exception";
        private static readonly LocalizableString MessageFormat009 = "Method '{0}' has catch block that only rethrows - consider removing unnecessary catch";
        private static readonly LocalizableString Description009 = "Detects catch blocks that only rethrow without doing any work.";

        public static readonly DiagnosticDescriptor RethrowOnlyCatch = new DiagnosticDescriptor(
            DiagnosticId009,
            Title009,
            MessageFormat009,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description009);

        // THROWS010: Overly broad exception catch
        public const string DiagnosticId010 = "THROWS010";

        private static readonly LocalizableString Title010 = "Overly broad exception catch";
        private static readonly LocalizableString MessageFormat010 = "Method '{0}' catches '{1}' which is too broad - consider catching specific exception types";
        private static readonly LocalizableString Description010 = "Detects catch clauses for System.Exception or System.SystemException.";

        public static readonly DiagnosticDescriptor OverlyBroadCatch = new DiagnosticDescriptor(
            DiagnosticId010,
            Title010,
            MessageFormat010,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description010);
    }
}
