using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.DisposableCollectionAnalyzer,
    DisposableAnalyzer.CodeFixes.DisposableCollectionCleanupCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class DisposableCollectionCleanupCodeFixProviderTests
{
    [Fact]
    public async Task AddCollectionCleanup_ForListField()
    {
        var code = @"
using System;
using System.Collections.Generic;
using System.IO;

class TestClass : IDisposable
{
    private List<FileStream> _streams;

    public void Dispose()
    {
        // Missing disposal of collection items
    }
}";

        var fixedCode = @"
using System;
using System.Collections.Generic;
using System.IO;

class TestClass : IDisposable
{
    private List<FileStream> _streams;

    public void Dispose()
    {
        // Missing disposal of collection items
        if (_streams != null)
        {
            foreach (var item in _streams)
            {
                item?.Dispose();
            }
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableCollection)
            .WithLocation(8, 31)
            .WithArguments("_streams", "TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task AddCollectionCleanup_ForArrayField()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream[] _streams;

    public void Dispose()
    {
        // Missing disposal of array items
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream[] _streams;

    public void Dispose()
    {
        // Missing disposal of array items
        if (_streams != null)
        {
            foreach (var item in _streams)
            {
                item?.Dispose();
            }
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableCollection)
            .WithLocation(7, 26)
            .WithArguments("_streams", "TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task AddCollectionCleanup_WithExistingDisposeLogic()
    {
        var code = @"
using System;
using System.Collections.Generic;
using System.IO;

class TestClass : IDisposable
{
    private List<FileStream> _streams;
    private FileStream _mainStream;

    public void Dispose()
    {
        _mainStream?.Dispose();
        // Missing disposal of collection items
    }
}";

        var fixedCode = @"
using System;
using System.Collections.Generic;
using System.IO;

class TestClass : IDisposable
{
    private List<FileStream> _streams;
    private FileStream _mainStream;

    public void Dispose()
    {
        _mainStream?.Dispose();
        // Missing disposal of collection items
        if (_streams != null)
        {
            foreach (var item in _streams)
            {
                item?.Dispose();
            }
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableCollection)
            .WithLocation(8, 31)
            .WithArguments("_streams", "TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
