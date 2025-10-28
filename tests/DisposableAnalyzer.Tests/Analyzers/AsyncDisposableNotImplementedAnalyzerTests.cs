using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.AsyncDisposableNotImplementedAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class AsyncDisposableNotImplementedAnalyzerTests
{
    [Fact]
    public async Task ClassWithAsyncDisposableFieldNoIAsyncDisposable_ReportsDiagnostic()
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

        var expected = VerifyCS.Diagnostic(DiagnosticIds.AsyncDisposableNotImplemented)
            .WithLocation(0)
            .WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task ClassWithAsyncDisposableFieldImplementsIAsyncDisposable_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;
using System.Threading.Tasks;

class TestClass : IAsyncDisposable
{
    private FileStream _stream;

    public TestClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }

    public async ValueTask DisposeAsync()
    {
        if (_stream != null)
        {
            await _stream.DisposeAsync();
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ClassWithSyncDisposableOnly_NoDiagnostic()
    {
        var code = @"
using System;

class SyncOnlyDisposable : IDisposable
{
    public void Dispose() { }
}

class TestClass
{
    private SyncOnlyDisposable _resource;

    public TestClass()
    {
        _resource = new SyncOnlyDisposable();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ClassWithOnlyIDisposable_NoDiagnostic()
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
    public async Task ClassWithMultipleAsyncDisposableFields_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class {|#0:TestClass|}
{
    private FileStream _stream1;
    private FileStream _stream2;

    public TestClass()
    {
        _stream1 = new FileStream(""test1.txt"", FileMode.Open);
        _stream2 = new FileStream(""test2.txt"", FileMode.Open);
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.AsyncDisposableNotImplemented)
            .WithLocation(0)
            .WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task StructWithAsyncDisposableField_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

struct TestStruct
{
    private FileStream _stream;

    public TestStruct(string path)
    {
        _stream = new FileStream(path, FileMode.Open);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
