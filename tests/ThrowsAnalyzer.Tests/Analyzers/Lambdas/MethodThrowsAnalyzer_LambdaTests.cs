using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Lambdas;

[TestClass]
public class MethodThrowsAnalyzer_LambdaTests
{
    [TestMethod]
    public async Task LambdaWithThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    System.Func<int, int> func = x =>
                    {
                        if (x < 0)
                            throw new System.ArgumentException("Value must be positive");
                        return x;
                    };

                    func(42);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task LambdaWithExpressionBodyThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    System.Func<int, int> func = x => throw new System.NotImplementedException();

                    func(42);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }
}
