using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BookStore.API.Middleware;

public class CorrelationIdMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Add to response header
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderName] = correlationId.ToString();
            return Task.CompletedTask;
        });

        // Add to logging scope so all logs include correlationId
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = correlationId
               }))
        {
            await _next(context);
        }
    }

    private static Guid GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValues) &&
            Guid.TryParse(headerValues.FirstOrDefault(), out var parsed))
        {
            return parsed;
        }

        // Fallback to existing TraceIdentifier if it's a Guid, otherwise create one
        if (Guid.TryParse(context.TraceIdentifier, out var fromTrace))
        {
            return fromTrace;
        }

        var correlationId = Guid.NewGuid();
        context.TraceIdentifier = correlationId.ToString();
        return correlationId;
    }
}

