using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.AsyncDisposableNotUsedAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class AsyncDisposableNotUsedAnalyzerTests
{
    [Fact]
    public async Task IAsyncDisposableWithSyncUsing_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        {|#0:using|} (var stream = new FileStream(""test.txt"", FileMode.Open))
        {
            // Use stream
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.AsyncDisposableNotUsed)
            .WithLocation(0)
            .WithArguments("FileStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task IAsyncDisposableWithAwaitUsing_NoDiagnostic()
    {
        var code = @"
using System;
using System.Threading.Tasks;

class AsyncDisposableType : IAsyncDisposable
{
    public ValueTask DisposeAsync() => default;
}

class TestClass
{
    public async Task TestMethod()
    {
        await using (var obj = new AsyncDisposableType())
        {
            // Use obj
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task IAsyncDisposableWithAwaitUsingDeclaration_NoDiagnostic()
    {
        var code = @"
using System;
using System.Threading.Tasks;

class AsyncDisposableType : IAsyncDisposable
{
    public ValueTask DisposeAsync() => default;
}

class TestClass
{
    public async Task TestMethod()
    {
        await using var obj = new AsyncDisposableType();
        // Use obj
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task SyncDisposableWithSyncUsing_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class SyncOnlyDisposable : IDisposable
{
    public void Dispose() { }
}

class TestClass
{
    public void TestMethod()
    {
        using (var obj = new SyncOnlyDisposable())
        {
            // Use obj
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task BothIDisposableAndIAsyncDisposable_SyncUsing_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        {|#0:using|} (var stream = new FileStream(""test.txt"", FileMode.Open))
        {
            // Use stream
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.AsyncDisposableNotUsed)
            .WithLocation(0)
            .WithArguments("FileStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task IAsyncDisposableManualDispose_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;
using System.Threading.Tasks;

class TestClass
{
    public async Task TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        // Use stream
        await {|#0:stream.DisposeAsync()|};
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.AsyncDisposableNotUsed)
            .WithLocation(0)
            .WithArguments("FileStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task IAsyncDisposableInField_NoDiagnostic()
    {
        var code = @"
using System;
using System.Threading.Tasks;

class AsyncDisposableType : IAsyncDisposable
{
    public ValueTask DisposeAsync() => default;
}

class TestClass
{
    private AsyncDisposableType _obj;

    public void Initialize()
    {
        _obj = new AsyncDisposableType();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
