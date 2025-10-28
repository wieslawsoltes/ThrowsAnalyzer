using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableReturnedAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableReturnedAnalyzerTests
{
    [Fact]
    public async Task DisposableReturnedFromMethod_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public FileStream {|#0:CreateStream|}()
    {
        return new FileStream(""test.txt"", FileMode.Open);
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableReturned)
            .WithLocation(0)
            .WithArguments("CreateStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposableCreatedAndDisposedBeforeReturn_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public string ReadFile()
    {
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableStoredInField_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream;

    public void Initialize()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableReturnedFromProperty_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public FileStream {|#0:Stream|} => new FileStream(""test.txt"", FileMode.Open);
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableReturned)
            .WithLocation(0)
            .WithArguments("Stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposablePassedToConstructor_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class Wrapper
{
    private readonly FileStream _stream;

    public Wrapper(FileStream stream)
    {
        _stream = stream;
    }
}

class TestClass
{
    public Wrapper CreateWrapper()
    {
        return new Wrapper(new FileStream(""test.txt"", FileMode.Open));
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
