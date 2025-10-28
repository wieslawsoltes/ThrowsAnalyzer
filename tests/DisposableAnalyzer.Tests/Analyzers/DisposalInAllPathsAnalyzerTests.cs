using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposalInAllPathsAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposalInAllPathsAnalyzerTests
{
    [Fact]
    public async Task DisposableNotDisposedInAllPaths_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod(bool condition)
    {
        var stream = {|#0:new FileStream(""test.txt"", FileMode.Open)|};

        if (condition)
        {
            stream.Dispose();
            return;
        }

        // Missing disposal in else path
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposalInAllPaths)
            .WithLocation(0)
            .WithArguments("stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposableDisposedInAllPaths_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod(bool condition)
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);

        if (condition)
        {
            stream.Dispose();
            return;
        }

        stream.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableInTryWithFinallyDisposal_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        FileStream stream = null;
        try
        {
            stream = new FileStream(""test.txt"", FileMode.Open);

            if (stream.Length > 0)
            {
                return;
            }

            // Other operations
        }
        finally
        {
            stream?.Dispose();
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableInSwitchNotAllPathsDispose_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod(int option)
    {
        var stream = {|#0:new FileStream(""test.txt"", FileMode.Open)|};

        switch (option)
        {
            case 1:
                stream.Dispose();
                break;
            case 2:
                // Uses stream but doesn't dispose
                var length = stream.Length;
                break;
            default:
                stream.Dispose();
                break;
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposalInAllPaths)
            .WithLocation(0)
            .WithArguments("stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposableUsedInUsing_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod(bool condition)
    {
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        {
            if (condition)
            {
                return;
            }

            // Other operations
        }
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
    public FileStream TestMethod(bool condition)
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);

        if (condition)
        {
            return stream; // Ownership transferred
        }

        return stream; // All paths return
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
