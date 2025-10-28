using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using DisposableAnalyzer.Analyzers;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposeAsyncPatternAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposeAsyncPatternAnalyzerTests
{
    [Fact]
    public async Task AsyncDisposableWithoutDisposeAsyncCore_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.Threading.Tasks;

class {|#0:TestClass|} : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        return default;
    }
}";

        var expected = VerifyCS.Diagnostic(DisposeAsyncPatternAnalyzer.MissingDisposeAsyncCoreRule)
            .WithLocation(0)
            .WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task AsyncDisposableWithDisposeAsyncCore_NoDiagnostic()
    {
        var code = @"
using System;
using System.Threading.Tasks;

class TestClass : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        return default;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task SimpleAsyncDisposable_NoDiagnostic()
    {
        var code = @"
using System;
using System.Threading.Tasks;

sealed class TestClass : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        // Sealed class doesn't need DisposeAsyncCore pattern
        return default;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task AsyncDisposableWithSyncDispose_NoDiagnostic()
    {
        var code = @"
using System;
using System.Threading.Tasks;

class TestClass : IDisposable, IAsyncDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        return default;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
