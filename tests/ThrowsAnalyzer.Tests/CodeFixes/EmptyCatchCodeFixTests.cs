using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThrowsAnalyzer.CodeFixes;

namespace ThrowsAnalyzer.Tests.CodeFixes;

[TestClass]
public class EmptyCatchCodeFixTests
{
    [TestMethod]
    public async Task EmptyCatch_Removed()
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
                    catch (InvalidOperationException)
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
                    DoSomething();
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, EmptyCatchCodeFixProvider>(
            source, expected, codeFixIndex: 0);
    }

    [TestMethod]
    public async Task EmptyCatch_LoggingAdded()
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
                    catch (InvalidOperationException ex)
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
                    catch (InvalidOperationException ex)
                    {
                        // TODO: Replace with proper logging
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, EmptyCatchCodeFixProvider>(
            source, expected, codeFixIndex: 1);
    }

    [TestMethod]
    public async Task EmptyCatchWithoutVariable_LoggingAddedWithDeclaration()
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
                    catch (InvalidOperationException)
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
                    catch (InvalidOperationException ex)
                    {
                        // TODO: Replace with proper logging
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, EmptyCatchCodeFixProvider>(
            source, expected, codeFixIndex: 1);
    }

    [TestMethod]
    public async Task EmptyCatch_WithOtherCatches_OnlyTargetRemoved()
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
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(ex);
                    }
                    catch (InvalidOperationException)
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
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, EmptyCatchCodeFixProvider>(
            source, expected, codeFixIndex: 0);
    }

    [TestMethod]
    public async Task EmptyCatch_WithFinally_CatchRemovedFinallyKept()
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
                    catch (InvalidOperationException)
                    {
                    }
                    finally
                    {
                        Cleanup();
                    }
                }

                void DoSomething() { }
                void Cleanup() { }
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
                    finally
                    {
                        Cleanup();
                    }
                }

                void DoSomething() { }
                void Cleanup() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, EmptyCatchCodeFixProvider>(
            source, expected, codeFixIndex: 0);
    }

    [TestMethod]
    public async Task TwoCodeFixesOffered_ForEmptyCatch()
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

        var fixCount = await CodeFixTestHelper.GetCodeFixCountAsync<CatchClauseOrderingAnalyzer, EmptyCatchCodeFixProvider>(source);
        Assert.AreEqual(2, fixCount, "Should offer both remove and add logging");
    }
}
