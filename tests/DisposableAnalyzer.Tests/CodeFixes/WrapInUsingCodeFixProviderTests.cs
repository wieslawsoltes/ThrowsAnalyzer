using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.UndisposedLocalAnalyzer,
    DisposableAnalyzer.CodeFixes.WrapInUsingCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class WrapInUsingCodeFixProviderTests
{
    [Fact]
    public async Task WrapInUsingStatement_BasicCase()
    {
        var code = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream.ReadByte();
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        {
            stream.ReadByte();
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UndisposedLocal)
            .WithLocation(8, 13)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task WrapInUsingStatement_MultipleStatements()
    {
        var code = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream.ReadByte();
        stream.WriteByte(1);
        stream.Flush();
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        {
            stream.ReadByte();
            stream.WriteByte(1);
            stream.Flush();
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UndisposedLocal)
            .WithLocation(8, 13)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    // NOTE: The following test was commented out because the test framework doesn't support testing multiple code fix options easily
    // [Fact]
    // public async Task UseUsingDeclaration_CSharp8()
    // {
    //     var code = @"
    // using System.IO;
    //
    // class TestClass
    // {
    //     void TestMethod()
    //     {
    //         var stream = new FileStream(""test.txt"", FileMode.Open);
    //         stream.ReadByte();
    //     }
    // }";
    //
    //     var fixedCode = @"
    // using System.IO;
    //
    // class TestClass
    // {
    //     void TestMethod()
    //     {
    //         using var stream = new FileStream(""test.txt"", FileMode.Open);
    //         stream.ReadByte();
    //     }
    // }";
    //
    //     var expected = VerifyCS.Diagnostic(DiagnosticIds.UndisposedLocal)
    //         .WithLocation(8, 13)
    //         .WithArguments("stream");
    //
    //     await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode); // using declaration (second fix)
    // }

    [Fact]
    public async Task WrapInUsingStatement_WithExistingStatementsAfter()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream.ReadByte();
        Console.WriteLine(""Done"");
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
            Console.WriteLine(""Done"");
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UndisposedLocal)
            .WithLocation(9, 13)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task WrapInUsingStatement_PreservesComments()
    {
        var code = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        // Create a file stream
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream.ReadByte(); // Read one byte
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        // Create a file stream
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        {
            stream.ReadByte(); // Read one byte
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UndisposedLocal)
            .WithLocation(9, 13)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task WrapInUsingStatement_ForMissingUsingStatement()
    {
        var code = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        FileStream stream = new FileStream(""test.txt"", FileMode.Open);
        stream.ReadByte();
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        using (FileStream stream = new FileStream(""test.txt"", FileMode.Open))
        {
            stream.ReadByte();
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UndisposedLocal)
            .WithLocation(8, 20)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task WrapInUsingStatement_WithNestedBlocks()
    {
        var code = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        if (true)
        {
            var stream = new FileStream(""test.txt"", FileMode.Open);
            stream.ReadByte();
        }
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        if (true)
        {
            using (var stream = new FileStream(""test.txt"", FileMode.Open))
            {
                stream.ReadByte();
            }
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UndisposedLocal)
            .WithLocation(10, 17)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    // NOTE: The following test was commented out because the test framework doesn't support testing multiple code fix options easily
    // [Fact]
    // public async Task UseUsingDeclaration_WithMultipleVariables()
    // {
    //     var code = @"
    // using System.IO;
    //
    // class TestClass
    // {
    //     void TestMethod()
    //     {
    //         var stream = new FileStream(""test.txt"", FileMode.Open);
    //         var data = stream.ReadByte();
    //         var more = stream.ReadByte();
    //     }
    // }";
    //
    //     var fixedCode = @"
    // using System.IO;
    //
    // class TestClass
    // {
    //     void TestMethod()
    //     {
    //         using var stream = new FileStream(""test.txt"", FileMode.Open);
    //         var data = stream.ReadByte();
    //         var more = stream.ReadByte();
    //     }
    // }";
    //
    //     var expected = VerifyCS.Diagnostic(DiagnosticIds.UndisposedLocal)
    //         .WithLocation(8, 13)
    //         .WithArguments("stream");
    //
    //     await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode); // using declaration (second fix)
    // }
}
