using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DoubleDisposeAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DoubleDisposeAnalyzerTests
{
    [Fact]
    public async Task DoubleDisposeWithoutNullCheck_ReportsDiagnostic()
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
        {|#0:stream.Dispose()|};
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DoubleDispose)
            .WithLocation(0)
            .WithArguments("stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DoubleDisposeWithNullCheck_NoDiagnostic()
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
        if (stream != null)
            stream.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DoubleDisposeWithNullConditional_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream?.Dispose();
        stream?.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposeInTryFinally_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        FileStream stream = null;
        try
        {
            stream = new FileStream(""test.txt"", FileMode.Open);
            // Use stream
        }
        finally
        {
            stream?.Dispose();
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task FieldDoubleDispose_ReportsDiagnostic()
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
        _stream.Dispose();
        {|#0:_stream.Dispose()|};
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DoubleDispose)
            .WithLocation(0)
            .WithArguments("_stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task ConditionalDoubleDispose_WithReassignment_NoDiagnostic()
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
        stream = null;
        stream?.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task MultipleVariablesDoubleDispose_ReportsMultipleDiagnostics()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        var stream1 = new FileStream(""test1.txt"", FileMode.Open);
        var stream2 = new FileStream(""test2.txt"", FileMode.Open);

        stream1.Dispose();
        stream2.Dispose();

        {|#0:stream1.Dispose()|};
        {|#1:stream2.Dispose()|};
    }
}";

        var expected1 = VerifyCS.Diagnostic(DiagnosticIds.DoubleDispose)
            .WithLocation(0)
            .WithArguments("stream1");

        var expected2 = VerifyCS.Diagnostic(DiagnosticIds.DoubleDispose)
            .WithLocation(1)
            .WithArguments("stream2");

        await VerifyCS.VerifyAnalyzerAsync(code, expected1, expected2);
    }

    [Fact]
    public async Task DisposeInDifferentScopes_NoFalsePositive()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        if (true)
        {
            var stream = new FileStream(""test.txt"", FileMode.Open);
            stream.Dispose();
        }
        else
        {
            var stream = new FileStream(""test.txt"", FileMode.Open);
            stream.Dispose();
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
