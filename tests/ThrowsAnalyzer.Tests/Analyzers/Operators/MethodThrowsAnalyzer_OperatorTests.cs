using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Operators;

[TestClass]
public class MethodThrowsAnalyzer_OperatorTests
{
    [TestMethod]
    public async Task OperatorWithThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public static TestClass operator +(TestClass a, TestClass b)
                {
                    if (a == null || b == null)
                        throw new System.ArgumentNullException();
                    return new TestClass();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ConversionOperatorWithThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public static explicit operator int(TestClass value)
                {
                    if (value == null)
                        throw new System.InvalidCastException();
                    return 42;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }
}
