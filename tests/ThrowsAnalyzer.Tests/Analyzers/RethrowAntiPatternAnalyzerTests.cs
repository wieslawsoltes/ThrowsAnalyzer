using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers;

[TestClass]
public class RethrowAntiPatternAnalyzerTests
{
    [TestMethod]
    public async Task ThrowExInCatch_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<RethrowAntiPatternAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS004", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("Method"));
    }

    [TestMethod]
    public async Task BareRethrow_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<RethrowAntiPatternAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task ThrowDifferentVariable_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    var otherEx = new InvalidOperationException();
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex)
                    {
                        throw otherEx;
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<RethrowAntiPatternAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task ThrowNewException_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Error", ex);
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<RethrowAntiPatternAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task ThrowExOutsideCatch_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    var ex = new Exception();
                    throw ex;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<RethrowAntiPatternAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task ThrowExInNestedCatch_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        try
                        {
                            DoSomething();
                        }
                        catch (InvalidOperationException ex)
                        {
                            throw ex;
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<RethrowAntiPatternAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS004", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ThrowExInConstructor_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                public TestClass()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<RethrowAntiPatternAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS004", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("Constructor"));
    }

    [TestMethod]
    public async Task ThrowExInProperty_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                public int Value
                {
                    get
                    {
                        try
                        {
                            return DoSomething();
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                }

                int DoSomething() => 42;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<RethrowAntiPatternAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS004", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ThrowExInLambda_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    Action a = () =>
                    {
                        try
                        {
                            DoSomething();
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    };
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<RethrowAntiPatternAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS004", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task MultipleCatchesWithThrowEx_ShouldReportMultipleDiagnostics()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (ArgumentException ex)
                    {
                        throw ex;
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw ex;
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<RethrowAntiPatternAnalyzer>(testCode);

        Assert.AreEqual(2, diagnostics.Length);
        Assert.IsTrue(diagnostics.All(d => d.Id == "THROWS004"));
    }

    [TestMethod]
    public async Task CatchWithoutDeclaration_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<RethrowAntiPatternAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }
}
