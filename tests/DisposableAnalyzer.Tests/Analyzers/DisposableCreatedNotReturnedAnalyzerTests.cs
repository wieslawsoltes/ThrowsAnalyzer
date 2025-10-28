using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableCreatedNotReturnedAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableCreatedNotReturnedAnalyzerTests
{
    [Fact]
    public async Task DisposableCreatedButNotReturnedOrStored_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        var stream = {|#0:new FileStream(""test.txt"", FileMode.Open)|};
        // stream is neither returned, stored in field, nor disposed
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableCreatedNotReturned)
            .WithLocation(0)
            .WithArguments("FileStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposableCreatedAndReturned_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public FileStream CreateStream()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        return stream;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableCreatedAndStoredInField_NoDiagnostic()
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
    public async Task DisposableCreatedAndDisposed_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableCreatedAndUsedInUsing_NoDiagnostic()
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
            // Use stream
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposablePassedToMethod_NoDiagnostic()
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
}
