using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Constructors;

[TestClass]
public class MethodThrowsAnalyzer_ConstructorTests
{
    [TestMethod]
    public async Task ConstructorWithThrowStatement_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public TestClass(string value)
                {
                    if (string.IsNullOrEmpty(value))
                        throw new System.ArgumentException("Value cannot be null or empty");
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("TestClass"));
    }

    [TestMethod]
    public async Task ConstructorWithoutThrowStatement_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public TestClass(int value)
                {
                    Value = value;
                }

                public int Value { get; }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task ConstructorWithThrowExpression_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public TestClass(string value) => throw new System.NotImplementedException();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }
}
