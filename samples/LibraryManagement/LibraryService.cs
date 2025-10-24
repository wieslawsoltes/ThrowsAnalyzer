namespace LibraryManagement;

/// <summary>
/// Library service demonstrating 3-level call hierarchy with throws
/// Level 1: CheckOutBookToMember (calls Level 2)
/// Level 2: ValidateCheckout (calls Level 3)
/// Level 3: ValidateBookAvailability, ValidateMemberEligibility (contains throws)
/// </summary>
public class LibraryService
{
    private readonly Dictionary<string, Book> _books;
    private readonly Dictionary<string, Member> _members;

    public LibraryService()
    {
        _books = new Dictionary<string, Book>();
        _members = new Dictionary<string, Member>();
    }

    // Methods without throws - simple operations
    public void AddBook(Book book)
    {
        _books[book.ISBN] = book;
    }

    public void AddMember(Member member)
    {
        _members[member.MemberId] = member;
    }

    public Book? FindBook(string isbn)
    {
        return _books.TryGetValue(isbn, out var book) ? book : null;
    }

    public Member? FindMember(string memberId)
    {
        return _members.TryGetValue(memberId, out var member) ? member : null;
    }

    public int GetTotalBooks()
    {
        return _books.Count;
    }

    public int GetTotalMembers()
    {
        return _members.Count;
    }

    public List<Book> GetAvailableBooks()
    {
        return _books.Values.Where(b => b.IsAvailable).ToList();
    }

    // LEVEL 1: Top-level method that orchestrates checkout
    // Calls Level 2 method
    public void CheckOutBookToMember(string isbn, string memberId)
    {
        // Call Level 2 - validation method
        ValidateCheckout(isbn, memberId);

        var book = _books[isbn];
        var member = _members[memberId];

        // Perform checkout
        member.CheckOutBook(isbn);
        book.MarkAsCheckedOut();
    }

    // LEVEL 2: Validation orchestrator
    // Calls Level 3 methods
    private void ValidateCheckout(string isbn, string memberId)
    {
        // Call Level 3 validation methods
        ValidateBookAvailability(isbn);
        ValidateMemberEligibility(memberId);
    }

    // LEVEL 3: Specific validation with throws
    private void ValidateBookAvailability(string isbn)
    {
        if (!_books.ContainsKey(isbn))
            throw new KeyNotFoundException($"Book with ISBN {isbn} not found");

        var book = _books[isbn];
        if (!book.IsAvailable)
            throw new InvalidOperationException($"Book '{book.Title}' is currently checked out");
    }

    // LEVEL 3: Specific validation with throws
    private void ValidateMemberEligibility(string memberId)
    {
        if (!_members.ContainsKey(memberId))
            throw new KeyNotFoundException($"Member with ID {memberId} not found");

        var member = _members[memberId];
        if (!member.CanCheckOutMoreBooks())
            throw new InvalidOperationException($"Member '{member.Name}' has reached the maximum book limit");
    }

    // Another 3-level hierarchy for returns
    // LEVEL 1: Top-level return method
    public void ReturnBookFromMember(string isbn, string memberId)
    {
        // Call Level 2
        ValidateReturn(isbn, memberId);

        var book = _books[isbn];
        var member = _members[memberId];

        // Perform return
        member.ReturnBook(isbn);
        book.MarkAsReturned();
    }

    // LEVEL 2: Validation for return
    private void ValidateReturn(string isbn, string memberId)
    {
        // Call Level 3
        ValidateBookExists(isbn);
        ValidateMemberHasBook(memberId, isbn);
    }

    // LEVEL 3: Validation with throws
    private void ValidateBookExists(string isbn)
    {
        if (!_books.ContainsKey(isbn))
            throw new KeyNotFoundException($"Book with ISBN {isbn} not found in library");
    }

    // LEVEL 3: Validation with throws
    private void ValidateMemberHasBook(string memberId, string isbn)
    {
        if (!_members.ContainsKey(memberId))
            throw new KeyNotFoundException($"Member with ID {memberId} not found");

        var member = _members[memberId];
        if (!member.CheckedOutBooks.Contains(isbn))
            throw new InvalidOperationException($"Member '{member.Name}' does not have this book checked out");
    }

    // Method without throws - just generates report
    public string GenerateLibraryReport()
    {
        var availableCount = _books.Values.Count(b => b.IsAvailable);
        var checkedOutCount = _books.Count - availableCount;

        return $"""
            Library Report
            ==============
            Total Books: {_books.Count}
            Available: {availableCount}
            Checked Out: {checkedOutCount}
            Total Members: {_members.Count}
            """;
    }

    // Method with try-catch blocks (demonstrates THROWS003)
    // This method handles exceptions gracefully instead of propagating them
    public bool TryCheckOutBookToMember(string isbn, string memberId, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            // Attempt to validate and checkout the book
            ValidateCheckout(isbn, memberId);

            var book = _books[isbn];
            var member = _members[memberId];

            member.CheckOutBook(isbn);
            book.MarkAsCheckedOut();

            return true;
        }
        catch (KeyNotFoundException ex)
        {
            errorMessage = $"Not found: {ex.Message}";
            return false;
        }
        catch (InvalidOperationException ex)
        {
            errorMessage = $"Invalid operation: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"Unexpected error: {ex.Message}";
            return false;
        }
    }

    // Another method with try-catch for return operations (demonstrates THROWS003)
    public bool TryReturnBookFromMember(string isbn, string memberId, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            ValidateReturn(isbn, memberId);

            var book = _books[isbn];
            var member = _members[memberId];

            member.ReturnBook(isbn);
            book.MarkAsReturned();

            return true;
        }
        catch (KeyNotFoundException ex)
        {
            errorMessage = $"Not found: {ex.Message}";
            return false;
        }
        catch (InvalidOperationException ex)
        {
            errorMessage = $"Invalid operation: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"Unexpected error: {ex.Message}";
            return false;
        }
    }

    // Method with nested try-catch blocks (demonstrates THROWS003)
    public void BatchCheckoutBooks(string memberId, List<string> isbns)
    {
        var successfulCheckouts = new List<string>();

        try
        {
            foreach (var isbn in isbns)
            {
                try
                {
                    ValidateCheckout(isbn, memberId);

                    var book = _books[isbn];
                    var member = _members[memberId];

                    member.CheckOutBook(isbn);
                    book.MarkAsCheckedOut();

                    successfulCheckouts.Add(isbn);
                }
                catch (InvalidOperationException ex)
                {
                    // Log individual book checkout failure and continue
                    Console.WriteLine($"Failed to checkout {isbn}: {ex.Message}");
                }
            }
        }
        catch (KeyNotFoundException ex)
        {
            // Handle member not found
            Console.WriteLine($"Member error: {ex.Message}");

            // Rollback successful checkouts
            foreach (var isbn in successfulCheckouts)
            {
                try
                {
                    var book = _books[isbn];
                    var member = _members[memberId];
                    member.ReturnBook(isbn);
                    book.MarkAsReturned();
                }
                catch (Exception rollbackEx)
                {
                    Console.WriteLine($"Rollback failed for {isbn}: {rollbackEx.Message}");
                }
            }
        }
    }
}
