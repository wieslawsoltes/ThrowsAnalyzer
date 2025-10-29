using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.UsingStatementScopeAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class UsingStatementScopeAnalyzerTests
{
    [Fact]
    public async Task UsingScopeTooBroad_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        {|#0:using|} (var stream = new FileStream(""test.txt"", FileMode.Open))
        {
            var data = ReadData(stream);
            // Many lines of code that don't use stream
            Console.WriteLine(""Line 1"");
            Console.WriteLine(""Line 2"");
            Console.WriteLine(""Line 3"");
            Console.WriteLine(""Line 4"");
            Console.WriteLine(""Line 5"");
            ProcessData(data);
        }
    }

    private byte[] ReadData(Stream stream)
    {
        var buffer = new byte[1024];
        stream.Read(buffer, 0, buffer.Length);
        return buffer;
    }

    private void ProcessData(byte[] data)
    {
        // Process data
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UsingStatementScopeToBroad)
            .WithLocation(0)
            .WithArguments("stream", "1", "7");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task UsingWithTightScope_NoDiagnostic()
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
            var buffer = new byte[1024];
            stream.Read(buffer, 0, buffer.Length);
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
    public void TestMethod()
    {
        using var stream = new FileStream(""test.txt"", FileMode.Open);
        var buffer = new byte[1024];
        stream.Read(buffer, 0, buffer.Length);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task NestedUsing_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            var line = reader.ReadLine();
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
