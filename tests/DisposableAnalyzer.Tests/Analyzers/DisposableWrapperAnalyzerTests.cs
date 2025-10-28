using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableWrapperAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableWrapperAnalyzerTests
{
    [Fact]
    public async Task WrapperClassWithoutDisposable_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class {|#0:StreamWrapper|}
{
    private readonly FileStream _stream;

    public StreamWrapper(FileStream stream)
    {
        _stream = stream;
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableWrapper)
            .WithLocation(0)
            .WithArguments("StreamWrapper");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task WrapperClassImplementsDisposable_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class StreamWrapper : IDisposable
{
    private readonly FileStream _stream;

    public StreamWrapper(FileStream stream)
    {
        _stream = stream;
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task WrapperWithNonDisposableField_NoDiagnostic()
    {
        var code = @"
using System;

class StringWrapper
{
    private readonly string _value;

    public StringWrapper(string value)
    {
        _value = value;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task WrapperNotOwningDisposable_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class StreamReader
{
    private readonly Stream _stream; // Not owned, just referenced

    public StreamReader(Stream stream)
    {
        _stream = stream;
    }

    public int ReadByte()
    {
        return _stream.ReadByte();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
