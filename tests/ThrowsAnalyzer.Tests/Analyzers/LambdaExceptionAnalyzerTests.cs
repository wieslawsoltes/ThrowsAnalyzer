using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers;

[TestClass]
public class LambdaExceptionAnalyzerTests
{
    #region THROWS025 Tests (Lambda Uncaught Exception)

    [TestMethod]
    public async Task THROWS025_LambdaThrowsUncaught_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Where(x =>
                    {
                        if (x < 0)
                            throw new InvalidOperationException();
                        return x > 1;
                    });
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<LambdaUncaughtExceptionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Length >= 1);
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS025"));
    }

    [TestMethod]
    public async Task THROWS025_LambdaThrowsCaught_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Where(x =>
                    {
                        try
                        {
                            if (x < 0)
                                throw new InvalidOperationException();
                            return x > 1;
                        }
                        catch (InvalidOperationException)
                        {
                            return false;
                        }
                    });
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<LambdaUncaughtExceptionAnalyzer>(testCode);

        var throws025 = diagnostics.Where(d => d.Id == "THROWS025").ToArray();
        Assert.AreEqual(0, throws025.Length);
    }

    [TestMethod]
    public async Task THROWS025_LambdaNoThrow_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Where(x => x > 1);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<LambdaUncaughtExceptionAnalyzer>(testCode);

        var throws025 = diagnostics.Where(d => d.Id == "THROWS025").ToArray();
        Assert.AreEqual(0, throws025.Length);
    }

    [TestMethod]
    public async Task THROWS025_ThrowExpressionInLambda_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Select(x => x >= 0 ? x : throw new ArgumentException());
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<LambdaUncaughtExceptionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS025"));
    }

    [TestMethod]
    public async Task THROWS025_EventHandlerLambda_ShouldNotReport()
    {
        var testCode = """
            using System;

            class TestClass
            {
                event EventHandler MyEvent;

                void Method()
                {
                    MyEvent += (sender, e) =>
                    {
                        throw new InvalidOperationException();
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<LambdaUncaughtExceptionAnalyzer>(testCode);

        // Event handlers are covered by THROWS026, not THROWS025
        var throws025 = diagnostics.Where(d => d.Id == "THROWS025").ToArray();
        Assert.AreEqual(0, throws025.Length);
    }

    [TestMethod]
    public async Task THROWS025_MultipleLambdasWithThrows_ShouldReportMultiple()
    {
        var testCode = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result1 = items.Where(x =>
                    {
                        throw new InvalidOperationException();
                    });
                    var result2 = items.Select(x =>
                    {
                        throw new ArgumentException();
                    });
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<LambdaUncaughtExceptionAnalyzer>(testCode);

        var throws025 = diagnostics.Where(d => d.Id == "THROWS025").ToArray();
        Assert.AreEqual(2, throws025.Length);
    }

    #endregion

    #region THROWS026 Tests (Event Handler Lambda Exception)

    [TestMethod]
    public async Task THROWS026_EventHandlerLambdaThrows_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                event EventHandler MyEvent;

                void Method()
                {
                    MyEvent += (sender, e) =>
                    {
                        throw new InvalidOperationException();
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<EventHandlerLambdaExceptionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Length >= 1);
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS026"));
    }

    [TestMethod]
    public async Task THROWS026_EventHandlerLambdaCaught_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                event EventHandler MyEvent;

                void Method()
                {
                    MyEvent += (sender, e) =>
                    {
                        try
                        {
                            throw new InvalidOperationException();
                        }
                        catch (InvalidOperationException)
                        {
                            // Handled
                        }
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<EventHandlerLambdaExceptionAnalyzer>(testCode);

        var throws026 = diagnostics.Where(d => d.Id == "THROWS026").ToArray();
        Assert.AreEqual(0, throws026.Length);
    }

    [TestMethod]
    public async Task THROWS026_NonEventHandlerLambda_ShouldNotReport()
    {
        var testCode = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Where(x =>
                    {
                        throw new InvalidOperationException();
                    });
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<EventHandlerLambdaExceptionAnalyzer>(testCode);

        // Non-event handlers are covered by THROWS025, not THROWS026
        var throws026 = diagnostics.Where(d => d.Id == "THROWS026").ToArray();
        Assert.AreEqual(0, throws026.Length);
    }

    [TestMethod]
    public async Task THROWS026_EventHandlerNoThrow_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                event EventHandler MyEvent;

                void Method()
                {
                    MyEvent += (sender, e) =>
                    {
                        Console.WriteLine("No throw");
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<EventHandlerLambdaExceptionAnalyzer>(testCode);

        var throws026 = diagnostics.Where(d => d.Id == "THROWS026").ToArray();
        Assert.AreEqual(0, throws026.Length);
    }

    [TestMethod]
    public async Task THROWS026_MultipleEventHandlersWithThrows_ShouldReportMultiple()
    {
        var testCode = """
            using System;

            class TestClass
            {
                event EventHandler Event1;
                event EventHandler Event2;

                void Method()
                {
                    Event1 += (sender, e) =>
                    {
                        throw new InvalidOperationException();
                    };

                    Event2 += (sender, e) =>
                    {
                        throw new ArgumentException();
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<EventHandlerLambdaExceptionAnalyzer>(testCode);

        var throws026 = diagnostics.Where(d => d.Id == "THROWS026").ToArray();
        Assert.AreEqual(2, throws026.Length);
    }

    [TestMethod]
    public async Task THROWS026_EventHandlerRethrow_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                event EventHandler MyEvent;

                void Method()
                {
                    MyEvent += (sender, e) =>
                    {
                        try
                        {
                            throw new InvalidOperationException();
                        }
                        catch (InvalidOperationException)
                        {
                            throw; // Rethrow
                        }
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<EventHandlerLambdaExceptionAnalyzer>(testCode);

        // Rethrown exception still escapes event handler
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS026"));
    }

    [TestMethod]
    public async Task THROWS026_CustomEventHandlerDelegate_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                public delegate void CustomEventHandler(object sender, EventArgs e);
                event CustomEventHandler MyEvent;

                void Method()
                {
                    MyEvent += (sender, e) =>
                    {
                        throw new InvalidOperationException();
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<EventHandlerLambdaExceptionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS026"));
    }

    [TestMethod]
    public async Task THROWS026_EventHandlerWithEventArgsSignature_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                public delegate void DataHandler(object sender, EventArgs args);
                event DataHandler DataReceived;

                void Method()
                {
                    DataReceived += (sender, args) =>
                    {
                        throw new InvalidOperationException("Data processing failed");
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<EventHandlerLambdaExceptionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS026"));
    }

    [TestMethod]
    public async Task THROWS026_ButtonClickHandler_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class Button
            {
                public event EventHandler Click;
            }

            class TestClass
            {
                void Method()
                {
                    var button = new Button();
                    button.Click += (sender, e) =>
                    {
                        throw new InvalidOperationException("Button click failed");
                    };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<EventHandlerLambdaExceptionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS026"));
    }

    [TestMethod]
    public async Task THROWS026_EventHandlerLambdaWithThrowExpression_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                event EventHandler MyEvent;

                void Method()
                {
                    MyEvent += (sender, e) => throw new InvalidOperationException();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<EventHandlerLambdaExceptionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS026"));
    }

    [TestMethod]
    public async Task THROWS026_EventHandlerMethodReference_NotCoveredByTHROWS026()
    {
        // Note: Event handler method references are covered by THROWS001 (method contains throw)
        // and THROWS019 (undocumented exception in public API), not THROWS026.
        // THROWS026 is specifically for lambdas.
        var testCode = """
            using System;

            class TestClass
            {
                event EventHandler MyEvent;

                void Method()
                {
                    MyEvent += OnMyEvent; // Method reference, not lambda
                }

                private void OnMyEvent(object sender, EventArgs e)
                {
                    throw new InvalidOperationException(); // This is covered by THROWS001
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<EventHandlerLambdaExceptionAnalyzer>(testCode);

        // Method references are not lambdas, so THROWS026 doesn't apply
        var throws026 = diagnostics.Where(d => d.Id == "THROWS026").ToArray();
        Assert.AreEqual(0, throws026.Length);

        // But other analyzers will catch the throw in OnMyEvent method
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS001" || d.Id == "THROWS002"));
    }

    #endregion
}
