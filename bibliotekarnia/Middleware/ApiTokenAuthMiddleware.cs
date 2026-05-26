using bibliotekarnia.Data;
using Microsoft.EntityFrameworkCore;

namespace bibliotekarnia.Middleware;

public class ApiTokenAuthMiddleware
{
    private readonly RequestDelegate _next;

    public ApiTokenAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, LibraryDbContext db)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var username = context.Request.Headers["X-Username"].FirstOrDefault();
            var token = context.Request.Headers["X-Api-Token"].FirstOrDefault();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Missing X-Username or X-Api-Token header." });
                return;
            }

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.ApiToken == token);

            if (user == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid credentials." });
                return;
            }

            context.Items["ApiUser"] = user;
        }

        await _next(context);
    }
}
