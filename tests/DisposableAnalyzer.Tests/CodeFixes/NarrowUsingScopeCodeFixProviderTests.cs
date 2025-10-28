using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.UsingStatementScopeAnalyzer,
    DisposableAnalyzer.CodeFixes.NarrowUsingScopeCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class NarrowUsingScopeCodeFixProviderTests
{
    [Fact]
    public async Task NarrowUsingScope_MoveStatementsOutside()
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
            stream.ReadByte();
            stream.WriteByte(1);
            // These statements don't use stream
            Console.WriteLine(""Done"");
            Console.WriteLine(""Finished"");
        }
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
    {
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        {
            stream.ReadByte();
            stream.WriteByte(1);
        }
        // These statements don't use stream
        Console.WriteLine(""Done"");
        Console.WriteLine(""Finished"");
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UsingStatementScopeToBroad)
            .WithLocation(9, 9)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task NarrowUsingScope_WithManyUnusedStatements()
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
            var data = stream.ReadByte();
            Console.WriteLine(data);
            Console.WriteLine(""Line 1"");
            Console.WriteLine(""Line 2"");
            Console.WriteLine(""Line 3"");
        }
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
    {
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        {
            var data = stream.ReadByte();
        }
        Console.WriteLine(data);
        Console.WriteLine(""Line 1"");
        Console.WriteLine(""Line 2"");
        Console.WriteLine(""Line 3"");
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UsingStatementScopeToBroad)
            .WithLocation(9, 9)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
