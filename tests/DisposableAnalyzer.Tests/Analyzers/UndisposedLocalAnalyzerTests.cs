using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.UndisposedLocalAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class UndisposedLocalAnalyzerTests
{
    [Fact]
    public async Task UndisposedLocal_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        // No disposal
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UndisposedLocal)
            .WithLocation(9, 13)
            .WithArguments("stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposedLocal_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task UsingStatement_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
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
    public async Task UsingDeclaration_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
    {
        using var stream = new FileStream(""test.txt"", FileMode.Open);
        // Use stream
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ReturnedLocal_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    FileStream TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        return stream; // Ownership transferred
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task AssignedToField_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream;

    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        _stream = stream; // Ownership transferred
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ConditionalDisposal_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream?.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
