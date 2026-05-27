# Bibliotekarnia — Library Management System
## Description

**Bibliotekarnia** is a web-based library management system that allows library staff to manage books, authors, library members, and book loans. The application provides both a web user interface (for day-to-day staff use) and a REST API (for programmatic access and integrations).

---

## Features

### Web UI (session-based login)
- **Dashboard** — real-time statistics: total books, members, active/overdue loans; top 5 most popular books; top 5 most active members; books-by-genre chart; loans-per-month chart; overdue loan alert table
- **Books** — list, filter by genre, view details (availability, loan history), create, edit, delete
- **Authors** — list, view per-author book list, create, edit, delete
- **Members** — register library patrons, view loan history, create, edit, delete
- **Loans** — create new loans (with copy-availability check), filter by status (All / Active / Overdue / Returned), return books
- **User Management** (admin only) — create staff accounts, view/copy API tokens, regenerate tokens, delete users

### REST API (token-based auth)
Full CRUD for Authors, Books, Members, and Loans via `/api/*` endpoints. Authenticated by `X-Username` and `X-Api-Token` request headers.

---

## Getting Started

### Prerequisites
- .NET 8 SDK

### Run the server

```bash
cd bibliotekarnia
dotnet run
```

On first run the application:
1. Creates the SQLite database (`library.db`) automatically
2. Runs EF Core migrations
3. Seeds sample data (3 authors, 5 books, 4 members, 3 loans)
4. Creates an admin account and prints credentials to the console:

```
===========================================
First run: admin user created.
  Username : admin
  Password : Admin123!
  ApiToken : <32-char hex token>
===========================================
```

### Access the application

Open your browser at: **http://localhost:5000**

Log in with: `admin` / `Admin123!`

Swagger UI (REST API docs): **http://localhost:5000/swagger**

---

## Default Credentials

| Field | Value |
|---|---|
| Username | `admin` |
| Password | `Admin123!` |
| API Token | Printed on first startup |

---

## REST API Usage

All `/api/*` endpoints require two headers:

```
X-Username: admin
X-Api-Token: <token from Users page or first-run console output>
```

### Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/authors` | List all authors |
| GET | `/api/authors/{id}` | Get author with books |
| POST | `/api/authors` | Create author |
| PUT | `/api/authors/{id}` | Update author |
| DELETE | `/api/authors/{id}` | Delete author (must have no books) |
| GET | `/api/books` | List all books with availability |
| GET | `/api/books/{id}` | Get book detail |
| POST | `/api/books` | Create book |
| PUT | `/api/books/{id}` | Update book |
| DELETE | `/api/books/{id}` | Delete book (must have no active loans) |
| GET | `/api/members` | List all members |
| GET | `/api/members/{id}` | Get member detail |
| POST | `/api/members` | Register member |
| PUT | `/api/members/{id}` | Update member |
| DELETE | `/api/members/{id}` | Delete member (must have no active loans) |
| GET | `/api/loans?status=active\|overdue\|returned` | List loans (filtered) |
| GET | `/api/loans/{id}` | Get loan detail |
| POST | `/api/loans` | Create loan |
| PUT | `/api/loans/{id}/return` | Mark loan as returned |
| DELETE | `/api/loans/{id}` | Delete returned loan |

### Example: Create a loan

```http
POST /api/loans
X-Username: admin
X-Api-Token: your_token_here
Content-Type: application/json

{
  "bookId": 1,
  "memberId": 2,
  "dueDate": "2026-06-15T00:00:00Z"
}
```

---

## Running the Console Demo

Start the web server first, then:

```bash
cd bibliotekarnia.ConsoleDemo
dotnet run
```

The demo will prompt for the admin API token and then run 13 sequential steps demonstrating all REST API operations.

---

## Database

SQLite database file: `bibliotekarnia/library.db`

Tables:
- `Users` — staff accounts (auth, not counted toward minimum)
- `Authors` — book authors
- `Books` — library catalog
- `Members` — library patrons
- `Loans` — borrowing records

---

## Project Structure

```
bibliotekarnia.sln
├── bibliotekarnia/          ASP.NET Core MVC + REST API
│   ├── Controllers/         MVC controllers (web UI)
│   ├── Api/                 REST API controllers + DTOs
│   ├── Data/                DbContext + seeder
│   ├── Filters/             Auth action filters
│   ├── Middleware/          API token middleware
│   ├── Models/              EF Core entity models
│   ├── Services/            LoanService (business logic)
│   ├── ViewModels/          View model classes
│   └── Views/               Razor views
└── bibliotekarnia.ConsoleDemo/   REST API demonstration console app
```
