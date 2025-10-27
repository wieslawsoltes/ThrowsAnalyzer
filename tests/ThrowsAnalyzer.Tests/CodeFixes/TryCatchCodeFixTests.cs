using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThrowsAnalyzer.CodeFixes;

namespace ThrowsAnalyzer.Tests.CodeFixes;

[TestClass]
public class TryCatchCodeFixTests
{
    [TestMethod]
    public async Task SimpleTryCatch_RemovedAndUnwrapped()
    {
        var source = """
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
                        // Handle error
                    }
                }

                void DoSomething() { }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                void Method()
                {
                    DoSomething();
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<TryCatchAnalyzer, TryCatchCodeFixProvider>(
            source, expected, codeFixIndex: 0);
    }

    [TestMethod]
    public async Task TryCatchWithMultipleStatements_RemovedAndUnwrapped()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                        DoSomethingElse();
                    }
                    catch (Exception)
                    {
                    }
                }

                void DoSomething() { }
                void DoSomethingElse() { }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                void Method()
                {
                    {
                        DoSomething();
                        DoSomethingElse();
                    }
                }

                void DoSomething() { }
                void DoSomethingElse() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<TryCatchAnalyzer, TryCatchCodeFixProvider>(
            source, expected, codeFixIndex: 0);
    }

    [TestMethod]
    public async Task EmptyCatch_AddLogging()
    {
        var source = """
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
                    }
                }

                void DoSomething() { }
            }
            """;

        var expected = """
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
                        // TODO: Replace with proper logging
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<TryCatchAnalyzer, TryCatchCodeFixProvider>(
            source, expected, codeFixIndex: 1);
    }

    [TestMethod]
    public async Task EmptyCatchWithVariable_AddLogging()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (InvalidOperationException ioe)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (InvalidOperationException ioe)
                    {
                        // TODO: Replace with proper logging
                        Console.WriteLine($"Error: {ioe.Message}");
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<TryCatchAnalyzer, TryCatchCodeFixProvider>(
            source, expected, codeFixIndex: 1);
    }

    [TestMethod]
    public async Task TryCatchWithNonEmptyCatch_OnlyOffersRemove()
    {
        var source = """
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
                        Console.WriteLine(ex.Message);
                    }
                }

                void DoSomething() { }
            }
            """;

        // Should only offer 1 fix (remove), not add logging
        var fixCount = await CodeFixTestHelper.GetCodeFixCountAsync<TryCatchAnalyzer, TryCatchCodeFixProvider>(source);
        Assert.AreEqual(1, fixCount, "Should only offer remove fix for non-empty catch");
    }

    [TestMethod]
    public async Task TryCatchInConstructor_Removed()
    {
        var source = """
            using System;

            class TestClass
            {
                public TestClass()
                {
                    try
                    {
                        Initialize();
                    }
                    catch
                    {
                    }
                }

                void Initialize() { }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                public TestClass()
                {
                    Initialize();
                }

                void Initialize() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<TryCatchAnalyzer, TryCatchCodeFixProvider>(
            source, expected, codeFixIndex: 0);
    }

    [TestMethod]
    public async Task TwoCodeFixesOffered_WhenEmptyCatch()
    {
        var source = """
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
                    }
                }

                void DoSomething() { }
            }
            """;

        var fixCount = await CodeFixTestHelper.GetCodeFixCountAsync<TryCatchAnalyzer, TryCatchCodeFixProvider>(source);
        Assert.AreEqual(2, fixCount, "Should offer both remove and add logging for empty catch");
    }
}
