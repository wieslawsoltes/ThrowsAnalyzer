using System;

namespace ExceptionPatterns;

/// <summary>
/// Sample code demonstrating all ThrowsAnalyzer diagnostics and their code fixes.
/// This file intentionally contains code that triggers analyzer warnings.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("ThrowsAnalyzer Sample Project");
        Console.WriteLine("This project demonstrates all diagnostic rules.");
    }

    // THROWS001: Method contains throw statement
    void MethodWithThrow()
    {
        throw new InvalidOperationException("This triggers THROWS001");
    }

    // THROWS002: Unhandled throw statement
    void MethodWithUnhandledThrow()
    {
        Console.WriteLine("Before throw");
        throw new ArgumentNullException("param");
        Console.WriteLine("After throw");
    }

    // THROWS003: Method contains try-catch block
    void MethodWithTryCatch()
    {
        try
        {
            DoSomething();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Caught: {ex.Message}");
        }
    }

    // THROWS004: Rethrow anti-pattern
    void RethrowAntiPattern()
    {
        try
        {
            DoSomething();
        }
        catch (Exception ex)
        {
            // This should be 'throw;' not 'throw ex;'
            throw ex;
        }
    }

    // THROWS007: Unreachable catch clause
    // Note: The code fix will reorder these catches
    void CatchOrderingIssue()
    {
        try
        {
            DoSomething();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("Specific exception");
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine("Another specific exception");
        }
        catch (Exception ex)
        {
            // General catch should come last
            Console.WriteLine("General exception");
        }
    }

    // THROWS008: Empty catch block
    void EmptyCatchBlock()
    {
        try
        {
            DoSomething();
        }
        catch (InvalidOperationException)
        {
            // Empty - swallows exception
        }
    }

    // THROWS009: Catch block only rethrows
    void RethrowOnlyCatch()
    {
        try
        {
            DoSomething();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
    }

    // THROWS010: Overly broad exception catch
    void OverlyBroadCatch()
    {
        try
        {
            DoSomething();
        }
        catch (Exception ex)
        {
            // Catching System.Exception is overly broad
            Console.WriteLine(ex.Message);
        }
    }

    // Multiple issues in one method
    void MultipleIssues()
    {
        try
        {
            throw new InvalidOperationException();
        }
        catch (InvalidOperationException ex)
        {
            throw ex; // THROWS004 - should be 'throw;'
        }
    }

    // Constructor with throw
    public Program()
    {
        throw new InvalidOperationException("Constructor throws");
    }

    // Property with throw
    public int PropertyWithThrow
    {
        get => throw new NotImplementedException();
        set
        {
            throw new NotImplementedException();
        }
    }

    // Operator with throw
    public static Program operator +(Program a, Program b)
    {
        throw new NotImplementedException();
    }

    // Local function with throw
    void MethodWithLocalFunction()
    {
        void LocalFunction()
        {
            throw new InvalidOperationException();
        }

        LocalFunction();
    }

    // Lambda with throw
    void MethodWithLambda()
    {
        Action lambda = () => throw new InvalidOperationException();
        lambda();
    }

    #region Event Handler Exception Patterns (THROWS026)

    // Define a simple event for demonstration
    public event EventHandler DataReceived;
    public event EventHandler ProcessingCompleted;

    // THROWS026: Event handler lambda throws exception without catching it
    // This is dangerous because uncaught exceptions in event handlers can crash the application
    void EventHandlerLambdaUncaught()
    {
        // BAD: Lambda event handler throws without catching
        DataReceived += (sender, e) =>
        {
            throw new InvalidOperationException("Data processing failed");
        };
    }

    // GOOD: Event handler lambda catches exceptions
    void EventHandlerLambdaCaught()
    {
        DataReceived += (sender, e) =>
        {
            try
            {
                ProcessData();
            }
            catch (InvalidOperationException ex)
            {
                // Log error, show message to user, etc.
                Console.WriteLine($"Error: {ex.Message}");
            }
        };
    }

    // THROWS026: Event handler with throw expression
    void EventHandlerWithThrowExpression()
    {
        // BAD: Throw expression in event handler lambda
        DataReceived += (sender, e) => throw new InvalidOperationException();
    }

    // THROWS026: Event handler lambda with rethrow
    void EventHandlerWithRethrow()
    {
        // BAD: Rethrow still escapes event handler
        DataReceived += (sender, e) =>
        {
            try
            {
                ProcessData();
            }
            catch (InvalidOperationException)
            {
                throw; // This still crashes the application
            }
        };
    }

    // Event handler method references
    // Note: Method references are analyzed by THROWS001/THROWS002, not THROWS026
    void EventHandlerMethodReferences()
    {
        // Method reference - the OnDataReceived method will be analyzed separately
        DataReceived += OnDataReceived;

        // Multiple event subscriptions
        ProcessingCompleted += OnProcessingCompleted;
        ProcessingCompleted += OnProcessingCompletedAlternate;
    }

    // BAD: Event handler method that throws (triggers THROWS001/THROWS002)
    private void OnDataReceived(object sender, EventArgs e)
    {
        throw new InvalidOperationException("Processing failed");
    }

    // GOOD: Event handler method that catches exceptions
    private void OnProcessingCompleted(object sender, EventArgs e)
    {
        try
        {
            ProcessData();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    // GOOD: Event handler method that doesn't throw
    private void OnProcessingCompletedAlternate(object sender, EventArgs e)
    {
        Console.WriteLine("Processing completed successfully");
    }

    // Real-world example: Button click handler
    class Button
    {
        public event EventHandler Click;
    }

    void ButtonClickHandlerExample()
    {
        var button = new Button();

        // BAD: Uncaught exception in click handler can crash UI application
        button.Click += (sender, e) =>
        {
            throw new InvalidOperationException("Button action failed");
        };

        // GOOD: Catch and handle exceptions appropriately
        button.Click += (sender, e) =>
        {
            try
            {
                PerformButtonAction();
            }
            catch (InvalidOperationException ex)
            {
                // Show error dialog, log error, etc.
                Console.WriteLine($"Action failed: {ex.Message}");
            }
        };
    }

    // Custom event handler delegate
    public delegate void DataHandler(object sender, EventArgs args);
    public event DataHandler CustomDataEvent;

    void CustomEventHandlerPattern()
    {
        // THROWS026: Custom delegate also triggers the analyzer
        CustomDataEvent += (sender, args) =>
        {
            throw new InvalidOperationException();
        };
    }

    #endregion

    #region Lambda Exception Patterns (THROWS025)

    // THROWS025: Lambda in LINQ throws uncaught exception
    void LinqLambdaUncaught()
    {
        var items = new[] { 1, 2, 3, -1, 5 };

        // BAD: Lambda throws without catching
        var result = items.Where(x =>
        {
            if (x < 0)
                throw new InvalidOperationException("Negative value");
            return x > 1;
        });
    }

    // GOOD: Lambda catches exception
    void LinqLambdaCaught()
    {
        var items = new[] { 1, 2, 3, -1, 5 };

        var result = items.Where(x =>
        {
            try
            {
                if (x < 0)
                    throw new InvalidOperationException("Negative value");
                return x > 1;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        });
    }

    // THROWS025: Throw expression in lambda
    void ThrowExpressionInLambda()
    {
        var items = new[] { 1, 2, 3 };

        // BAD: Throw expression
        var result = items.Select(x => x >= 0 ? x : throw new ArgumentException());
    }

    #endregion

    private void DoSomething()
    {
        // Placeholder method
    }

    private void ProcessData()
    {
        // Placeholder method
    }

    private void PerformButtonAction()
    {
        // Placeholder method
    }
}
