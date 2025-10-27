# ThrowsAnalyzer Report: ExceptionPatterns

**Analysis Date:** 2025-10-27 13:30:21
**Target:** Project - `/Users/wieslawsoltes/GitHub/ThrowsAnalyzer/samples/ExceptionPatterns/ExceptionPatterns.csproj`
**Configuration:** Debug
**Duration:** 2.31s
**Status:** ✓ Success

## Summary

| Metric | Count |
|--------|------:|
| Total Diagnostics | 69 |
| Errors | 0 |
| Warnings | 69 |
| Info/Hidden | 0 |
| Projects Analyzed | 1 |
| Files Analyzed | 1 |

## Diagnostics by ID

| Diagnostic ID | Count | Percentage |
|--------------|------:|-----------:|
| THROWS001 | 20 | 29.0% |
| THROWS002 | 19 | 27.5% |
| THROWS003 | 12 | 17.4% |
| THROWS010 | 5 | 7.2% |
| THROWS026 | 3 | 4.3% |
| THROWS009 | 2 | 2.9% |
| THROWS027 | 2 | 2.9% |
| THROWS004 | 2 | 2.9% |
| THROWS025 | 2 | 2.9% |
| THROWS008 | 1 | 1.4% |
| THROWS017 | 1 | 1.4% |

## Diagnostics by Severity

| Severity | Count | Percentage |
|----------|------:|-----------:|
| Warning | 69 | 100.0% |

## Top 20 Files with Most Diagnostics

| File | Count |
|------|------:|
| `Program.cs` | 69 |

## Detailed Diagnostics

### Program.cs

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 18, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'MethodWithThrow'' contains throw statement(s)

**Code:**

```csharp
      15:     }
      16: 
      17:     // THROWS001: Method contains throw statement
>>>   18:     void MethodWithThrow()
      19:     {
      20:         throw new InvalidOperationException("This triggers THROWS001");
      21:     }
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 18, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'MethodWithThrow'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
      15:     }
      16: 
      17:     // THROWS001: Method contains throw statement
>>>   18:     void MethodWithThrow()
      19:     {
      20:         throw new InvalidOperationException("This triggers THROWS001");
      21:     }
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 24, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'MethodWithUnhandledThrow'' contains throw statement(s)

**Code:**

```csharp
      21:     }
      22: 
      23:     // THROWS002: Unhandled throw statement
>>>   24:     void MethodWithUnhandledThrow()
      25:     {
      26:         Console.WriteLine("Before throw");
      27:         throw new ArgumentNullException("param");
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 24, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'MethodWithUnhandledThrow'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
      21:     }
      22: 
      23:     // THROWS002: Unhandled throw statement
>>>   24:     void MethodWithUnhandledThrow()
      25:     {
      26:         Console.WriteLine("Before throw");
      27:         throw new ArgumentNullException("param");
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 32, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'MethodWithTryCatch'' contains try/catch block(s)

**Code:**

```csharp
      29:     }
      30: 
      31:     // THROWS003: Method contains try-catch block
>>>   32:     void MethodWithTryCatch()
      33:     {
      34:         try
      35:         {
```

---

#### THROWS010: Overly broad exception catch

**Severity:** Warning  
**Location:** Line 38, Column 9  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'MethodWithTryCatch'' catches 'System.Exception' which is too broad - consider catching specific exception types

**Code:**

```csharp
      35:         {
      36:             DoSomething();
      37:         }
>>>   38:         catch (Exception ex)
      39:         {
      40:             Console.WriteLine($"Caught: {ex.Message}");
      41:         }
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 45, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'RethrowAntiPattern'' contains throw statement(s)

**Code:**

```csharp
      42:     }
      43: 
      44:     // THROWS004: Rethrow anti-pattern
>>>   45:     void RethrowAntiPattern()
      46:     {
      47:         try
      48:         {
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 45, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'RethrowAntiPattern'' contains try/catch block(s)

**Code:**

```csharp
      42:     }
      43: 
      44:     // THROWS004: Rethrow anti-pattern
>>>   45:     void RethrowAntiPattern()
      46:     {
      47:         try
      48:         {
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 45, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'RethrowAntiPattern'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
      42:     }
      43: 
      44:     // THROWS004: Rethrow anti-pattern
>>>   45:     void RethrowAntiPattern()
      46:     {
      47:         try
      48:         {
```

---

#### THROWS010: Overly broad exception catch

**Severity:** Warning  
**Location:** Line 51, Column 9  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'RethrowAntiPattern'' catches 'System.Exception' which is too broad - consider catching specific exception types

**Code:**

```csharp
      48:         {
      49:             DoSomething();
      50:         }
>>>   51:         catch (Exception ex)
      52:         {
      53:             // This should be 'throw;' not 'throw ex;'
      54:             throw ex;
```

---

#### THROWS004: Rethrow modifies stack trace

**Severity:** Warning  
**Location:** Line 54, Column 13  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'RethrowAntiPattern'' rethrows exception with 'throw ex;' which modifies stack trace - use 'throw;' instead

**Code:**

```csharp
      51:         catch (Exception ex)
      52:         {
      53:             // This should be 'throw;' not 'throw ex;'
>>>   54:             throw ex;
      55:         }
      56:     }
      57: 
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 60, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'CatchOrderingIssue'' contains try/catch block(s)

**Code:**

```csharp
      57: 
      58:     // THROWS007: Unreachable catch clause
      59:     // Note: The code fix will reorder these catches
>>>   60:     void CatchOrderingIssue()
      61:     {
      62:         try
      63:         {
```

---

#### THROWS010: Overly broad exception catch

**Severity:** Warning  
**Location:** Line 74, Column 9  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'CatchOrderingIssue'' catches 'System.Exception' which is too broad - consider catching specific exception types

**Code:**

```csharp
      71:         {
      72:             Console.WriteLine("Another specific exception");
      73:         }
>>>   74:         catch (Exception ex)
      75:         {
      76:             // General catch should come last
      77:             Console.WriteLine("General exception");
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 82, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'EmptyCatchBlock'' contains try/catch block(s)

**Code:**

```csharp
      79:     }
      80: 
      81:     // THROWS008: Empty catch block
>>>   82:     void EmptyCatchBlock()
      83:     {
      84:         try
      85:         {
```

---

#### THROWS008: Empty catch block swallows exceptions

**Severity:** Warning  
**Location:** Line 88, Column 9  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'EmptyCatchBlock'' has empty catch block that swallows exceptions

**Code:**

```csharp
      85:         {
      86:             DoSomething();
      87:         }
>>>   88:         catch (InvalidOperationException)
      89:         {
      90:             // Empty - swallows exception
      91:         }
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 95, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'RethrowOnlyCatch'' contains throw statement(s)

**Code:**

```csharp
      92:     }
      93: 
      94:     // THROWS009: Catch block only rethrows
>>>   95:     void RethrowOnlyCatch()
      96:     {
      97:         try
      98:         {
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 95, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'RethrowOnlyCatch'' contains try/catch block(s)

**Code:**

```csharp
      92:     }
      93: 
      94:     // THROWS009: Catch block only rethrows
>>>   95:     void RethrowOnlyCatch()
      96:     {
      97:         try
      98:         {
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 95, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'RethrowOnlyCatch'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
      92:     }
      93: 
      94:     // THROWS009: Catch block only rethrows
>>>   95:     void RethrowOnlyCatch()
      96:     {
      97:         try
      98:         {
```

---

#### THROWS009: Catch block only rethrows exception

**Severity:** Warning  
**Location:** Line 101, Column 9  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'RethrowOnlyCatch'' has catch block that only rethrows - consider removing unnecessary catch

**Code:**

```csharp
      98:         {
      99:             DoSomething();
     100:         }
>>>  101:         catch (InvalidOperationException)
     102:         {
     103:             throw;
     104:         }
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 108, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'OverlyBroadCatch'' contains try/catch block(s)

**Code:**

```csharp
     105:     }
     106: 
     107:     // THROWS010: Overly broad exception catch
>>>  108:     void OverlyBroadCatch()
     109:     {
     110:         try
     111:         {
```

---

#### THROWS010: Overly broad exception catch

**Severity:** Warning  
**Location:** Line 114, Column 9  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'OverlyBroadCatch'' catches 'System.Exception' which is too broad - consider catching specific exception types

**Code:**

```csharp
     111:         {
     112:             DoSomething();
     113:         }
>>>  114:         catch (Exception ex)
     115:         {
     116:             // Catching System.Exception is overly broad
     117:             Console.WriteLine(ex.Message);
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 122, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'MultipleIssues'' contains throw statement(s)

**Code:**

```csharp
     119:     }
     120: 
     121:     // Multiple issues in one method
>>>  122:     void MultipleIssues()
     123:     {
     124:         try
     125:         {
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 122, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'MultipleIssues'' contains try/catch block(s)

**Code:**

```csharp
     119:     }
     120: 
     121:     // Multiple issues in one method
>>>  122:     void MultipleIssues()
     123:     {
     124:         try
     125:         {
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 122, Column 10  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'MultipleIssues'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     119:     }
     120: 
     121:     // Multiple issues in one method
>>>  122:     void MultipleIssues()
     123:     {
     124:         try
     125:         {
```

---

#### THROWS027: Exception used for control flow

**Severity:** Warning  
**Location:** Line 126, Column 13  
**Project:** ExceptionPatterns

**Message:** Exception 'System.InvalidOperationException' is thrown and caught in the same method - consider using return values instead

**Code:**

```csharp
     123:     {
     124:         try
     125:         {
>>>  126:             throw new InvalidOperationException();
     127:         }
     128:         catch (InvalidOperationException ex)
     129:         {
```

---

#### THROWS004: Rethrow modifies stack trace

**Severity:** Warning  
**Location:** Line 130, Column 13  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'MultipleIssues'' rethrows exception with 'throw ex;' which modifies stack trace - use 'throw;' instead

**Code:**

```csharp
     127:         }
     128:         catch (InvalidOperationException ex)
     129:         {
>>>  130:             throw ex; // THROWS004 - should be 'throw;'
     131:         }
     132:     }
     133: 
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 135, Column 12  
**Project:** ExceptionPatterns

**Message:** Method 'Constructor 'Program'' contains throw statement(s)

**Code:**

```csharp
     132:     }
     133: 
     134:     // Constructor with throw
>>>  135:     public Program()
     136:     {
     137:         throw new InvalidOperationException("Constructor throws");
     138:     }
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 135, Column 12  
**Project:** ExceptionPatterns

**Message:** Method 'Constructor 'Program'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     132:     }
     133: 
     134:     // Constructor with throw
>>>  135:     public Program()
     136:     {
     137:         throw new InvalidOperationException("Constructor throws");
     138:     }
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 143, Column 9  
**Project:** ExceptionPatterns

**Message:** Method 'Property 'PropertyWithThrow' getter' contains throw statement(s)

**Code:**

```csharp
     140:     // Property with throw
     141:     public int PropertyWithThrow
     142:     {
>>>  143:         get => throw new NotImplementedException();
     144:         set
     145:         {
     146:             throw new NotImplementedException();
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 143, Column 9  
**Project:** ExceptionPatterns

**Message:** Method 'Property 'PropertyWithThrow' getter' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     140:     // Property with throw
     141:     public int PropertyWithThrow
     142:     {
>>>  143:         get => throw new NotImplementedException();
     144:         set
     145:         {
     146:             throw new NotImplementedException();
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 144, Column 9  
**Project:** ExceptionPatterns

**Message:** Method 'Property 'PropertyWithThrow' setter' contains throw statement(s)

**Code:**

```csharp
     141:     public int PropertyWithThrow
     142:     {
     143:         get => throw new NotImplementedException();
>>>  144:         set
     145:         {
     146:             throw new NotImplementedException();
     147:         }
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 144, Column 9  
**Project:** ExceptionPatterns

**Message:** Method 'Property 'PropertyWithThrow' setter' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     141:     public int PropertyWithThrow
     142:     {
     143:         get => throw new NotImplementedException();
>>>  144:         set
     145:         {
     146:             throw new NotImplementedException();
     147:         }
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 151, Column 36  
**Project:** ExceptionPatterns

**Message:** Method 'Operator '+'' contains throw statement(s)

**Code:**

```csharp
     148:     }
     149: 
     150:     // Operator with throw
>>>  151:     public static Program operator +(Program a, Program b)
     152:     {
     153:         throw new NotImplementedException();
     154:     }
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 151, Column 36  
**Project:** ExceptionPatterns

**Message:** Method 'Operator '+'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     148:     }
     149: 
     150:     // Operator with throw
>>>  151:     public static Program operator +(Program a, Program b)
     152:     {
     153:         throw new NotImplementedException();
     154:     }
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 159, Column 14  
**Project:** ExceptionPatterns

**Message:** Method 'Local function 'LocalFunction'' contains throw statement(s)

**Code:**

```csharp
     156:     // Local function with throw
     157:     void MethodWithLocalFunction()
     158:     {
>>>  159:         void LocalFunction()
     160:         {
     161:             throw new InvalidOperationException();
     162:         }
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 159, Column 14  
**Project:** ExceptionPatterns

**Message:** Method 'Local function 'LocalFunction'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     156:     // Local function with throw
     157:     void MethodWithLocalFunction()
     158:     {
>>>  159:         void LocalFunction()
     160:         {
     161:             throw new InvalidOperationException();
     162:         }
```

---

#### THROWS017: Method calls throwing method without handling

**Severity:** Warning  
**Location:** Line 164, Column 9  
**Project:** ExceptionPatterns

**Message:** Method calls 'Program.LocalFunction' which may throw InvalidOperationException, but does not handle it

**Code:**

```csharp
     161:             throw new InvalidOperationException();
     162:         }
     163: 
>>>  164:         LocalFunction();
     165:     }
     166: 
     167:     // Lambda with throw
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 170, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s)

**Code:**

```csharp
     167:     // Lambda with throw
     168:     void MethodWithLambda()
     169:     {
>>>  170:         Action lambda = () => throw new InvalidOperationException();
     171:         lambda();
     172:     }
     173: 
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 170, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     167:     // Lambda with throw
     168:     void MethodWithLambda()
     169:     {
>>>  170:         Action lambda = () => throw new InvalidOperationException();
     171:         lambda();
     172:     }
     173: 
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 185, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s)

**Code:**

```csharp
     182:     void EventHandlerLambdaUncaught()
     183:     {
     184:         // BAD: Lambda event handler throws without catching
>>>  185:         DataReceived += (sender, e) =>
     186:         {
     187:             throw new InvalidOperationException("Data processing failed");
     188:         };
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 185, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     182:     void EventHandlerLambdaUncaught()
     183:     {
     184:         // BAD: Lambda event handler throws without catching
>>>  185:         DataReceived += (sender, e) =>
     186:         {
     187:             throw new InvalidOperationException("Data processing failed");
     188:         };
```

---

#### THROWS026: Event handler lambda throws exception without catching it

**Severity:** Warning  
**Location:** Line 187, Column 13  
**Project:** ExceptionPatterns

**Message:** Event handler lambda throws System.InvalidOperationException which is not caught - exception may crash application

**Code:**

```csharp
     184:         // BAD: Lambda event handler throws without catching
     185:         DataReceived += (sender, e) =>
     186:         {
>>>  187:             throw new InvalidOperationException("Data processing failed");
     188:         };
     189:     }
     190: 
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 194, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains try/catch block(s)

**Code:**

```csharp
     191:     // GOOD: Event handler lambda catches exceptions
     192:     void EventHandlerLambdaCaught()
     193:     {
>>>  194:         DataReceived += (sender, e) =>
     195:         {
     196:             try
     197:             {
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 212, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s)

**Code:**

```csharp
     209:     void EventHandlerWithThrowExpression()
     210:     {
     211:         // BAD: Throw expression in event handler lambda
>>>  212:         DataReceived += (sender, e) => throw new InvalidOperationException();
     213:     }
     214: 
     215:     // THROWS026: Event handler lambda with rethrow
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 212, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     209:     void EventHandlerWithThrowExpression()
     210:     {
     211:         // BAD: Throw expression in event handler lambda
>>>  212:         DataReceived += (sender, e) => throw new InvalidOperationException();
     213:     }
     214: 
     215:     // THROWS026: Event handler lambda with rethrow
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 219, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s)

**Code:**

```csharp
     216:     void EventHandlerWithRethrow()
     217:     {
     218:         // BAD: Rethrow still escapes event handler
>>>  219:         DataReceived += (sender, e) =>
     220:         {
     221:             try
     222:             {
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 219, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains try/catch block(s)

**Code:**

```csharp
     216:     void EventHandlerWithRethrow()
     217:     {
     218:         // BAD: Rethrow still escapes event handler
>>>  219:         DataReceived += (sender, e) =>
     220:         {
     221:             try
     222:             {
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 219, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     216:     void EventHandlerWithRethrow()
     217:     {
     218:         // BAD: Rethrow still escapes event handler
>>>  219:         DataReceived += (sender, e) =>
     220:         {
     221:             try
     222:             {
```

---

#### THROWS009: Catch block only rethrows exception

**Severity:** Warning  
**Location:** Line 225, Column 13  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' has catch block that only rethrows - consider removing unnecessary catch

**Code:**

```csharp
     222:             {
     223:                 ProcessData();
     224:             }
>>>  225:             catch (InvalidOperationException)
     226:             {
     227:                 throw; // This still crashes the application
     228:             }
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 245, Column 18  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'OnDataReceived'' contains throw statement(s)

**Code:**

```csharp
     242:     }
     243: 
     244:     // BAD: Event handler method that throws (triggers THROWS001/THROWS002)
>>>  245:     private void OnDataReceived(object sender, EventArgs e)
     246:     {
     247:         throw new InvalidOperationException("Processing failed");
     248:     }
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 245, Column 18  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'OnDataReceived'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     242:     }
     243: 
     244:     // BAD: Event handler method that throws (triggers THROWS001/THROWS002)
>>>  245:     private void OnDataReceived(object sender, EventArgs e)
     246:     {
     247:         throw new InvalidOperationException("Processing failed");
     248:     }
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 251, Column 18  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'OnProcessingCompleted'' contains try/catch block(s)

**Code:**

```csharp
     248:     }
     249: 
     250:     // GOOD: Event handler method that catches exceptions
>>>  251:     private void OnProcessingCompleted(object sender, EventArgs e)
     252:     {
     253:         try
     254:         {
```

---

#### THROWS010: Overly broad exception catch

**Severity:** Warning  
**Location:** Line 257, Column 9  
**Project:** ExceptionPatterns

**Message:** Method 'Method 'OnProcessingCompleted'' catches 'System.Exception' which is too broad - consider catching specific exception types

**Code:**

```csharp
     254:         {
     255:             ProcessData();
     256:         }
>>>  257:         catch (Exception ex)
     258:         {
     259:             Console.WriteLine($"Error: {ex.Message}");
     260:         }
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 280, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s)

**Code:**

```csharp
     277:         var button = new Button();
     278: 
     279:         // BAD: Uncaught exception in click handler can crash UI application
>>>  280:         button.Click += (sender, e) =>
     281:         {
     282:             throw new InvalidOperationException("Button action failed");
     283:         };
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 280, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     277:         var button = new Button();
     278: 
     279:         // BAD: Uncaught exception in click handler can crash UI application
>>>  280:         button.Click += (sender, e) =>
     281:         {
     282:             throw new InvalidOperationException("Button action failed");
     283:         };
```

---

#### THROWS026: Event handler lambda throws exception without catching it

**Severity:** Warning  
**Location:** Line 282, Column 13  
**Project:** ExceptionPatterns

**Message:** Event handler lambda throws System.InvalidOperationException which is not caught - exception may crash application

**Code:**

```csharp
     279:         // BAD: Uncaught exception in click handler can crash UI application
     280:         button.Click += (sender, e) =>
     281:         {
>>>  282:             throw new InvalidOperationException("Button action failed");
     283:         };
     284: 
     285:         // GOOD: Catch and handle exceptions appropriately
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 286, Column 25  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains try/catch block(s)

**Code:**

```csharp
     283:         };
     284: 
     285:         // GOOD: Catch and handle exceptions appropriately
>>>  286:         button.Click += (sender, e) =>
     287:         {
     288:             try
     289:             {
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 307, Column 28  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s)

**Code:**

```csharp
     304:     void CustomEventHandlerPattern()
     305:     {
     306:         // THROWS026: Custom delegate also triggers the analyzer
>>>  307:         CustomDataEvent += (sender, args) =>
     308:         {
     309:             throw new InvalidOperationException();
     310:         };
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 307, Column 28  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     304:     void CustomEventHandlerPattern()
     305:     {
     306:         // THROWS026: Custom delegate also triggers the analyzer
>>>  307:         CustomDataEvent += (sender, args) =>
     308:         {
     309:             throw new InvalidOperationException();
     310:         };
```

---

#### THROWS026: Event handler lambda throws exception without catching it

**Severity:** Warning  
**Location:** Line 309, Column 13  
**Project:** ExceptionPatterns

**Message:** Event handler lambda throws System.InvalidOperationException which is not caught - exception may crash application

**Code:**

```csharp
     306:         // THROWS026: Custom delegate also triggers the analyzer
     307:         CustomDataEvent += (sender, args) =>
     308:         {
>>>  309:             throw new InvalidOperationException();
     310:         };
     311:     }
     312: 
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 323, Column 34  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s)

**Code:**

```csharp
     320:         var items = new[] { 1, 2, 3, -1, 5 };
     321: 
     322:         // BAD: Lambda throws without catching
>>>  323:         var result = items.Where(x =>
     324:         {
     325:             if (x < 0)
     326:                 throw new InvalidOperationException("Negative value");
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 323, Column 34  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     320:         var items = new[] { 1, 2, 3, -1, 5 };
     321: 
     322:         // BAD: Lambda throws without catching
>>>  323:         var result = items.Where(x =>
     324:         {
     325:             if (x < 0)
     326:                 throw new InvalidOperationException("Negative value");
```

---

#### THROWS025: Lambda throws exception without catching it

**Severity:** Warning  
**Location:** Line 326, Column 17  
**Project:** ExceptionPatterns

**Message:** Lambda expression throws System.InvalidOperationException which is not caught within the lambda - exception will propagate to LINQ query evaluator

**Code:**

```csharp
     323:         var result = items.Where(x =>
     324:         {
     325:             if (x < 0)
>>>  326:                 throw new InvalidOperationException("Negative value");
     327:             return x > 1;
     328:         });
     329:     }
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 336, Column 34  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s)

**Code:**

```csharp
     333:     {
     334:         var items = new[] { 1, 2, 3, -1, 5 };
     335: 
>>>  336:         var result = items.Where(x =>
     337:         {
     338:             try
     339:             {
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 336, Column 34  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains try/catch block(s)

**Code:**

```csharp
     333:     {
     334:         var items = new[] { 1, 2, 3, -1, 5 };
     335: 
>>>  336:         var result = items.Where(x =>
     337:         {
     338:             try
     339:             {
```

---

#### THROWS027: Exception used for control flow

**Severity:** Warning  
**Location:** Line 341, Column 21  
**Project:** ExceptionPatterns

**Message:** Exception 'System.InvalidOperationException' is thrown and caught in the same method - consider using return values instead

**Code:**

```csharp
     338:             try
     339:             {
     340:                 if (x < 0)
>>>  341:                     throw new InvalidOperationException("Negative value");
     342:                 return x > 1;
     343:             }
     344:             catch (InvalidOperationException)
```

---

#### THROWS025: Lambda throws exception without catching it

**Severity:** Warning  
**Location:** Line 357, Column 53  
**Project:** ExceptionPatterns

**Message:** Lambda expression throws System.ArgumentException which is not caught within the lambda - exception will propagate to LINQ query evaluator

**Code:**

```csharp
     354:         var items = new[] { 1, 2, 3 };
     355: 
     356:         // BAD: Throw expression
>>>  357:         var result = items.Select(x => x >= 0 ? x : throw new ArgumentException());
     358:     }
     359: 
     360:     #endregion
```

---

#### THROWS001: Method contains throw statement

**Severity:** Warning  
**Location:** Line 357, Column 35  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s)

**Code:**

```csharp
     354:         var items = new[] { 1, 2, 3 };
     355: 
     356:         // BAD: Throw expression
>>>  357:         var result = items.Select(x => x >= 0 ? x : throw new ArgumentException());
     358:     }
     359: 
     360:     #endregion
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 357, Column 35  
**Project:** ExceptionPatterns

**Message:** Method 'Lambda expression' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     354:         var items = new[] { 1, 2, 3 };
     355: 
     356:         // BAD: Throw expression
>>>  357:         var result = items.Select(x => x >= 0 ? x : throw new ArgumentException());
     358:     }
     359: 
     360:     #endregion
```

---

