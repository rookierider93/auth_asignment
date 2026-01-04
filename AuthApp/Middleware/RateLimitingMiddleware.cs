using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace AuthApp.Middleware;

// Very small in-memory rate limiter for login endpoints (IP-based)
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> Attempts = new();

    // Limit settings
    private const int MAX_ATTEMPTS = 5;
    private static readonly TimeSpan WINDOW = TimeSpan.FromMinutes(5);

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path.Contains("/account/login") || path.Contains("/local/login"))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;
            var entry = Attempts.GetOrAdd(ip, _ => (0, now));
            if (now - entry.WindowStart > WINDOW)
            {
                entry = (0, now);
            }

            if (entry.Count >= MAX_ATTEMPTS)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too many login attempts. Try again later.");
                return;
            }

            // If this is a POST to login, increment count
            if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                Attempts[ip] = (entry.Count + 1, entry.WindowStart);
            }
        }

        await _next(context);
    }
}

public class LoginAttemptTracker { }
