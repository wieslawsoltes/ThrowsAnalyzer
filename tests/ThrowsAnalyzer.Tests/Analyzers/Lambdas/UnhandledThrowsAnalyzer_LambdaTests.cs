using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Lambdas;

[TestClass]
public class UnhandledThrowsAnalyzer_LambdaTests
{
    [TestMethod]
    public async Task SimpleLambda_WithUnhandledThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    System.Action<int> lambda = x =>
                    {
                        throw new System.InvalidOperationException();
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ParenthesizedLambda_WithUnhandledThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    System.Action<int, string> lambda = (x, y) =>
                    {
                        throw new System.InvalidOperationException();
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task Lambda_WithHandledThrow_ShouldNotReportDiagnostic()
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
                            throw new System.InvalidOperationException();
                        }
                        catch (System.Exception)
                        {
                        }
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task NestedLambda_WithUnhandledThrow_ShouldReportDiagnosticForNestedOnly()
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
                            throw new System.InvalidOperationException();
                        };
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ExpressionBodiedLambda_WithThrowExpression_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    System.Func<int, int> lambda = x => throw new System.NotImplementedException();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }
}
