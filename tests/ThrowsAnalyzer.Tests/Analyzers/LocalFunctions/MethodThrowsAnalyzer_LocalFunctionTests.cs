using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.LocalFunctions;

[TestClass]
public class MethodThrowsAnalyzer_LocalFunctionTests
{
    [TestMethod]
    public async Task LocalFunctionWithThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    void LocalFunction(int value)
                    {
                        if (value < 0)
                            throw new System.ArgumentException("Value must be positive");
                    }

                    LocalFunction(42);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task LocalFunctionWithExpressionBodyThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    int LocalFunction(int value) => throw new System.NotImplementedException();

                    LocalFunction(42);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }
}
