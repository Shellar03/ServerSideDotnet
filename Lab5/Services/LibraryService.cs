using Lab5.Models;

namespace Lab5.Services;

public class LibraryService
{
    private readonly string _booksPath;
    private readonly string _usersPath;
    private readonly object _lock = new();

    public List<Book> Books { get; private set; } = new();
    public List<User> Users { get; private set; } = new();
    public Dictionary<int, List<Book>> BorrowedBooks { get; } = new();

    public LibraryService(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        _booksPath = Path.Combine(dataDir, "Books.csv");
        _usersPath = Path.Combine(dataDir, "Users.csv");
        ReadBooks();
        ReadUsers();
    }

    // ---------- Read ----------
    public void ReadBooks()
    {
        Books = new List<Book>();
        if (!File.Exists(_booksPath)) return;
        foreach (var line in File.ReadLines(_booksPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var f = line.Split(',');
            if (f.Length < 4) continue;
            if (!int.TryParse(f[0].Trim(), out var id)) continue;
            Books.Add(new Book
            {
                Id = id,
                Title = f[1].Trim(),
                Author = f[2].Trim(),
                ISBN = f[3].Trim()
            });
        }
    }

    public void ReadUsers()
    {
        Users = new List<User>();
        if (!File.Exists(_usersPath)) return;
        foreach (var line in File.ReadLines(_usersPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var f = line.Split(',');
            if (f.Length < 3) continue;
            if (!int.TryParse(f[0].Trim(), out var id)) continue;
            Users.Add(new User
            {
                Id = id,
                Name = f[1].Trim(),
                Email = f[2].Trim()
            });
        }
    }

    // ---------- Book CRUD ----------
    public void AddBook(Book book)
    {
        lock (_lock)
        {
            book.Id = Books.Any() ? Books.Max(b => b.Id) + 1 : 1;
            Books.Add(book);
            SaveBooks();
        }
    }

    public bool EditBook(int id, string? title, string? author, string? isbn)
    {
        lock (_lock)
        {
            var book = Books.FirstOrDefault(b => b.Id == id);
            if (book is null) return false;
            if (!string.IsNullOrWhiteSpace(title)) book.Title = title;
            if (!string.IsNullOrWhiteSpace(author)) book.Author = author;
            if (!string.IsNullOrWhiteSpace(isbn)) book.ISBN = isbn;
            SaveBooks();
            return true;
        }
    }

    public bool DeleteBook(int id)
    {
        lock (_lock)
        {
            var book = Books.FirstOrDefault(b => b.Id == id);
            if (book is null) return false;
            Books.Remove(book);
            SaveBooks();
            return true;
        }
    }

    // ---------- User CRUD ----------
    public void AddUser(User user)
    {
        lock (_lock)
        {
            user.Id = Users.Any() ? Users.Max(u => u.Id) + 1 : 1;
            Users.Add(user);
            SaveUsers();
        }
    }

    public bool EditUser(int id, string? name, string? email)
    {
        lock (_lock)
        {
            var user = Users.FirstOrDefault(u => u.Id == id);
            if (user is null) return false;
            if (!string.IsNullOrWhiteSpace(name)) user.Name = name;
            if (!string.IsNullOrWhiteSpace(email)) user.Email = email;
            SaveUsers();
            return true;
        }
    }

    public bool DeleteUser(int id)
    {
        lock (_lock)
        {
            var user = Users.FirstOrDefault(u => u.Id == id);
            if (user is null) return false;

            // Return any borrowed books to the catalog before removing the user
            if (BorrowedBooks.TryGetValue(id, out var borrowed))
            {
                Books.AddRange(borrowed);
                BorrowedBooks.Remove(id);
            }

            Users.Remove(user);
            SaveUsers();
            return true;
        }
    }

    // ---------- Borrow / Return ----------
    public bool BorrowBook(int bookId, int userId)
    {
        lock (_lock)
        {
            var book = Books.FirstOrDefault(b => b.Id == bookId);
            var user = Users.FirstOrDefault(u => u.Id == userId);
            if (book is null || user is null) return false;

            if (!BorrowedBooks.ContainsKey(userId))
                BorrowedBooks[userId] = new List<Book>();
            BorrowedBooks[userId].Add(book);
            Books.Remove(book);
            return true;
        }
    }

    public bool ReturnBook(int userId, int index)
    {
        lock (_lock)
        {
            if (!BorrowedBooks.TryGetValue(userId, out var list)) return false;
            if (index < 0 || index >= list.Count) return false;

            var book = list[index];
            list.RemoveAt(index);
            Books.Add(book);
            if (list.Count == 0) BorrowedBooks.Remove(userId);
            return true;
        }
    }

    // ---------- Persist ----------
    private void SaveBooks()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_booksPath)!);
        File.WriteAllLines(_booksPath,
            Books.Select(b => $"{b.Id},{b.Title},{b.Author},{b.ISBN}"));
    }

    private void SaveUsers()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_usersPath)!);
        File.WriteAllLines(_usersPath,
            Users.Select(u => $"{u.Id},{u.Name},{u.Email}"));
    }
}