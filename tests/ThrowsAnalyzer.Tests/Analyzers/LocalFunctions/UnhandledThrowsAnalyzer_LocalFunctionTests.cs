using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.LocalFunctions;

[TestClass]
public class UnhandledThrowsAnalyzer_LocalFunctionTests
{
    [TestMethod]
    public async Task LocalFunction_WithUnhandledThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    void LocalFunction(int x)
                    {
                        if (x < 0)
                            throw new System.ArgumentException("Value must be positive");
                    }

                    LocalFunction(42);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task LocalFunction_WithHandledThrow_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    void LocalFunction(int x)
                    {
                        try
                        {
                            throw new System.InvalidOperationException();
                        }
                        catch (System.Exception)
                        {
                        }
                    }

                    LocalFunction(42);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task NestedLocalFunction_WithUnhandledThrow_ShouldReportDiagnosticForNestedOnly()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    void OuterFunction()
                    {
                        void InnerFunction()
                        {
                            throw new System.InvalidOperationException();
                        }

                        InnerFunction();
                    }

                    OuterFunction();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task LocalFunction_WithExpressionBody_WithThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    int LocalFunction(int x) => throw new System.NotImplementedException();

                    LocalFunction(42);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }
}
