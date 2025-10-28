using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.DisposableInIteratorAnalyzer,
    DisposableAnalyzer.CodeFixes.ExtractIteratorWrapperCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class ExtractIteratorWrapperCodeFixProviderTests
{
    [Fact]
    public async Task ExtractIteratorWrapper_BasicCase()
    {
        var code = @"
using System.Collections.Generic;
using System.IO;

class TestClass
{
    IEnumerable<string> ReadLines(string path)
    {
        using var reader = new StreamReader(path);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            yield return line;
        }
    }
}";

        var fixedCode = @"
using System.Collections.Generic;
using System.IO;

class TestClass
{
    IEnumerable<string> ReadLines(string path)
    {
        // TODO: Move using statement to the wrapper method
        using var reader = new StreamReader(path);
        return ReadLinesCore(reader);
    }

    private IEnumerable<string> ReadLinesCore(StreamReader reader)
    {
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            yield return line;
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableInIterator)
            .WithLocation(7, 27)
            .WithArguments("ReadLines");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task ExtractIteratorWrapper_WithMultipleYields()
    {
        var code = @"
using System.Collections.Generic;
using System.IO;

class TestClass
{
    IEnumerable<int> ProcessFile(string path)
    {
        var stream = new FileStream(path, FileMode.Open);
        yield return stream.ReadByte();
        yield return stream.ReadByte();
        stream.Dispose();
    }
}";

        var fixedCode = @"
using System.Collections.Generic;
using System.IO;

class TestClass
{
    IEnumerable<int> ProcessFile(string path)
    {
        // TODO: Move using statement to the wrapper method
        var stream = new FileStream(path, FileMode.Open);
        return ProcessFileCore(stream);
    }

    private IEnumerable<int> ProcessFileCore(FileStream stream)
    {
        yield return stream.ReadByte();
        yield return stream.ReadByte();
        stream.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableInIterator)
            .WithLocation(7, 22)
            .WithArguments("ProcessFile");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
