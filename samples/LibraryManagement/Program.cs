using LibraryManagement;

Console.WriteLine("=== Library Management System Demo ===\n");

var library = new LibraryService();

// Add some books
try
{
    var book1 = new Book("978-0-13-468599-1", "Clean Code", "Robert C. Martin", 2008);
    var book2 = new Book("978-0-201-61622-4", "The Pragmatic Programmer", "Hunt & Thomas", 1999);
    var book3 = new Book("978-0-13-235088-4", "Clean Architecture", "Robert C. Martin", 2017);

    library.AddBook(book1);
    library.AddBook(book2);
    library.AddBook(book3);

    Console.WriteLine($"Added {library.GetTotalBooks()} books to library");
}
catch (Exception ex)
{
    Console.WriteLine($"Error adding books: {ex.Message}");
}

// Add some members
try
{
    var member1 = new Member("M001", "Alice Johnson", "alice@example.com");
    var member2 = new Member("M002", "Bob Smith", "bob@example.com");

    library.AddMember(member1);
    library.AddMember(member2);

    Console.WriteLine($"Added {library.GetTotalMembers()} members\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Error adding members: {ex.Message}");
}

// Demonstrate successful checkout (3-level call hierarchy)
Console.WriteLine("--- Successful Checkout ---");
try
{
    library.CheckOutBookToMember("978-0-13-468599-1", "M001");
    Console.WriteLine("✓ Alice checked out 'Clean Code'");

    var member = library.FindMember("M001");
    Console.WriteLine($"  {member?.GetMemberInfo()}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Checkout failed: {ex.Message}\n");
}

// Demonstrate validation errors (throws in Level 3)
Console.WriteLine("--- Attempted Invalid Operations ---");

// Try to checkout non-existent book
try
{
    library.CheckOutBookToMember("INVALID-ISBN", "M001");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ {ex.GetType().Name}: {ex.Message}");
}

// Try to checkout already checked out book
try
{
    library.CheckOutBookToMember("978-0-13-468599-1", "M002");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ {ex.GetType().Name}: {ex.Message}");
}

// Try to checkout with invalid member
try
{
    library.CheckOutBookToMember("978-0-201-61622-4", "M999");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ {ex.GetType().Name}: {ex.Message}\n");
}

// Demonstrate successful return
Console.WriteLine("--- Successful Return ---");
try
{
    library.ReturnBookFromMember("978-0-13-468599-1", "M001");
    Console.WriteLine("✓ Alice returned 'Clean Code'\n");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Return failed: {ex.Message}\n");
}

// Display library report (method without throws)
Console.WriteLine(library.GenerateLibraryReport());

// Demonstrate member limit
Console.WriteLine("\n--- Testing Book Limit ---");
try
{
    var member = library.FindMember("M001");
    for (int i = 0; i < 6; i++)
    {
        if (member != null && member.CanCheckOutMoreBooks())
        {
            Console.WriteLine($"  Member can check out more books ({member.GetCheckedOutCount()}/{Member.MaxBooksAllowed})");
        }
        else
        {
            Console.WriteLine($"  Member has reached limit!");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.WriteLine("\n=== Demo Complete ===");
