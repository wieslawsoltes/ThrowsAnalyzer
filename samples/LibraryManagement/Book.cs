namespace LibraryManagement;

public class Book
{
    public string ISBN { get; }
    public string Title { get; }
    public string Author { get; }
    public int YearPublished { get; }
    public bool IsAvailable { get; private set; }

    public Book(string isbn, string title, string author, int yearPublished)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN cannot be empty", nameof(isbn));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author cannot be empty", nameof(author));

        if (yearPublished < 1000 || yearPublished > DateTime.Now.Year)
            throw new ArgumentOutOfRangeException(nameof(yearPublished), "Year must be between 1000 and current year");

        ISBN = isbn;
        Title = title;
        Author = author;
        YearPublished = yearPublished;
        IsAvailable = true;
    }

    // Method without throws - just updates state
    public void MarkAsCheckedOut()
    {
        IsAvailable = false;
    }

    // Method without throws - just updates state
    public void MarkAsReturned()
    {
        IsAvailable = true;
    }

    // Method without throws - just returns data
    public string GetDisplayInfo()
    {
        var status = IsAvailable ? "Available" : "Checked Out";
        return $"{Title} by {Author} ({YearPublished}) - ISBN: {ISBN} - {status}";
    }

    // Method without throws - simple calculation
    public int GetAge()
    {
        return DateTime.Now.Year - YearPublished;
    }
}
