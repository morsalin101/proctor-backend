using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Controllers;
using PROCTOR.Domain.Entities;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.API.Middleware;

public class AuditLogMiddleware
{
    private const int MaxBodyBytes = 64 * 1024;
    private const int MaxDetailsLength = 16 * 1024;
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "currentPassword", "newPassword", "confirmPassword", "token",
        "accessToken", "refreshToken", "authorization", "secret", "apiKey", "jwt"
    };
    private static readonly string[] NoisyReadPaths =
    {
        "/api/notifications/unread-count",
        "/api/notifications/category-counts",
        "/api/cases/my-cases/count"
    };

    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLogMiddleware> _logger;

    public AuditLogMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldAudit(context.Request))
        {
            await _next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        var details = await ReadDetailsAsync(context.Request);
        Exception? requestException = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            requestException = ex;
            throw;
        }
        finally
        {
            var statusCode = requestException is null
                ? context.Response.StatusCode
                : StatusCodes.Status500InternalServerError;
            await WriteAuditLogAsync(context, details, statusCode, started, requestException);
        }
    }

    private static bool ShouldAudit(HttpRequest request)
    {
        var path = request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.StartsWith("/api/audit-logs", StringComparison.OrdinalIgnoreCase)) return false;
        if (HttpMethods.IsGet(request.Method)
            && NoisyReadPaths.Any(item => path.Equals(item, StringComparison.OrdinalIgnoreCase))) return false;
        return true;
    }

    private async Task WriteAuditLogAsync(
        HttpContext context,
        string? details,
        int statusCode,
        long started,
        Exception? exception)
    {
        try
        {
            var endpoint = context.GetEndpoint();
            var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
            var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub")
                ?? context.User.FindFirstValue("id");
            var userName = context.User.FindFirstValue(ClaimTypes.Name)
                ?? context.User.FindFirstValue("name")
                ?? GetLoginIdentifier(details)
                ?? "Anonymous";
            var userRole = context.User.FindFirstValue(ClaimTypes.Role)
                ?? context.User.FindFirstValue("role")
                ?? "anonymous";

            var entityId = GetEntityId(context.Request.RouteValues);
            var action = actionDescriptor?.ActionName ?? GetFallbackAction(context.Request.Method);
            var entityType = actionDescriptor?.ControllerName ?? GetFallbackEntity(context.Request.Path);

            if (exception is not null)
            {
                var failure = new JsonObject
                {
                    ["request"] = ParseDetails(details),
                    ["errorType"] = exception.GetType().Name,
                    ["error"] = Truncate(exception.Message, 500)
                };
                details = Truncate(failure.ToJsonString(), MaxDetailsLength);
            }

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = Guid.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : null,
                UserName = Truncate(userName, 160),
                UserRole = Truncate(userRole, 80),
                Action = Truncate(action, 160),
                EntityType = Truncate(entityType, 120),
                EntityId = TruncateNullable(entityId, 160),
                HttpMethod = context.Request.Method,
                Path = Truncate(context.Request.Path.Value ?? string.Empty, 500),
                QueryString = SanitizeQuery(context.Request.Query),
                StatusCode = statusCode,
                Succeeded = statusCode is >= 200 and < 400,
                IpAddress = TruncateNullable(context.Connection.RemoteIpAddress?.ToString(), 64),
                UserAgent = Truncate(context.Request.Headers.UserAgent.ToString(), 500),
                DurationMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                DetailsJson = TruncateNullable(details, MaxDetailsLength),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ProctorDbContext>();
            db.AuditLogs.Add(auditLog);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Auditing must never turn a successful application request into a failure.
            _logger.LogError(ex, "Could not persist API audit log for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
    }

    private static async Task<string?> ReadDetailsAsync(HttpRequest request)
    {
        var result = new JsonObject();
        if (request.RouteValues.Count > 0)
        {
            var route = new JsonObject();
            foreach (var (key, value) in request.RouteValues)
                route[key] = value?.ToString();
            result["route"] = route;
        }

        if (request.ContentLength is > 0)
        {
            if (request.HasJsonContentType() && request.ContentLength <= MaxBodyBytes)
            {
                request.EnableBuffering();
                using var reader = new StreamReader(request.Body, leaveOpen: true);
                var raw = await reader.ReadToEndAsync();
                request.Body.Position = 0;
                try
                {
                    var body = JsonNode.Parse(raw);
                    Redact(body);
                    result["body"] = body;
                }
                catch (JsonException)
                {
                    result["body"] = "[unparseable JSON omitted]";
                }
            }
            else
            {
                result["upload"] = new JsonObject
                {
                    ["contentType"] = request.ContentType,
                    ["contentLength"] = request.ContentLength
                };
            }
        }

        return result.Count == 0 ? null : Truncate(result.ToJsonString(), MaxDetailsLength);
    }

    private static void Redact(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (SensitiveNames.Contains(property.Key)) obj[property.Key] = "[REDACTED]";
                else Redact(property.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array) Redact(child);
        }
        else if (node is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > 1000)
        {
            value.ReplaceWith(JsonValue.Create(Truncate(text, 1000) + "…"));
        }
    }

    private static string? SanitizeQuery(IQueryCollection query)
    {
        if (query.Count == 0) return null;
        var safe = new Dictionary<string, string>();
        foreach (var (key, value) in query)
            safe[key] = SensitiveNames.Contains(key) ? "[REDACTED]" : Truncate(value.ToString(), 500);
        return Truncate(JsonSerializer.Serialize(safe), 4000);
    }

    private static string? GetEntityId(RouteValueDictionary values)
    {
        foreach (var key in new[] { "id", "caseId", "reportId", "hearingId", "resolutionId", "attachmentId" })
            if (values.TryGetValue(key, out var value) && value is not null) return value.ToString();
        return null;
    }

    private static string GetFallbackAction(string method) => method.ToUpperInvariant() switch
    {
        "POST" => "Create",
        "PUT" or "PATCH" => "Update",
        "DELETE" => "Delete",
        _ => "View"
    };

    private static string GetFallbackEntity(PathString path) =>
        path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() ?? "API";

    private static JsonNode? ParseDetails(string? details)
    {
        if (string.IsNullOrWhiteSpace(details)) return null;
        try { return JsonNode.Parse(details); }
        catch (JsonException) { return JsonValue.Create(details); }
    }

    private static string? GetLoginIdentifier(string? details)
    {
        try
        {
            return JsonNode.Parse(details ?? "")?["body"]?["email"]?.GetValue<string>();
        }
        catch { return null; }
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
    private static string? TruncateNullable(string? value, int length) => value is null ? null : Truncate(value, length);
}
