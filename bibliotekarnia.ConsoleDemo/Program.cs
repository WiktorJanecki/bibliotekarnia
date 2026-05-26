using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

// ─── Configuration ────────────────────────────────────────────────────────────
var baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";
const string AdminUsername = "admin";

string adminToken;
if (args.Length > 0)
{
    adminToken = args[0];
}
else
{
    Console.Write("Enter admin API token (printed on first run of the server): ");
    adminToken = Console.ReadLine()?.Trim() ?? string.Empty;
}

// ─── HTTP Client Setup ────────────────────────────────────────────────────────
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

var json = new JsonSerializerOptions { WriteIndented = true };
int createdAuthorId = 0, createdBookId = 0, createdMemberId = 0, createdLoanId = 0;

// ─── Helpers ─────────────────────────────────────────────────────────────────
void SetHeaders(string username, string token)
{
    http.DefaultRequestHeaders.Remove("X-Username");
    http.DefaultRequestHeaders.Remove("X-Api-Token");
    http.DefaultRequestHeaders.Add("X-Username", username);
    http.DefaultRequestHeaders.Add("X-Api-Token", token);
}

async Task RunStep(int step, string description, Func<Task> action)
{
    Console.WriteLine();
    Console.WriteLine("══════════════════════════════════════════");
    Console.WriteLine($"  Step {step}: {description}");
    Console.WriteLine("══════════════════════════════════════════");
    try { await action(); }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ERROR: {ex.Message}");
        Console.ResetColor();
    }
}

async Task<string?> SendAndPrint(HttpMethod method, string path, object? body = null, System.Net.HttpStatusCode expectedStatus = System.Net.HttpStatusCode.OK)
{
    Console.WriteLine($"  → {method.Method} {baseUrl}{path}");
    HttpResponseMessage response;

    if (body != null)
    {
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        response = await http.SendAsync(new HttpRequestMessage(method, path) { Content = content });
    }
    else
    {
        response = await http.SendAsync(new HttpRequestMessage(method, path));
    }

    var raw = await response.Content.ReadAsStringAsync();
    Console.Write($"  ← {(int)response.StatusCode} {response.ReasonPhrase}");

    string? prettyJson = null;
    try
    {
        var doc = JsonDocument.Parse(raw);
        prettyJson = JsonSerializer.Serialize(doc, json);
        Console.WriteLine();
        
        bool isExpected = response.StatusCode == expectedStatus || 
                         (expectedStatus == System.Net.HttpStatusCode.OK && (int)response.StatusCode >= 200 && (int)response.StatusCode < 300);
                         
        Console.ForegroundColor = isExpected ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(prettyJson);
        Console.ResetColor();
    }
    catch
    {
        Console.WriteLine($"  (non-JSON response: {raw})");
    }

    return prettyJson;
}

// ─── Demo Steps ───────────────────────────────────────────────────────────────
await RunStep(1, "Invalid token → expect 401", async () =>
{
    SetHeaders(AdminUsername, "invalid_token_xyz");
    await SendAndPrint(HttpMethod.Get, "/api/books", expectedStatus: System.Net.HttpStatusCode.Unauthorized);
});

SetHeaders(AdminUsername, adminToken);

await RunStep(2, "List all authors → expect 200", async () =>
{
    await SendAndPrint(HttpMethod.Get, "/api/authors");
});

await RunStep(3, "Create new author → expect 201", async () =>
{
    var result = await SendAndPrint(HttpMethod.Post, "/api/authors", new
    {
        FirstName = "Adam",
        LastName = "Mickiewicz",
        BirthYear = 1798,
        Nationality = "Polish"
    });
    if (result != null)
    {
        var doc = JsonDocument.Parse(result);
        createdAuthorId = doc.RootElement.GetProperty("id").GetInt32();
        Console.WriteLine($"  → Created author ID: {createdAuthorId}");
    }
});

await RunStep(4, "Create new book for that author → expect 201", async () =>
{
    var result = await SendAndPrint(HttpMethod.Post, "/api/books", new
    {
        Title = "Pan Tadeusz",
        PublishedYear = 1834,
        Genre = "Epic Poem",
        TotalCopies = 3,
        AuthorId = createdAuthorId
    });
    if (result != null)
    {
        var doc = JsonDocument.Parse(result);
        createdBookId = doc.RootElement.GetProperty("id").GetInt32();
        Console.WriteLine($"  → Created book ID: {createdBookId}");
    }
});

await RunStep(5, "Create a member → expect 201", async () =>
{
    var result = await SendAndPrint(HttpMethod.Post, "/api/members", new
    {
        FirstName = "Jan",
        LastName = "Kowalski",
        Email = $"jan.demo.{Guid.NewGuid().ToString("N").Substring(0, 8)}@test.com",
        MemberSince = DateTime.Today.AddYears(-1)
    });
    if (result != null)
    {
        var doc = JsonDocument.Parse(result);
        createdMemberId = doc.RootElement.GetProperty("id").GetInt32();
        Console.WriteLine($"  → Created member ID: {createdMemberId}");
    }
});

await RunStep(6, "Create a loan → expect 201", async () =>
{
    var result = await SendAndPrint(HttpMethod.Post, "/api/loans", new
    {
        BookId = createdBookId,
        MemberId = createdMemberId,
        DueDate = DateTime.UtcNow.AddDays(14)
    });
    if (result != null)
    {
        var doc = JsonDocument.Parse(result);
        createdLoanId = doc.RootElement.GetProperty("id").GetInt32();
        Console.WriteLine($"  → Created loan ID: {createdLoanId}");
    }
});

await RunStep(7, "List overdue loans → expect 200", async () =>
{
    await SendAndPrint(HttpMethod.Get, "/api/loans?status=overdue");
});

await RunStep(8, "Return the loan → expect 200", async () =>
{
    await SendAndPrint(HttpMethod.Put, $"/api/loans/{createdLoanId}/return");
});

await RunStep(9, "List returned loans → expect 200 with our loan", async () =>
{
    await SendAndPrint(HttpMethod.Get, "/api/loans?status=returned");
});

await RunStep(10, "Update author nationality → expect 200", async () =>
{
    await SendAndPrint(HttpMethod.Put, $"/api/authors/{createdAuthorId}", new
    {
        FirstName = "Adam",
        LastName = "Mickiewicz",
        BirthYear = 1798,
        Nationality = "Polish (Romantic Era)"
    });
});

await RunStep(11, "Delete the test book (no active loans) → expect 200", async () =>
{
    await SendAndPrint(HttpMethod.Delete, $"/api/books/{createdBookId}");
});

await RunStep(12, "Delete the test author (no books) → expect 200", async () =>
{
    await SendAndPrint(HttpMethod.Delete, $"/api/authors/{createdAuthorId}");
});

await RunStep(13, "Delete the demo member (no active loans) → expect 200", async () =>
{
    await SendAndPrint(HttpMethod.Delete, $"/api/members/{createdMemberId}");
});

Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════");
Console.WriteLine("  All demo steps completed.");
Console.WriteLine("══════════════════════════════════════════");
