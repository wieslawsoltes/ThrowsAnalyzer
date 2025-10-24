namespace LibraryManagement;

public class Member
{
    public string MemberId { get; }
    public string Name { get; }
    public string Email { get; }
    public DateTime MembershipDate { get; }
    public List<string> CheckedOutBooks { get; }
    public const int MaxBooksAllowed = 5;

    public Member(string memberId, string name, string email)
    {
        if (string.IsNullOrWhiteSpace(memberId))
            throw new ArgumentException("Member ID cannot be empty", nameof(memberId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            throw new ArgumentException("Valid email is required", nameof(email));

        MemberId = memberId;
        Name = name;
        Email = email;
        MembershipDate = DateTime.Now;
        CheckedOutBooks = new List<string>();
    }

    // Method with throw
    public void CheckOutBook(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN cannot be empty", nameof(isbn));

        if (CheckedOutBooks.Count >= MaxBooksAllowed)
            throw new InvalidOperationException($"Member has reached maximum books limit of {MaxBooksAllowed}");

        if (CheckedOutBooks.Contains(isbn))
            throw new InvalidOperationException("Book is already checked out by this member");

        CheckedOutBooks.Add(isbn);
    }

    // Method with throw
    public void ReturnBook(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN cannot be empty", nameof(isbn));

        if (!CheckedOutBooks.Contains(isbn))
            throw new InvalidOperationException("Member does not have this book checked out");

        CheckedOutBooks.Remove(isbn);
    }

    // Method without throws - just checks
    public bool CanCheckOutMoreBooks()
    {
        return CheckedOutBooks.Count < MaxBooksAllowed;
    }

    // Method without throws - just returns data
    public int GetCheckedOutCount()
    {
        return CheckedOutBooks.Count;
    }

    // Method without throws - just returns data
    public string GetMemberInfo()
    {
        return $"{Name} ({MemberId}) - Member since {MembershipDate:yyyy-MM-dd} - Books: {CheckedOutBooks.Count}/{MaxBooksAllowed}";
    }
}
