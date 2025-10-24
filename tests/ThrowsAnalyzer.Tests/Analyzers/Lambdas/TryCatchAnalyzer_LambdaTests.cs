using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Lambdas;

[TestClass]
public class TryCatchAnalyzer_LambdaTests
{
    [TestMethod]
    public async Task SimpleLambda_WithTryCatch_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    System.Action<int> lambda = x =>
                    {
                        try
                        {
                            System.Console.WriteLine(x);
                        }
                        catch (System.Exception)
                        {
                        }
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ParenthesizedLambda_WithTryCatch_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    System.Action<int, string> lambda = (x, y) =>
                    {
                        try
                        {
                            System.Console.WriteLine(x + y);
                        }
                        catch (System.Exception)
                        {
                        }
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task Lambda_WithoutTryCatch_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    System.Action<int> lambda = x =>
                    {
                        System.Console.WriteLine(x);
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task NestedLambda_WithTryCatch_ShouldReportDiagnosticForNestedOnly()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    System.Action outer = () =>
                    {
                        System.Action inner = () =>
                        {
                            try
                            {
                                System.Console.WriteLine("test");
                            }
                            catch (System.Exception)
                            {
                            }
                        };
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }
}
