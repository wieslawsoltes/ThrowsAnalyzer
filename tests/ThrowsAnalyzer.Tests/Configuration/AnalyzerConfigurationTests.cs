using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Configuration;

[TestClass]
public class AnalyzerConfigurationTests
{
    private const string TestCodeWithThrow = """
        class TestClass
        {
            public void Method()
            {
                throw new System.InvalidOperationException();
            }
        }
        """;

    private const string TestCodeWithUnhandledThrow = """
        class TestClass
        {
            public void Method()
            {
                throw new System.InvalidOperationException();
            }
        }
        """;

    private const string TestCodeWithTryCatch = """
        class TestClass
        {
            public void Method()
            {
                try
                {
                    System.Console.WriteLine("test");
                }
                catch (System.Exception)
                {
                }
            }
        }
        """;

    #region Analyzer Enable/Disable Tests

    [TestMethod]
    public async Task ThrowStatementAnalyzer_WhenEnabled_ShouldReportDiagnostic()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_enable_throw_statement"] = "true"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            TestCodeWithThrow, config);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ThrowStatementAnalyzer_WhenDisabled_ShouldNotReportDiagnostic()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_enable_throw_statement"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            TestCodeWithThrow, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task UnhandledThrowAnalyzer_WhenEnabled_ShouldReportDiagnostic()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_enable_unhandled_throw"] = "true"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(
            TestCodeWithUnhandledThrow, config);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task UnhandledThrowAnalyzer_WhenDisabled_ShouldNotReportDiagnostic()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_enable_unhandled_throw"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(
            TestCodeWithUnhandledThrow, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task TryCatchAnalyzer_WhenEnabled_ShouldReportDiagnostic()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_enable_try_catch"] = "true"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(
            TestCodeWithTryCatch, config);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task TryCatchAnalyzer_WhenDisabled_ShouldNotReportDiagnostic()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_enable_try_catch"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(
            TestCodeWithTryCatch, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    #endregion

    #region Member Type Configuration Tests

    [TestMethod]
    public async Task ThrowStatementAnalyzer_MethodsDisabled_ShouldNotReportForMethods()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_analyze_methods"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            TestCodeWithThrow, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task ThrowStatementAnalyzer_MethodsEnabled_ShouldReportForMethods()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_analyze_methods"] = "true"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            TestCodeWithThrow, config);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ThrowStatementAnalyzer_ConstructorsDisabled_ShouldNotReportForConstructors()
    {
        var testCode = """
            class TestClass
            {
                public TestClass()
                {
                    throw new System.InvalidOperationException();
                }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_analyze_constructors"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            testCode, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task ThrowStatementAnalyzer_LambdasDisabled_ShouldNotReportForLambdas()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    System.Action action = () => throw new System.InvalidOperationException();
                }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_analyze_lambdas"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            testCode, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task ThrowStatementAnalyzer_LocalFunctionsDisabled_ShouldNotReportForLocalFunctions()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    void LocalFunction()
                    {
                        throw new System.InvalidOperationException();
                    }
                }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_analyze_local_functions"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            testCode, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task ThrowStatementAnalyzer_PropertiesDisabled_ShouldNotReportForProperties()
    {
        var testCode = """
            class TestClass
            {
                public int Value => throw new System.NotImplementedException();
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_analyze_properties"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            testCode, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task ThrowStatementAnalyzer_AccessorsDisabled_ShouldNotReportForAccessors()
    {
        var testCode = """
            class TestClass
            {
                private int _value;
                public int Value
                {
                    get { throw new System.NotImplementedException(); }
                    set { _value = value; }
                }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_analyze_accessors"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            testCode, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    #endregion

    #region Combined Configuration Tests

    [TestMethod]
    public async Task AnalyzerDisabled_OverridesMemberTypeConfiguration()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_enable_throw_statement"] = "false",
            ["throws_analyzer_analyze_methods"] = "true"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            TestCodeWithThrow, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task BothAnalyzerAndMemberTypeDisabled_ShouldNotReport()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_enable_throw_statement"] = "false",
            ["throws_analyzer_analyze_methods"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            TestCodeWithThrow, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task AnalyzerEnabled_MemberTypeDisabled_ShouldNotReport()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_enable_throw_statement"] = "true",
            ["throws_analyzer_analyze_methods"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            TestCodeWithThrow, config);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task BothAnalyzerAndMemberTypeEnabled_ShouldReport()
    {
        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_enable_throw_statement"] = "true",
            ["throws_analyzer_analyze_methods"] = "true"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            TestCodeWithThrow, config);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    #endregion

    #region Multiple Member Type Tests

    [TestMethod]
    public async Task MultipleAnalyzers_SelectivelyDisabled_ShouldRespectConfiguration()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    try
                    {
                        throw new System.InvalidOperationException();
                    }
                    catch (System.Exception)
                    {
                    }
                }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_enable_throw_statement"] = "false",
            ["throws_analyzer_enable_unhandled_throw"] = "false",
            ["throws_analyzer_enable_try_catch"] = "true"
        };

        // Should only get THROWS003 (try-catch)
        var tryCatchDiagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(
            testCode, config);
        Assert.AreEqual(1, tryCatchDiagnostics.Length);
        Assert.AreEqual("THROWS003", tryCatchDiagnostics[0].Id);

        // Should not get THROWS001
        var throwDiagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            testCode, config);
        Assert.AreEqual(0, throwDiagnostics.Length);

        // Should not get THROWS002
        var unhandledDiagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(
            testCode, config);
        Assert.AreEqual(0, unhandledDiagnostics.Length);
    }

    [TestMethod]
    public async Task MultipleMemberTypes_SelectivelyEnabled_ShouldRespectConfiguration()
    {
        var testCode = """
            class TestClass
            {
                public void Method()
                {
                    throw new System.InvalidOperationException();
                }

                public TestClass()
                {
                    throw new System.InvalidOperationException();
                }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["throws_analyzer_analyze_methods"] = "true",
            ["throws_analyzer_analyze_constructors"] = "false"
        };

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            testCode, config);

        // Should only report for method, not constructor
        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    #endregion

    #region Default Behavior Tests

    [TestMethod]
    public async Task NoConfiguration_ShouldReportDiagnostic()
    {
        // Test that analyzer is enabled by default when no config is provided
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            TestCodeWithThrow, null);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task EmptyConfiguration_ShouldReportDiagnostic()
    {
        // Test that analyzer is enabled by default with empty config
        var config = new Dictionary<string, string>();

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(
            TestCodeWithThrow, config);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    #endregion
}
