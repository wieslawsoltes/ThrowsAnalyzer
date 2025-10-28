using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.ResourceLeakAcrossMethodsAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class ResourceLeakAcrossMethodsAnalyzerTests
{
    [Fact]
    public async Task DisposableCreatedAndPassedWithoutDisposal_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void {|#0:TestMethod|}()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        ProcessStream(stream);
        // stream not disposed after ProcessStream returns
    }

    private void ProcessStream(FileStream stream)
    {
        // Uses but doesn't dispose
        var length = stream.Length;
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.ResourceLeakAcrossMethod)
            .WithLocation(0)
            .WithArguments("stream", "ProcessStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposablePassedToMethodThatDisposes_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        ProcessAndDisposeStream(stream);
    }

    private void ProcessAndDisposeStream(FileStream stream)
    {
        using (stream)
        {
            // Process and dispose
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableCreatedAndDisposedLocally_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        ProcessStream(stream);
        stream.Dispose();
    }

    private void ProcessStream(FileStream stream)
    {
        var length = stream.Length;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableUsedInUsingScope_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        {
            ProcessStream(stream);
        }
    }

    private void ProcessStream(FileStream stream)
    {
        var length = stream.Length;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableReturnedFromMethod_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public FileStream CreateAndInitializeStream()
    {
        var stream = CreateStream();
        InitializeStream(stream);
        return stream; // Ownership transferred to caller
    }

    private FileStream CreateStream()
    {
        return new FileStream(""test.txt"", FileMode.Open);
    }

    private void InitializeStream(FileStream stream)
    {
        // Initialize stream
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
