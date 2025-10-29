using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.AsyncDisposableNotUsedAnalyzer,
    DisposableAnalyzer.CodeFixes.ConvertToAwaitUsingCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class ConvertToAwaitUsingCodeFixProviderTests
{
    [Fact]
    public async Task ConvertToAwaitUsing_BasicCase()
    {
        var code = @"
using System;
using System.Threading.Tasks;

class AsyncResource : IAsyncDisposable, IDisposable
{
    public void Dispose()
    {
        // Synchronous disposal fallback
    }

    public ValueTask DisposeAsync() => default;
}

class TestClass
{
    void TestMethod()
    {
        using (var resource = new AsyncResource())
        {
            // Use resource
        }
    }
}";

        var fixedCode = @"
using System;
using System.Threading.Tasks;

class AsyncResource : IAsyncDisposable, IDisposable
{
    public void Dispose()
    {
        // Synchronous disposal fallback
    }

    public ValueTask DisposeAsync() => default;
}

class TestClass
{
    async System.Threading.Tasks.Task TestMethod()
    {
        await using (var resource = new AsyncResource())
        {
            // Use resource
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.AsyncDisposableNotUsed)
            .WithSpan(19, 9, 19, 14)
            .WithArguments("AsyncResource");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertToAwaitUsing_AlreadyAsyncMethod()
    {
        var code = @"
using System;
using System.Threading.Tasks;

class AsyncResource : IAsyncDisposable, IDisposable
{
    public void Dispose()
    {
        // Synchronous disposal fallback
    }

    public ValueTask DisposeAsync() => default;
}

class TestClass
{
    async Task TestMethod()
    {
        using (var resource = new AsyncResource())
        {
            await Task.Delay(100);
        }
    }
}";

        var fixedCode = @"
using System;
using System.Threading.Tasks;

class AsyncResource : IAsyncDisposable, IDisposable
{
    public void Dispose()
    {
        // Synchronous disposal fallback
    }

    public ValueTask DisposeAsync() => default;
}

class TestClass
{
    async Task TestMethod()
    {
        await using (var resource = new AsyncResource())
        {
            await Task.Delay(100);
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.AsyncDisposableNotUsed)
            .WithSpan(19, 9, 19, 14)
            .WithArguments("AsyncResource");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertToAwaitUsing_WithReturnType()
    {
        var code = @"
using System;
using System.Threading.Tasks;

class AsyncResource : IAsyncDisposable, IDisposable
{
    public void Dispose()
    {
        // Synchronous disposal fallback
    }

    public ValueTask DisposeAsync() => default;
}

class TestClass
{
    int TestMethod()
    {
        using (var resource = new AsyncResource())
        {
            return 42;
        }
    }
}";

        var fixedCode = @"
using System;
using System.Threading.Tasks;

class AsyncResource : IAsyncDisposable, IDisposable
{
    public void Dispose()
    {
        // Synchronous disposal fallback
    }

    public ValueTask DisposeAsync() => default;
}

class TestClass
{
    async Task<int> TestMethod()
    {
        await using (var resource = new AsyncResource())
        {
            return 42;
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.AsyncDisposableNotUsed)
            .WithSpan(19, 9, 19, 14)
            .WithArguments("AsyncResource");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
