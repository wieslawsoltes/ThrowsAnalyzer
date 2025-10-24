using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Operators;

[TestClass]
public class UnhandledThrowsAnalyzer_OperatorTests
{
    [TestMethod]
    public async Task BinaryOperator_WithUnhandledThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public int Value { get; set; }

                public static TestClass operator +(TestClass a, TestClass b)
                {
                    if (a == null || b == null)
                        throw new System.ArgumentNullException();

                    return new TestClass { Value = a.Value + b.Value };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task UnaryOperator_WithUnhandledThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public int Value { get; set; }

                public static TestClass operator -(TestClass a)
                {
                    if (a == null)
                        throw new System.ArgumentNullException();

                    return new TestClass { Value = -a.Value };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ConversionOperator_WithUnhandledThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public int Value { get; set; }

                public static explicit operator int(TestClass test)
                {
                    if (test == null)
                        throw new System.ArgumentNullException();

                    return test.Value;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task Operator_WithHandledThrow_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public int Value { get; set; }

                public static TestClass operator +(TestClass a, TestClass b)
                {
                    try
                    {
                        if (a == null || b == null)
                            throw new System.ArgumentNullException();

                        return new TestClass { Value = a.Value + b.Value };
                    }
                    catch (System.Exception)
                    {
                        return new TestClass();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task Operator_WithExpressionBody_WithThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public int Value { get; set; }

                public static TestClass operator +(TestClass a, TestClass b)
                    => throw new System.NotImplementedException();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }
}
