using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.ConditionalOwnershipAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class ConditionalOwnershipAnalyzerTests
{
    [Fact]
    public async Task ConditionalDisposableCreationWithoutAllPathsDisposing_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void {|#0:TestMethod|}(bool condition)
    {
        FileStream stream = null;

        if (condition)
        {
            stream = new FileStream(""test.txt"", FileMode.Open);
        }

        // Only disposed in one path
        if (condition)
        {
            stream?.Dispose();
        }
        // Missing else clause to dispose in all paths
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.ConditionalOwnership)
            .WithLocation(0)
            .WithArguments("stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task ConditionalDisposableWithProperDisposal_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod(bool condition)
    {
        FileStream stream = null;

        if (condition)
        {
            stream = new FileStream(""test.txt"", FileMode.Open);
        }

        stream?.Dispose(); // Disposed in all paths where created
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableInTryFinallyPattern_NoDiagnostic()
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
            // Use stream
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
    public async Task DisposableInUsingStatement_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod(bool condition)
    {
        if (condition)
        {
            using (var stream = new FileStream(""test.txt"", FileMode.Open))
            {
                // Use stream
            }
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ConditionalCreationWithFieldStorage_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream;

    public void TestMethod(bool condition)
    {
        if (condition)
        {
            _stream = new FileStream(""test.txt"", FileMode.Open);
        }
        // Field ownership - disposed elsewhere
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
