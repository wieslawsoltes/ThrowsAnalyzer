using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThrowsAnalyzer.CodeFixes;

namespace ThrowsAnalyzer.Tests.CodeFixes;

[TestClass]
public class RethrowAntiPatternCodeFixTests
{
    [TestMethod]
    public async Task ThrowEx_ReplacedWithBareRethrow()
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
                        throw ex;
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
                        throw;
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<RethrowAntiPatternAnalyzer, RethrowAntiPatternCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task ThrowEx_WithComment_PreservesComment()
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
                        // Log the error
                        throw ex;
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
                        // Log the error
                        throw;
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<RethrowAntiPatternAnalyzer, RethrowAntiPatternCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task ThrowEx_InConstructor_ReplacedWithBareRethrow()
    {
        var source = """
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

        var expected = """
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
                        throw;
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<RethrowAntiPatternAnalyzer, RethrowAntiPatternCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task ThrowEx_InProperty_ReplacedWithBareRethrow()
    {
        var source = """
            using System;

            class TestClass
            {
                public int Value
                {
                    get
                    {
                        try
                        {
                            return GetValue();
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                }

                int GetValue() => 42;
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                public int Value
                {
                    get
                    {
                        try
                        {
                            return GetValue();
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                    }
                }

                int GetValue() => 42;
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<RethrowAntiPatternAnalyzer, RethrowAntiPatternCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task ThrowEx_InLambda_ReplacedWithBareRethrow()
    {
        var source = """
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

        var expected = """
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
                            throw;
                        }
                    };
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<RethrowAntiPatternAnalyzer, RethrowAntiPatternCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task ThrowEx_MultipleInSameMethod_FixesFirstOccurrence()
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
                        throw;
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw ex;
                    }
                }

                void DoSomething() { }
            }
            """;

        // Fixes the first occurrence (user can apply "Fix All" in IDE to fix all occurrences)
        await CodeFixTestHelper.VerifyCodeFixAsync<RethrowAntiPatternAnalyzer, RethrowAntiPatternCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task ThrowEx_WithLeadingTrivia_PreservesTrivia()
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
                        /* Important comment */
                        throw ex;
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
                        /* Important comment */
                        throw;
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<RethrowAntiPatternAnalyzer, RethrowAntiPatternCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task ThrowEx_NestedCatch_ReplacedWithBareRethrow()
    {
        var source = """
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

        var expected = """
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
                            throw;
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

        await CodeFixTestHelper.VerifyCodeFixAsync<RethrowAntiPatternAnalyzer, RethrowAntiPatternCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task BareRethrow_NoCodeFixOffered()
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
                        throw;
                    }
                }

                void DoSomething() { }
            }
            """;

        // Should not offer a code fix for already correct code
        var fixCount = await CodeFixTestHelper.GetCodeFixCountAsync<RethrowAntiPatternAnalyzer, RethrowAntiPatternCodeFixProvider>(source);
        Assert.AreEqual(0, fixCount, "Should not offer code fix for bare rethrow");
    }
}
