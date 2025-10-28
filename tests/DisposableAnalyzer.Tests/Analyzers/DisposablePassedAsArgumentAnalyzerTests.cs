using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposablePassedAsArgumentAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposablePassedAsArgumentAnalyzerTests
{
    [Fact]
    public async Task DisposablePassedToMethodThatTakesOwnership_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        ProcessStream(new FileStream(""test.txt"", FileMode.Open));
    }

    private void ProcessStream(FileStream stream)
    {
        using (stream)
        {
            // Process stream
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposablePassedToMethodParameter_ReportsDiagnostic()
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
            ProcessStream({|#0:stream|});
        }
    }

    private void ProcessStream(FileStream stream)
    {
        // Just uses stream, doesn't take ownership
        var length = stream.Length;
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposablePassedAsArgument)
            .WithLocation(0)
            .WithArguments("stream", "ProcessStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposablePassedToConstructor_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class StreamWrapper : IDisposable
{
    private readonly Stream _stream;

    public StreamWrapper(Stream stream)
    {
        _stream = stream;
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }
}

class TestClass
{
    public void TestMethod()
    {
        using (var wrapper = new StreamWrapper(new FileStream(""test.txt"", FileMode.Open)))
        {
            // Use wrapper
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableStoredInCollection_NoDiagnostic()
    {
        var code = @"
using System;
using System.Collections.Generic;
using System.IO;

class TestClass
{
    private List<FileStream> _streams = new List<FileStream>();

    public void TestMethod()
    {
        _streams.Add(new FileStream(""test.txt"", FileMode.Open));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
