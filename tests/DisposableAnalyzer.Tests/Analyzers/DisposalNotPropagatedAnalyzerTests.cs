using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposalNotPropagatedAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposalNotPropagatedAnalyzerTests
{
    [Fact]
    public async Task DisposableFieldNotDisposedInDispose_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class {|#0:TestClass|} : IDisposable
{
    private FileStream _stream;

    public TestClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }

    public void Dispose()
    {
        // Missing: _stream?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposalNotPropagated)
            .WithLocation(0)
            .WithArguments("TestClass", "_stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposableFieldDisposedProperly_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream;

    public TestClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task MultipleFieldsNotAllDisposed_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class {|#0:TestClass|} : IDisposable
{
    private FileStream _stream1;
    private FileStream _stream2;

    public TestClass()
    {
        _stream1 = new FileStream(""test1.txt"", FileMode.Open);
        _stream2 = new FileStream(""test2.txt"", FileMode.Open);
    }

    public void Dispose()
    {
        _stream1?.Dispose();
        // Missing: _stream2?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposalNotPropagated)
            .WithLocation(0)
            .WithArguments("TestClass", "_stream2");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposeBoolPatternProperlyImplemented_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream;

    public TestClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
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
            _stream?.Dispose();
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ClassWithoutDisposableFields_NoDiagnostic()
    {
        var code = @"
using System;

class TestClass : IDisposable
{
    private string _name;

    public void Dispose()
    {
        // Nothing to dispose
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
