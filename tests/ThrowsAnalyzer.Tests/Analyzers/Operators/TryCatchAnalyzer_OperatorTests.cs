using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Operators;

[TestClass]
public class TryCatchAnalyzer_OperatorTests
{
    [TestMethod]
    public async Task BinaryOperator_WithTryCatch_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public int Value { get; set; }

                public static TestClass operator +(TestClass a, TestClass b)
                {
                    try
                    {
                        return new TestClass { Value = a.Value + b.Value };
                    }
                    catch (System.Exception)
                    {
                        return new TestClass();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task UnaryOperator_WithTryCatch_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public int Value { get; set; }

                public static TestClass operator -(TestClass a)
                {
                    try
                    {
                        return new TestClass { Value = -a.Value };
                    }
                    catch (System.Exception)
                    {
                        return new TestClass();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ConversionOperator_WithTryCatch_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public int Value { get; set; }

                public static explicit operator int(TestClass test)
                {
                    try
                    {
                        return test.Value;
                    }
                    catch (System.Exception)
                    {
                        return 0;
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task Operator_WithoutTryCatch_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public int Value { get; set; }

                public static TestClass operator +(TestClass a, TestClass b)
                {
                    return new TestClass { Value = a.Value + b.Value };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }
}
