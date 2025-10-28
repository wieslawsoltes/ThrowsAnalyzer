using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using DisposableAnalyzer.Analyzers;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.SuppressFinalizerPerformanceAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class SuppressFinalizerPerformanceAnalyzerTests
{
    [Fact]
    public async Task DisposeWithoutFinalizerOrSuppressFinalize_NoDiagnostic()
    {
        var code = @"
using System;

class TestClass : IDisposable
{
    public void Dispose()
    {
        // No finalizer, so no need for GC.SuppressFinalize
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposeWithSuppressFinalizeButNoFinalizer_ReportsDiagnostic()
    {
        var code = @"
using System;

class TestClass : IDisposable
{
    public void {|#0:Dispose|}()
    {
        GC.SuppressFinalize(this);
    }
}";

        var expected = VerifyCS.Diagnostic(SuppressFinalizerPerformanceAnalyzer.UnnecessaryCallRule)
            .WithLocation(0)
            .WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task SealedClassWithoutFinalizer_NoDiagnostic()
    {
        var code = @"
using System;

sealed class TestClass : IDisposable
{
    public void Dispose()
    {
        // Sealed class without finalizer doesn't need GC.SuppressFinalize
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposeBoolPatternWithSuppressFinalizeButNoFinalizer_ReportsDiagnostic()
    {
        var code = @"
using System;

class TestClass : IDisposable
{
    public void {|#0:Dispose|}()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}";

        var expected = VerifyCS.Diagnostic(SuppressFinalizerPerformanceAnalyzer.UnnecessaryCallRule)
            .WithLocation(0)
            .WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task ClassWithFinalizer_ReportsDiagnostic()
    {
        var code = @"
using System;

class TestClass : IDisposable
{
    ~TestClass()
    {
        Dispose(false);
    }

    public void {|#0:Dispose|}()
    {
        Dispose(true);
        // Missing GC.SuppressFinalize(this)
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}";

        var expected = VerifyCS.Diagnostic(SuppressFinalizerPerformanceAnalyzer.MissingCallRule)
            .WithLocation(0)
            .WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task ClassWithFinalizerAndSuppressFinalize_NoDiagnostic()
    {
        var code = @"
using System;

class TestClass : IDisposable
{
    ~TestClass()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
