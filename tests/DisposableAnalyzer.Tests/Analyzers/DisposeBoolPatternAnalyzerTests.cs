using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using DisposableAnalyzer.Analyzers;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposeBoolPatternAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposeBoolPatternAnalyzerTests
{
    [Fact]
    public async Task ClassWithFinalizerButNoDisposeBool_ReportsDiagnostic()
    {
        var code = @"
using System;

class {|#0:TestClass|} : IDisposable
{
    ~TestClass()
    {
    }

    public void Dispose()
    {
    }
}";

        var expected = VerifyCS.Diagnostic(DisposeBoolPatternAnalyzer.MissingDisposeBoolRule)
            .WithLocation(0)
            .WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task ClassWithDisposeBoolPattern_NoDiagnostic()
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
        if (disposing)
        {
            // Dispose managed resources
        }
        // Dispose unmanaged resources
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ClassWithFinalizerButNoSuppressFinalize_ReportsDiagnostic()
    {
        var code = @"
using System;

class {|#0:TestClass|} : IDisposable
{
    ~TestClass()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        // Missing GC.SuppressFinalize(this)
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}";

        var expected = VerifyCS.Diagnostic(DisposeBoolPatternAnalyzer.MissingSuppressFinalize)
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
        // Simple disposal for sealed class without finalizer
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ClassWithoutDisposable_NoDiagnostic()
    {
        var code = @"
class TestClass
{
    ~TestClass()
    {
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task StructWithDisposable_NoDiagnostic()
    {
        var code = @"
using System;

struct TestStruct : IDisposable
{
    public void Dispose()
    {
        // Structs don't need Dispose(bool) pattern
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
