using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.AsyncDisposableNotImplementedAnalyzer,
    DisposableAnalyzer.CodeFixes.ImplementIAsyncDisposableCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class ImplementIAsyncDisposableCodeFixProviderTests
{
    [Fact]
    public async Task ImplementIAsyncDisposable_ForTypeWithAsyncDisposal()
    {
        var code = @"
using System;
using System.IO;
using System.Threading.Tasks;

class TestClass : IDisposable
{
    private FileStream _stream;

    public void Dispose()
    {
        _stream?.DisposeAsync(); // Using async disposal in sync method
    }
}";

        var fixedCode = @"
using System;
using System.IO;
using System.Threading.Tasks;

class TestClass : IDisposable, IAsyncDisposable
{
    private FileStream _stream;

    public void Dispose()
    {
        _stream?.DisposeAsync(); // Using async disposal in sync method
    }

    public async ValueTask DisposeAsync()
    {
        // TODO: Dispose async resources
        await Task.CompletedTask;
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.AsyncDisposableNotImplemented)
            .WithLocation(6, 7)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task ImplementIAsyncDisposable_BasicClass()
    {
        var code = @"
using System;
using System.Threading.Tasks;

class TestClass
{
    public void Cleanup()
    {
        var task = SomeAsyncCleanup();
        task.Wait(); // Should use async disposal pattern
    }

    private async Task SomeAsyncCleanup()
    {
        await Task.Delay(100);
    }
}";

        var fixedCode = @"
using System;
using System.Threading.Tasks;

class TestClass : IAsyncDisposable
{
    public void Cleanup()
    {
        var task = SomeAsyncCleanup();
        task.Wait(); // Should use async disposal pattern
    }

    private async Task SomeAsyncCleanup()
    {
        await Task.Delay(100);
    }

    public async ValueTask DisposeAsync()
    {
        // TODO: Dispose async resources
        await Task.CompletedTask;
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.AsyncDisposableNotImplemented)
            .WithLocation(5, 7)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
