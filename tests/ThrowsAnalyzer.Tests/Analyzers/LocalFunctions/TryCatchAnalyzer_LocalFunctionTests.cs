using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.LocalFunctions;

[TestClass]
public class TryCatchAnalyzer_LocalFunctionTests
{
    [TestMethod]
    public async Task LocalFunction_WithTryCatch_ShouldReportDiagnostic()
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
                            System.Console.WriteLine(x);
                        }
                        catch (System.Exception)
                        {
                        }
                    }

                    LocalFunction(42);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task LocalFunction_WithoutTryCatch_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    void LocalFunction(int x)
                    {
                        System.Console.WriteLine(x);
                    }

                    LocalFunction(42);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task NestedLocalFunction_WithTryCatch_ShouldReportDiagnosticForNestedOnly()
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
                            try
                            {
                                System.Console.WriteLine("test");
                            }
                            catch (System.Exception)
                            {
                            }
                        }

                        InnerFunction();
                    }

                    OuterFunction();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task LocalFunction_WithMultipleTryCatchBlocks_ShouldReportDiagnostic()
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
                            System.Console.WriteLine(x);
                        }
                        catch (System.Exception)
                        {
                        }

                        try
                        {
                            System.Console.WriteLine(x * 2);
                        }
                        catch (System.Exception)
                        {
                        }
                    }

                    LocalFunction(42);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }
}
