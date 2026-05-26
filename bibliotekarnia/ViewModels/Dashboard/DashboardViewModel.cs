namespace bibliotekarnia.ViewModels.Dashboard;

public class DashboardViewModel
{
    public int TotalBooks { get; set; }
    public int TotalMembers { get; set; }
    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }

    public List<PopularBookItem> TopBooks { get; set; } = new();
    public List<ActiveMemberItem> TopMembers { get; set; } = new();
    public List<GenreCountItem> GenreCounts { get; set; } = new();
    public List<MonthlyLoanItem> MonthlyLoans { get; set; } = new();
    public List<OverdueLoanItem> OverdueLoanList { get; set; } = new();
}

public record PopularBookItem(int BookId, string Title, string AuthorName, int LoanCount);
public record ActiveMemberItem(int MemberId, string FullName, int LoanCount);
public record GenreCountItem(string Genre, int Count);
public record MonthlyLoanItem(int Year, int Month, int Count);
public record OverdueLoanItem(int LoanId, string BookTitle, string MemberName, DateTime DueDate, int DaysOverdue);
