# ThrowsAnalyzer Report: LibraryManagement

**Analysis Date:** 2025-10-27 13:39:58
**Target:** Project - `/Users/wieslawsoltes/GitHub/ThrowsAnalyzer/samples/LibraryManagement/LibraryManagement.csproj`
**Configuration:** Debug
**Duration:** 2.18s
**Status:** ✓ Success

## Summary

| Metric | Count |
|--------|------:|
| Total Diagnostics | 51 |
| Errors | 0 |
| Warnings | 25 |
| Info/Hidden | 26 |
| Projects Analyzed | 1 |
| Files Analyzed | 3 |

## Diagnostics by ID

| Diagnostic ID | Count | Percentage |
|--------------|------:|-----------:|
| THROWS029 | 10 | 19.6% |
| THROWS001 | 8 | 15.7% |
| THROWS002 | 8 | 15.7% |
| THROWS017 | 8 | 15.7% |
| THROWS019 | 6 | 11.8% |
| THROWS030 | 5 | 9.8% |
| THROWS010 | 3 | 5.9% |
| THROWS003 | 3 | 5.9% |

## Diagnostics by Severity

| Severity | Count | Percentage |
|----------|------:|-----------:|
| Info | 26 | 51.0% |
| Warning | 25 | 49.0% |

## Top 20 Files with Most Diagnostics

| File | Count |
|------|------:|
| `LibraryService.cs` | 35 |
| `Member.cs` | 13 |
| `Book.cs` | 3 |

## Detailed Diagnostics

### Book.cs

#### THROWS019: Public API throws undocumented exception

**Severity:** Warning  
**Location:** Line 11, Column 12  
**Project:** LibraryManagement

**Message:** Public method 'Book()' may throw ArgumentException and ArgumentOutOfRangeException, but it is not documented

**Code:**

```csharp
       8:     public int YearPublished { get; }
       9:     public bool IsAvailable { get; private set; }
      10: 
>>>   11:     public Book(string isbn, string title, string author, int yearPublished)
      12:     {
      13:         if (string.IsNullOrWhiteSpace(isbn))
      14:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
```

---

#### THROWS001: Method contains throw statement

**Severity:** Info  
**Location:** Line 11, Column 12  
**Project:** LibraryManagement

**Message:** Method 'Constructor 'Book'' contains throw statement(s)

**Code:**

```csharp
       8:     public int YearPublished { get; }
       9:     public bool IsAvailable { get; private set; }
      10: 
>>>   11:     public Book(string isbn, string title, string author, int yearPublished)
      12:     {
      13:         if (string.IsNullOrWhiteSpace(isbn))
      14:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 11, Column 12  
**Project:** LibraryManagement

**Message:** Method 'Constructor 'Book'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
       8:     public int YearPublished { get; }
       9:     public bool IsAvailable { get; private set; }
      10: 
>>>   11:     public Book(string isbn, string title, string author, int yearPublished)
      12:     {
      13:         if (string.IsNullOrWhiteSpace(isbn))
      14:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
```

---

### LibraryService.cs

#### THROWS019: Public API throws undocumented exception

**Severity:** Warning  
**Location:** Line 58, Column 17  
**Project:** LibraryManagement

**Message:** Public method 'LibraryService.CheckOutBookToMember' may throw KeyNotFoundException, InvalidOperationException and ArgumentException, but it is not documented

**Code:**

```csharp
      55: 
      56:     // LEVEL 1: Top-level method that orchestrates checkout
      57:     // Calls Level 2 method
>>>   58:     public void CheckOutBookToMember(string isbn, string memberId)
      59:     {
      60:         // Call Level 2 - validation method
      61:         ValidateCheckout(isbn, memberId);
```

---

#### THROWS017: Method calls throwing method without handling

**Severity:** Warning  
**Location:** Line 61, Column 9  
**Project:** LibraryManagement

**Message:** Method calls 'LibraryService.ValidateCheckout' which may throw KeyNotFoundException or InvalidOperationException, but does not handle it

**Code:**

```csharp
      58:     public void CheckOutBookToMember(string isbn, string memberId)
      59:     {
      60:         // Call Level 2 - validation method
>>>   61:         ValidateCheckout(isbn, memberId);
      62: 
      63:         var book = _books[isbn];
      64:         var member = _members[memberId];
```

---

#### THROWS017: Method calls throwing method without handling

**Severity:** Warning  
**Location:** Line 67, Column 9  
**Project:** LibraryManagement

**Message:** Method calls 'Member.CheckOutBook' which may throw ArgumentException or InvalidOperationException, but does not handle it

**Code:**

```csharp
      64:         var member = _members[memberId];
      65: 
      66:         // Perform checkout
>>>   67:         member.CheckOutBook(isbn);
      68:         book.MarkAsCheckedOut();
      69:     }
      70: 
```

---

#### THROWS017: Method calls throwing method without handling

**Severity:** Warning  
**Location:** Line 76, Column 9  
**Project:** LibraryManagement

**Message:** Method calls 'LibraryService.ValidateBookAvailability' which may throw KeyNotFoundException or InvalidOperationException, but does not handle it

**Code:**

```csharp
      73:     private void ValidateCheckout(string isbn, string memberId)
      74:     {
      75:         // Call Level 3 validation methods
>>>   76:         ValidateBookAvailability(isbn);
      77:         ValidateMemberEligibility(memberId);
      78:     }
      79: 
```

---

#### THROWS017: Method calls throwing method without handling

**Severity:** Warning  
**Location:** Line 77, Column 9  
**Project:** LibraryManagement

**Message:** Method calls 'LibraryService.ValidateMemberEligibility' which may throw KeyNotFoundException or InvalidOperationException, but does not handle it

**Code:**

```csharp
      74:     {
      75:         // Call Level 3 validation methods
      76:         ValidateBookAvailability(isbn);
>>>   77:         ValidateMemberEligibility(memberId);
      78:     }
      79: 
      80:     // LEVEL 3: Specific validation with throws
```

---

#### THROWS001: Method contains throw statement

**Severity:** Info  
**Location:** Line 81, Column 18  
**Project:** LibraryManagement

**Message:** Method 'Method 'ValidateBookAvailability'' contains throw statement(s)

**Code:**

```csharp
      78:     }
      79: 
      80:     // LEVEL 3: Specific validation with throws
>>>   81:     private void ValidateBookAvailability(string isbn)
      82:     {
      83:         if (!_books.ContainsKey(isbn))
      84:             throw new KeyNotFoundException($"Book with ISBN {isbn} not found");
```

---

#### THROWS030: Consider using Result<T> pattern for expected errors

**Severity:** Info  
**Location:** Line 81, Column 18  
**Project:** LibraryManagement

**Message:** Method 'ValidateBookAvailability' throws 'System.Collections.Generic.KeyNotFoundException, System.InvalidOperationException' for expected validation - consider using Result<T> pattern

**Code:**

```csharp
      78:     }
      79: 
      80:     // LEVEL 3: Specific validation with throws
>>>   81:     private void ValidateBookAvailability(string isbn)
      82:     {
      83:         if (!_books.ContainsKey(isbn))
      84:             throw new KeyNotFoundException($"Book with ISBN {isbn} not found");
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 81, Column 18  
**Project:** LibraryManagement

**Message:** Method 'Method 'ValidateBookAvailability'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
      78:     }
      79: 
      80:     // LEVEL 3: Specific validation with throws
>>>   81:     private void ValidateBookAvailability(string isbn)
      82:     {
      83:         if (!_books.ContainsKey(isbn))
      84:             throw new KeyNotFoundException($"Book with ISBN {isbn} not found");
```

---

#### THROWS029: Exception thrown in potential hot path

**Severity:** Info  
**Location:** Line 84, Column 13  
**Project:** LibraryManagement

**Message:** Exception 'System.Collections.Generic.KeyNotFoundException' is thrown in validation method (consider returning bool) - consider performance implications

**Code:**

```csharp
      81:     private void ValidateBookAvailability(string isbn)
      82:     {
      83:         if (!_books.ContainsKey(isbn))
>>>   84:             throw new KeyNotFoundException($"Book with ISBN {isbn} not found");
      85: 
      86:         var book = _books[isbn];
      87:         if (!book.IsAvailable)
```

---

#### THROWS029: Exception thrown in potential hot path

**Severity:** Info  
**Location:** Line 88, Column 13  
**Project:** LibraryManagement

**Message:** Exception 'System.InvalidOperationException' is thrown in validation method (consider returning bool) - consider performance implications

**Code:**

```csharp
      85: 
      86:         var book = _books[isbn];
      87:         if (!book.IsAvailable)
>>>   88:             throw new InvalidOperationException($"Book '{book.Title}' is currently checked out");
      89:     }
      90: 
      91:     // LEVEL 3: Specific validation with throws
```

---

#### THROWS001: Method contains throw statement

**Severity:** Info  
**Location:** Line 92, Column 18  
**Project:** LibraryManagement

**Message:** Method 'Method 'ValidateMemberEligibility'' contains throw statement(s)

**Code:**

```csharp
      89:     }
      90: 
      91:     // LEVEL 3: Specific validation with throws
>>>   92:     private void ValidateMemberEligibility(string memberId)
      93:     {
      94:         if (!_members.ContainsKey(memberId))
      95:             throw new KeyNotFoundException($"Member with ID {memberId} not found");
```

---

#### THROWS030: Consider using Result<T> pattern for expected errors

**Severity:** Info  
**Location:** Line 92, Column 18  
**Project:** LibraryManagement

**Message:** Method 'ValidateMemberEligibility' throws 'System.Collections.Generic.KeyNotFoundException, System.InvalidOperationException' for expected validation - consider using Result<T> pattern

**Code:**

```csharp
      89:     }
      90: 
      91:     // LEVEL 3: Specific validation with throws
>>>   92:     private void ValidateMemberEligibility(string memberId)
      93:     {
      94:         if (!_members.ContainsKey(memberId))
      95:             throw new KeyNotFoundException($"Member with ID {memberId} not found");
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 92, Column 18  
**Project:** LibraryManagement

**Message:** Method 'Method 'ValidateMemberEligibility'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
      89:     }
      90: 
      91:     // LEVEL 3: Specific validation with throws
>>>   92:     private void ValidateMemberEligibility(string memberId)
      93:     {
      94:         if (!_members.ContainsKey(memberId))
      95:             throw new KeyNotFoundException($"Member with ID {memberId} not found");
```

---

#### THROWS029: Exception thrown in potential hot path

**Severity:** Info  
**Location:** Line 95, Column 13  
**Project:** LibraryManagement

**Message:** Exception 'System.Collections.Generic.KeyNotFoundException' is thrown in validation method (consider returning bool) - consider performance implications

**Code:**

```csharp
      92:     private void ValidateMemberEligibility(string memberId)
      93:     {
      94:         if (!_members.ContainsKey(memberId))
>>>   95:             throw new KeyNotFoundException($"Member with ID {memberId} not found");
      96: 
      97:         var member = _members[memberId];
      98:         if (!member.CanCheckOutMoreBooks())
```

---

#### THROWS029: Exception thrown in potential hot path

**Severity:** Info  
**Location:** Line 99, Column 13  
**Project:** LibraryManagement

**Message:** Exception 'System.InvalidOperationException' is thrown in validation method (consider returning bool) - consider performance implications

**Code:**

```csharp
      96: 
      97:         var member = _members[memberId];
      98:         if (!member.CanCheckOutMoreBooks())
>>>   99:             throw new InvalidOperationException($"Member '{member.Name}' has reached the maximum book limit");
     100:     }
     101: 
     102:     // Another 3-level hierarchy for returns
```

---

#### THROWS019: Public API throws undocumented exception

**Severity:** Warning  
**Location:** Line 104, Column 17  
**Project:** LibraryManagement

**Message:** Public method 'LibraryService.ReturnBookFromMember' may throw KeyNotFoundException, InvalidOperationException and ArgumentException, but it is not documented

**Code:**

```csharp
     101: 
     102:     // Another 3-level hierarchy for returns
     103:     // LEVEL 1: Top-level return method
>>>  104:     public void ReturnBookFromMember(string isbn, string memberId)
     105:     {
     106:         // Call Level 2
     107:         ValidateReturn(isbn, memberId);
```

---

#### THROWS017: Method calls throwing method without handling

**Severity:** Warning  
**Location:** Line 107, Column 9  
**Project:** LibraryManagement

**Message:** Method calls 'LibraryService.ValidateReturn' which may throw KeyNotFoundException or InvalidOperationException, but does not handle it

**Code:**

```csharp
     104:     public void ReturnBookFromMember(string isbn, string memberId)
     105:     {
     106:         // Call Level 2
>>>  107:         ValidateReturn(isbn, memberId);
     108: 
     109:         var book = _books[isbn];
     110:         var member = _members[memberId];
```

---

#### THROWS017: Method calls throwing method without handling

**Severity:** Warning  
**Location:** Line 113, Column 9  
**Project:** LibraryManagement

**Message:** Method calls 'Member.ReturnBook' which may throw ArgumentException or InvalidOperationException, but does not handle it

**Code:**

```csharp
     110:         var member = _members[memberId];
     111: 
     112:         // Perform return
>>>  113:         member.ReturnBook(isbn);
     114:         book.MarkAsReturned();
     115:     }
     116: 
```

---

#### THROWS017: Method calls throwing method without handling

**Severity:** Warning  
**Location:** Line 121, Column 9  
**Project:** LibraryManagement

**Message:** Method calls 'LibraryService.ValidateBookExists' which may throw KeyNotFoundException, but does not handle it

**Code:**

```csharp
     118:     private void ValidateReturn(string isbn, string memberId)
     119:     {
     120:         // Call Level 3
>>>  121:         ValidateBookExists(isbn);
     122:         ValidateMemberHasBook(memberId, isbn);
     123:     }
     124: 
```

---

#### THROWS017: Method calls throwing method without handling

**Severity:** Warning  
**Location:** Line 122, Column 9  
**Project:** LibraryManagement

**Message:** Method calls 'LibraryService.ValidateMemberHasBook' which may throw KeyNotFoundException or InvalidOperationException, but does not handle it

**Code:**

```csharp
     119:     {
     120:         // Call Level 3
     121:         ValidateBookExists(isbn);
>>>  122:         ValidateMemberHasBook(memberId, isbn);
     123:     }
     124: 
     125:     // LEVEL 3: Validation with throws
```

---

#### THROWS001: Method contains throw statement

**Severity:** Info  
**Location:** Line 126, Column 18  
**Project:** LibraryManagement

**Message:** Method 'Method 'ValidateBookExists'' contains throw statement(s)

**Code:**

```csharp
     123:     }
     124: 
     125:     // LEVEL 3: Validation with throws
>>>  126:     private void ValidateBookExists(string isbn)
     127:     {
     128:         if (!_books.ContainsKey(isbn))
     129:             throw new KeyNotFoundException($"Book with ISBN {isbn} not found in library");
```

---

#### THROWS030: Consider using Result<T> pattern for expected errors

**Severity:** Info  
**Location:** Line 126, Column 18  
**Project:** LibraryManagement

**Message:** Method 'ValidateBookExists' throws 'System.Collections.Generic.KeyNotFoundException' for expected validation - consider using Result<T> pattern

**Code:**

```csharp
     123:     }
     124: 
     125:     // LEVEL 3: Validation with throws
>>>  126:     private void ValidateBookExists(string isbn)
     127:     {
     128:         if (!_books.ContainsKey(isbn))
     129:             throw new KeyNotFoundException($"Book with ISBN {isbn} not found in library");
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 126, Column 18  
**Project:** LibraryManagement

**Message:** Method 'Method 'ValidateBookExists'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     123:     }
     124: 
     125:     // LEVEL 3: Validation with throws
>>>  126:     private void ValidateBookExists(string isbn)
     127:     {
     128:         if (!_books.ContainsKey(isbn))
     129:             throw new KeyNotFoundException($"Book with ISBN {isbn} not found in library");
```

---

#### THROWS029: Exception thrown in potential hot path

**Severity:** Info  
**Location:** Line 129, Column 13  
**Project:** LibraryManagement

**Message:** Exception 'System.Collections.Generic.KeyNotFoundException' is thrown in validation method (consider returning bool) - consider performance implications

**Code:**

```csharp
     126:     private void ValidateBookExists(string isbn)
     127:     {
     128:         if (!_books.ContainsKey(isbn))
>>>  129:             throw new KeyNotFoundException($"Book with ISBN {isbn} not found in library");
     130:     }
     131: 
     132:     // LEVEL 3: Validation with throws
```

---

#### THROWS001: Method contains throw statement

**Severity:** Info  
**Location:** Line 133, Column 18  
**Project:** LibraryManagement

**Message:** Method 'Method 'ValidateMemberHasBook'' contains throw statement(s)

**Code:**

```csharp
     130:     }
     131: 
     132:     // LEVEL 3: Validation with throws
>>>  133:     private void ValidateMemberHasBook(string memberId, string isbn)
     134:     {
     135:         if (!_members.ContainsKey(memberId))
     136:             throw new KeyNotFoundException($"Member with ID {memberId} not found");
```

---

#### THROWS030: Consider using Result<T> pattern for expected errors

**Severity:** Info  
**Location:** Line 133, Column 18  
**Project:** LibraryManagement

**Message:** Method 'ValidateMemberHasBook' throws 'System.Collections.Generic.KeyNotFoundException, System.InvalidOperationException' for expected validation - consider using Result<T> pattern

**Code:**

```csharp
     130:     }
     131: 
     132:     // LEVEL 3: Validation with throws
>>>  133:     private void ValidateMemberHasBook(string memberId, string isbn)
     134:     {
     135:         if (!_members.ContainsKey(memberId))
     136:             throw new KeyNotFoundException($"Member with ID {memberId} not found");
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 133, Column 18  
**Project:** LibraryManagement

**Message:** Method 'Method 'ValidateMemberHasBook'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
     130:     }
     131: 
     132:     // LEVEL 3: Validation with throws
>>>  133:     private void ValidateMemberHasBook(string memberId, string isbn)
     134:     {
     135:         if (!_members.ContainsKey(memberId))
     136:             throw new KeyNotFoundException($"Member with ID {memberId} not found");
```

---

#### THROWS029: Exception thrown in potential hot path

**Severity:** Info  
**Location:** Line 136, Column 13  
**Project:** LibraryManagement

**Message:** Exception 'System.Collections.Generic.KeyNotFoundException' is thrown in validation method (consider returning bool) - consider performance implications

**Code:**

```csharp
     133:     private void ValidateMemberHasBook(string memberId, string isbn)
     134:     {
     135:         if (!_members.ContainsKey(memberId))
>>>  136:             throw new KeyNotFoundException($"Member with ID {memberId} not found");
     137: 
     138:         var member = _members[memberId];
     139:         if (!member.CheckedOutBooks.Contains(isbn))
```

---

#### THROWS029: Exception thrown in potential hot path

**Severity:** Info  
**Location:** Line 140, Column 13  
**Project:** LibraryManagement

**Message:** Exception 'System.InvalidOperationException' is thrown in validation method (consider returning bool) - consider performance implications

**Code:**

```csharp
     137: 
     138:         var member = _members[memberId];
     139:         if (!member.CheckedOutBooks.Contains(isbn))
>>>  140:             throw new InvalidOperationException($"Member '{member.Name}' does not have this book checked out");
     141:     }
     142: 
     143:     // Method without throws - just generates report
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 161, Column 17  
**Project:** LibraryManagement

**Message:** Method 'Method 'TryCheckOutBookToMember'' contains try/catch block(s)

**Code:**

```csharp
     158: 
     159:     // Method with try-catch blocks (demonstrates THROWS003)
     160:     // This method handles exceptions gracefully instead of propagating them
>>>  161:     public bool TryCheckOutBookToMember(string isbn, string memberId, out string? errorMessage)
     162:     {
     163:         errorMessage = null;
     164: 
```

---

#### THROWS010: Overly broad exception catch

**Severity:** Info  
**Location:** Line 188, Column 9  
**Project:** LibraryManagement

**Message:** Method 'Method 'TryCheckOutBookToMember'' catches 'System.Exception' which is too broad - consider catching specific exception types

**Code:**

```csharp
     185:             errorMessage = $"Invalid operation: {ex.Message}";
     186:             return false;
     187:         }
>>>  188:         catch (Exception ex)
     189:         {
     190:             errorMessage = $"Unexpected error: {ex.Message}";
     191:             return false;
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 196, Column 17  
**Project:** LibraryManagement

**Message:** Method 'Method 'TryReturnBookFromMember'' contains try/catch block(s)

**Code:**

```csharp
     193:     }
     194: 
     195:     // Another method with try-catch for return operations (demonstrates THROWS003)
>>>  196:     public bool TryReturnBookFromMember(string isbn, string memberId, out string? errorMessage)
     197:     {
     198:         errorMessage = null;
     199: 
```

---

#### THROWS010: Overly broad exception catch

**Severity:** Info  
**Location:** Line 222, Column 9  
**Project:** LibraryManagement

**Message:** Method 'Method 'TryReturnBookFromMember'' catches 'System.Exception' which is too broad - consider catching specific exception types

**Code:**

```csharp
     219:             errorMessage = $"Invalid operation: {ex.Message}";
     220:             return false;
     221:         }
>>>  222:         catch (Exception ex)
     223:         {
     224:             errorMessage = $"Unexpected error: {ex.Message}";
     225:             return false;
```

---

#### THROWS003: Method contains try/catch block

**Severity:** Warning  
**Location:** Line 230, Column 17  
**Project:** LibraryManagement

**Message:** Method 'Method 'BatchCheckoutBooks'' contains try/catch block(s)

**Code:**

```csharp
     227:     }
     228: 
     229:     // Method with nested try-catch blocks (demonstrates THROWS003)
>>>  230:     public void BatchCheckoutBooks(string memberId, List<string> isbns)
     231:     {
     232:         var successfulCheckouts = new List<string>();
     233: 
```

---

#### THROWS010: Overly broad exception catch

**Severity:** Info  
**Location:** Line 272, Column 17  
**Project:** LibraryManagement

**Message:** Method 'Method 'BatchCheckoutBooks'' catches 'System.Exception' which is too broad - consider catching specific exception types

**Code:**

```csharp
     269:                     member.ReturnBook(isbn);
     270:                     book.MarkAsReturned();
     271:                 }
>>>  272:                 catch (Exception rollbackEx)
     273:                 {
     274:                     Console.WriteLine($"Rollback failed for {isbn}: {rollbackEx.Message}");
     275:                 }
```

---

### Member.cs

#### THROWS019: Public API throws undocumented exception

**Severity:** Warning  
**Location:** Line 12, Column 12  
**Project:** LibraryManagement

**Message:** Public method 'Member()' may throw ArgumentException, but it is not documented

**Code:**

```csharp
       9:     public List<string> CheckedOutBooks { get; }
      10:     public const int MaxBooksAllowed = 5;
      11: 
>>>   12:     public Member(string memberId, string name, string email)
      13:     {
      14:         if (string.IsNullOrWhiteSpace(memberId))
      15:             throw new ArgumentException("Member ID cannot be empty", nameof(memberId));
```

---

#### THROWS001: Method contains throw statement

**Severity:** Info  
**Location:** Line 12, Column 12  
**Project:** LibraryManagement

**Message:** Method 'Constructor 'Member'' contains throw statement(s)

**Code:**

```csharp
       9:     public List<string> CheckedOutBooks { get; }
      10:     public const int MaxBooksAllowed = 5;
      11: 
>>>   12:     public Member(string memberId, string name, string email)
      13:     {
      14:         if (string.IsNullOrWhiteSpace(memberId))
      15:             throw new ArgumentException("Member ID cannot be empty", nameof(memberId));
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 12, Column 12  
**Project:** LibraryManagement

**Message:** Method 'Constructor 'Member'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
       9:     public List<string> CheckedOutBooks { get; }
      10:     public const int MaxBooksAllowed = 5;
      11: 
>>>   12:     public Member(string memberId, string name, string email)
      13:     {
      14:         if (string.IsNullOrWhiteSpace(memberId))
      15:             throw new ArgumentException("Member ID cannot be empty", nameof(memberId));
```

---

#### THROWS019: Public API throws undocumented exception

**Severity:** Warning  
**Location:** Line 31, Column 17  
**Project:** LibraryManagement

**Message:** Public method 'Member.CheckOutBook' may throw ArgumentException and InvalidOperationException, but it is not documented

**Code:**

```csharp
      28:     }
      29: 
      30:     // Method with throw
>>>   31:     public void CheckOutBook(string isbn)
      32:     {
      33:         if (string.IsNullOrWhiteSpace(isbn))
      34:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
```

---

#### THROWS001: Method contains throw statement

**Severity:** Info  
**Location:** Line 31, Column 17  
**Project:** LibraryManagement

**Message:** Method 'Method 'CheckOutBook'' contains throw statement(s)

**Code:**

```csharp
      28:     }
      29: 
      30:     // Method with throw
>>>   31:     public void CheckOutBook(string isbn)
      32:     {
      33:         if (string.IsNullOrWhiteSpace(isbn))
      34:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
```

---

#### THROWS030: Consider using Result<T> pattern for expected errors

**Severity:** Info  
**Location:** Line 31, Column 17  
**Project:** LibraryManagement

**Message:** Method 'CheckOutBook' throws 'System.ArgumentException, System.InvalidOperationException' for expected validation - consider using Result<T> pattern

**Code:**

```csharp
      28:     }
      29: 
      30:     // Method with throw
>>>   31:     public void CheckOutBook(string isbn)
      32:     {
      33:         if (string.IsNullOrWhiteSpace(isbn))
      34:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 31, Column 17  
**Project:** LibraryManagement

**Message:** Method 'Method 'CheckOutBook'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
      28:     }
      29: 
      30:     // Method with throw
>>>   31:     public void CheckOutBook(string isbn)
      32:     {
      33:         if (string.IsNullOrWhiteSpace(isbn))
      34:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
```

---

#### THROWS029: Exception thrown in potential hot path

**Severity:** Info  
**Location:** Line 34, Column 13  
**Project:** LibraryManagement

**Message:** Exception 'System.ArgumentException' is thrown in validation method (consider returning bool) - consider performance implications

**Code:**

```csharp
      31:     public void CheckOutBook(string isbn)
      32:     {
      33:         if (string.IsNullOrWhiteSpace(isbn))
>>>   34:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
      35: 
      36:         if (CheckedOutBooks.Count >= MaxBooksAllowed)
      37:             throw new InvalidOperationException($"Member has reached maximum books limit of {MaxBooksAllowed}");
```

---

#### THROWS029: Exception thrown in potential hot path

**Severity:** Info  
**Location:** Line 37, Column 13  
**Project:** LibraryManagement

**Message:** Exception 'System.InvalidOperationException' is thrown in validation method (consider returning bool) - consider performance implications

**Code:**

```csharp
      34:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
      35: 
      36:         if (CheckedOutBooks.Count >= MaxBooksAllowed)
>>>   37:             throw new InvalidOperationException($"Member has reached maximum books limit of {MaxBooksAllowed}");
      38: 
      39:         if (CheckedOutBooks.Contains(isbn))
      40:             throw new InvalidOperationException("Book is already checked out by this member");
```

---

#### THROWS029: Exception thrown in potential hot path

**Severity:** Info  
**Location:** Line 40, Column 13  
**Project:** LibraryManagement

**Message:** Exception 'System.InvalidOperationException' is thrown in validation method (consider returning bool) - consider performance implications

**Code:**

```csharp
      37:             throw new InvalidOperationException($"Member has reached maximum books limit of {MaxBooksAllowed}");
      38: 
      39:         if (CheckedOutBooks.Contains(isbn))
>>>   40:             throw new InvalidOperationException("Book is already checked out by this member");
      41: 
      42:         CheckedOutBooks.Add(isbn);
      43:     }
```

---

#### THROWS019: Public API throws undocumented exception

**Severity:** Warning  
**Location:** Line 46, Column 17  
**Project:** LibraryManagement

**Message:** Public method 'Member.ReturnBook' may throw ArgumentException and InvalidOperationException, but it is not documented

**Code:**

```csharp
      43:     }
      44: 
      45:     // Method with throw
>>>   46:     public void ReturnBook(string isbn)
      47:     {
      48:         if (string.IsNullOrWhiteSpace(isbn))
      49:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
```

---

#### THROWS001: Method contains throw statement

**Severity:** Info  
**Location:** Line 46, Column 17  
**Project:** LibraryManagement

**Message:** Method 'Method 'ReturnBook'' contains throw statement(s)

**Code:**

```csharp
      43:     }
      44: 
      45:     // Method with throw
>>>   46:     public void ReturnBook(string isbn)
      47:     {
      48:         if (string.IsNullOrWhiteSpace(isbn))
      49:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
```

---

#### THROWS002: Method contains unhandled throw statement

**Severity:** Warning  
**Location:** Line 46, Column 17  
**Project:** LibraryManagement

**Message:** Method 'Method 'ReturnBook'' contains throw statement(s) without try/catch handling

**Code:**

```csharp
      43:     }
      44: 
      45:     // Method with throw
>>>   46:     public void ReturnBook(string isbn)
      47:     {
      48:         if (string.IsNullOrWhiteSpace(isbn))
      49:             throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
```

---

