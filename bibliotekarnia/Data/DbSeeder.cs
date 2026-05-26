using bibliotekarnia.Models;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Data;

public static class DbSeeder
{
    public static void Seed(IServiceProvider services)
    {
        var db = services.GetRequiredService<LibraryDbContext>();
        db.Database.Migrate();

        if (!db.Users.Any())
        {
            var adminToken = Guid.NewGuid().ToString("N");
            db.Users.Add(new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                ApiToken = adminToken,
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();

            Console.WriteLine("===========================================");
            Console.WriteLine("First run: admin user created.");
            Console.WriteLine("  Username : admin");
            Console.WriteLine("  Password : Admin123!");
            Console.WriteLine($"  ApiToken : {adminToken}");
            Console.WriteLine("===========================================");
        }

        if (!db.Authors.Any())
        {
            var tolkien = new Author { FirstName = "J.R.R.", LastName = "Tolkien", BirthYear = 1892, Nationality = "British" };
            var herbert = new Author { FirstName = "Frank", LastName = "Herbert", BirthYear = 1920, Nationality = "American" };
            var sapkowski = new Author { FirstName = "Andrzej", LastName = "Sapkowski", BirthYear = 1948, Nationality = "Polish" };
            db.Authors.AddRange(tolkien, herbert, sapkowski);
            db.SaveChanges();

            var admin = db.Users.First();
            var books = new List<Book>
            {
                new Book { Title = "The Lord of the Rings", ISBN = "9780618640157", PublishedYear = 1954, Genre = "Fantasy", TotalCopies = 3, AuthorId = tolkien.Id },
                new Book { Title = "The Hobbit", ISBN = "9780261102217", PublishedYear = 1937, Genre = "Fantasy", TotalCopies = 4, AuthorId = tolkien.Id },
                new Book { Title = "Dune", ISBN = "9780441013593", PublishedYear = 1965, Genre = "Science Fiction", TotalCopies = 2, AuthorId = herbert.Id },
                new Book { Title = "The Witcher: Blood of Elves", ISBN = "9780575082441", PublishedYear = 1994, Genre = "Fantasy", TotalCopies = 3, AuthorId = sapkowski.Id },
                new Book { Title = "The Witcher: Time of Contempt", ISBN = "9780575084841", PublishedYear = 1995, Genre = "Fantasy", TotalCopies = 2, AuthorId = sapkowski.Id },
            };
            db.Books.AddRange(books);
            db.SaveChanges();

            var members = new List<Member>
            {
                new Member { FirstName = "Anna", LastName = "Kowalska", Email = "anna.kowalska@example.com", Phone = "123-456-789", MemberSince = DateTime.UtcNow.AddYears(-2) },
                new Member { FirstName = "Piotr", LastName = "Nowak", Email = "piotr.nowak@example.com", MemberSince = DateTime.UtcNow.AddYears(-1) },
                new Member { FirstName = "Maria", LastName = "Wiśniewska", Email = "maria.wisniewska@example.com", Phone = "987-654-321", MemberSince = DateTime.UtcNow.AddMonths(-6) },
                new Member { FirstName = "Jan", LastName = "Wójcik", Email = "jan.wojcik@example.com", MemberSince = DateTime.UtcNow.AddMonths(-3) },
            };
            db.Members.AddRange(members);
            db.SaveChanges();

            var now = DateTime.UtcNow;
            var loans = new List<Loan>
            {
                // Active loan
                new Loan
                {
                    BookId = books[0].Id,
                    MemberId = members[0].Id,
                    LoanedAt = now.AddDays(-7),
                    DueDate = now.AddDays(7),
                    LoanedByUserId = admin.Id
                },
                // Returned loan
                new Loan
                {
                    BookId = books[2].Id,
                    MemberId = members[1].Id,
                    LoanedAt = now.AddDays(-30),
                    DueDate = now.AddDays(-16),
                    ReturnedAt = now.AddDays(-20),
                    LoanedByUserId = admin.Id
                },
                // Overdue loan
                new Loan
                {
                    BookId = books[3].Id,
                    MemberId = members[2].Id,
                    LoanedAt = now.AddDays(-21),
                    DueDate = now.AddDays(-7),
                    LoanedByUserId = admin.Id
                },
            };
            db.Loans.AddRange(loans);
            db.SaveChanges();
        }
    }
}
