using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableNotImplementedAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableNotImplementedAnalyzerTests
{
    [Fact]
    public async Task ClassWithDisposableFieldNoIDisposable_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class {|#0:TestClass|}
{
    private FileStream _stream;

    public TestClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
            .WithLocation(0)
            .WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task ClassWithDisposableFieldImplementsIDisposable_NoDiagnostic()
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
    public async Task ClassWithMultipleDisposableFields_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class {|#0:TestClass|}
{
    private FileStream _stream1;
    private FileStream _stream2;
    private MemoryStream _stream3;

    public TestClass()
    {
        _stream1 = new FileStream(""test1.txt"", FileMode.Open);
        _stream2 = new FileStream(""test2.txt"", FileMode.Open);
        _stream3 = new MemoryStream();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
            .WithLocation(0)
            .WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task ClassWithNoDisposableFields_NoDiagnostic()
    {
        var code = @"
using System;

class TestClass
{
    private int _value;
    private string _name;

    public TestClass()
    {
        _value = 42;
        _name = ""test"";
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ClassWithStaticDisposableField_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private static FileStream _stream = new FileStream(""test.txt"", FileMode.Open);
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task StructWithDisposableField_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

struct {|#0:TestStruct|}
{
    private FileStream _stream;

    public TestStruct(string path)
    {
        _stream = new FileStream(path, FileMode.Open);
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
            .WithLocation(0)
            .WithArguments("TestStruct");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task ClassWithIAsyncDisposableField_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.Threading.Tasks;

class AsyncDisposableType : IAsyncDisposable
{
    public ValueTask DisposeAsync() => default;
}

class {|#0:TestClass|}
{
    private AsyncDisposableType _asyncDisposable;

    public TestClass()
    {
        _asyncDisposable = new AsyncDisposableType();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
            .WithLocation(0)
            .WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DerivedClassWithDisposableFieldBaseImplementsIDisposable_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class BaseClass : IDisposable
{
    public virtual void Dispose()
    {
    }
}

class DerivedClass : BaseClass
{
    private FileStream _stream;

    public DerivedClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }

    public override void Dispose()
    {
        _stream?.Dispose();
        base.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
