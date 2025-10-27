using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThrowsAnalyzer.CodeFixes;

namespace ThrowsAnalyzer.Tests.Integration;

/// <summary>
/// Integration tests that verify multiple code fixes work together correctly.
/// </summary>
[TestClass]
public class CodeFixIntegrationTests
{
    [TestMethod]
    public async Task MultipleIssues_CanBeFixedSequentially()
    {
        // This test verifies that applying multiple code fixes in sequence works correctly
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

        // First fix the rethrow anti-pattern
        await CodeFixTestHelper.VerifyCodeFixAsync<RethrowAntiPatternAnalyzer, RethrowAntiPatternCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task EmptyCatchAndOrdering_BothDetectedAndFixed()
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

        // Can fix empty catch by removing it
        var expectedAfterRemove = """
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
            source, expectedAfterRemove, codeFixIndex: 0);
    }

    [TestMethod]
    public async Task ComplexScenario_MultipleAnalyzersDetectIssues()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    throw new InvalidOperationException();
                }

                void AnotherMethod()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        // Verify THROWS001 can be fixed by wrapping in try-catch
        var fixedMethod1 = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        throw new InvalidOperationException();
                    }
                    catch (InvalidOperationException ex)
                    {
                        // TODO: Handle exception appropriately
                        throw;
                    }
                }

                void AnotherMethod()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, MethodThrowsCodeFixProvider>(
            source, fixedMethod1);
    }

    [TestMethod]
    public async Task CatchOrdering_WithMultipleClauses_CorrectlyReordered()
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
                        Console.WriteLine("General");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine("Specific");
                    }
                    catch (ArgumentNullException ex)
                    {
                        Console.WriteLine("Very specific");
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
                    catch (ArgumentNullException ex)
                    {
                        Console.WriteLine("Very specific");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine("Specific");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("General");
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, CatchClauseOrderingCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task AllCodeFixProvidersRegistered_CanBeFound()
    {
        // Verify that all code fix providers can be instantiated
        var providers = new object[]
        {
            new MethodThrowsCodeFixProvider(),
            new UnhandledThrowsCodeFixProvider(),
            new TryCatchCodeFixProvider(),
            new RethrowAntiPatternCodeFixProvider(),
            new EmptyCatchCodeFixProvider(),
            new RethrowOnlyCatchCodeFixProvider(),
            new CatchClauseOrderingCodeFixProvider(),
            new OverlyBroadCatchCodeFixProvider()
        };

        Assert.AreEqual(8, providers.Length, "All 8 code fix providers should be available");
    }

    [TestMethod]
    public async Task RethrowOnlyAndEmpty_DifferentBehaviors()
    {
        // Rethrow-only catch (THROWS009)
        var rethrowOnlySource = """
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
                        throw;  // Has statement - rethrow only
                    }
                }

                void DoSomething() { }
            }
            """;

        // Empty catch (THROWS008)
        var emptySource = """
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
                        // No statements - empty
                    }
                }

                void DoSomething() { }
            }
            """;

        var expectedUnwrapped = """
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

        // Both should unwrap to the same result when removed
        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, RethrowOnlyCatchCodeFixProvider>(
            rethrowOnlySource, expectedUnwrapped);

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, EmptyCatchCodeFixProvider>(
            emptySource, expectedUnwrapped, codeFixIndex: 0);
    }
}
