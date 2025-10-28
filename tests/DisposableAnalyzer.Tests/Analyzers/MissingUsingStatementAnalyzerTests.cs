using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.MissingUsingStatementAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class MissingUsingStatementAnalyzerTests
{
    [Fact]
    public async Task DisposableWithoutUsing_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        var {|#0:stream|} = new FileStream(""test.txt"", FileMode.Open);
        // Use stream
        stream.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.MissingUsingStatement)
            .WithLocation(0)
            .WithArguments("stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposableWithUsingStatement_NoDiagnostic()
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
    public async Task DisposableWithUsingDeclaration_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        using var stream = new FileStream(""test.txt"", FileMode.Open);
        // Use stream
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableAssignedToField_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream;

    public void TestMethod()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableReturned_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public FileStream CreateStream()
    {
        return new FileStream(""test.txt"", FileMode.Open);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposablePassedToMethod_WithOwnershipTransfer_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        TakeOwnership(stream);
    }

    private void TakeOwnership(FileStream stream)
    {
        // Takes ownership
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task MultipleDisposablesWithoutUsing_ReportsMultipleDiagnostics()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        var {|#0:stream1|} = new FileStream(""test1.txt"", FileMode.Open);
        var {|#1:stream2|} = new FileStream(""test2.txt"", FileMode.Open);

        stream1.Dispose();
        stream2.Dispose();
    }
}";

        var expected1 = VerifyCS.Diagnostic(DiagnosticIds.MissingUsingStatement)
            .WithLocation(0)
            .WithArguments("stream1");

        var expected2 = VerifyCS.Diagnostic(DiagnosticIds.MissingUsingStatement)
            .WithLocation(1)
            .WithArguments("stream2");

        await VerifyCS.VerifyAnalyzerAsync(code, expected1, expected2);
    }

    [Fact]
    public async Task DisposableInTryCatch_WithManualDispose_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        FileStream {|#0:stream|} = null;
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

        var expected = VerifyCS.Diagnostic(DiagnosticIds.MissingUsingStatement)
            .WithLocation(0)
            .WithArguments("stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }
}
