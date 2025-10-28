using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposedFieldAccessAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposedFieldAccessAnalyzerTests
{
    [Fact]
    public async Task FieldAccessAfterDispose_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream;

    public void TestMethod()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
        _stream.Dispose();
        var length = {|#0:_stream|}.Length; // Access after dispose
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposedFieldAccess)
            .WithLocation(0)
            .WithArguments("_stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task FieldAccessBeforeDispose_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream;

    public void TestMethod()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
        var length = _stream.Length; // Access before dispose - OK
        _stream.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task LocalAccessAfterDispose_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream.Dispose();
        // Analyzer only checks fields, not locals
        var length = stream.Length;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task FieldAccessInDifferentMethod_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream;

    public void Initialize()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }

    public void UseStream()
    {
        var length = _stream.Length; // OK - no dispose in this method
    }

    public void Cleanup()
    {
        _stream.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
