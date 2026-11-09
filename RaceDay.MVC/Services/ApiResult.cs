using System.Net;
using System.Text.Json;

namespace RaceDay.MVC.Services;

/// <summary>
/// The outcome of one call to the API. Failures keep the status code; the raw response body is never shown to users.
/// </summary>
public class ApiResult
{
    public HttpStatusCode Status { get; init; }
    public string? RawBody { get; init; }

    public bool Ok => (int)Status is >= 200 and < 300;
    public bool IsUnauthorized => Status == HttpStatusCode.Unauthorized;
    public bool IsNotFound => Status == HttpStatusCode.NotFound;

    /// <summary>A short, friendly message that is safe to show on a page.</summary>
    public string Message => Ok ? string.Empty : ProblemMessage.From(RawBody, Status);
}

public sealed class ApiResult<T> : ApiResult
{
    public T? Value { get; init; }

    public static ApiResult<T> Success(HttpStatusCode status, T? value) => new() { Status = status, Value = value };

    public static ApiResult<T> Failure(HttpStatusCode status, string? rawBody) => new() { Status = status, RawBody = rawBody };
}

/// <summary>Reads the RFC 7807 problem details that the API returns and turns them into a readable sentence.</summary>
internal static class ProblemMessage
{
    public static string From(string? body, HttpStatusCode status)
    {
        var detail = ReadDetail(body) ?? FirstValidationError(body);
        if (!string.IsNullOrWhiteSpace(detail))
        {
            return detail;
        }

        return status switch
        {
            HttpStatusCode.BadRequest => "Please check the form and try again.",
            HttpStatusCode.Unauthorized => "Please sign in to continue.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            HttpStatusCode.Conflict => "That conflicts with existing details. Check and try again.",
            HttpStatusCode.ServiceUnavailable => "RaceDay is unreachable right now. Please try again shortly.",
            _ => "Something went wrong. Please try again.",
        };
    }

    private static string? ReadDetail(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("detail", out var detail) &&
                detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString();
            }
        }
        catch (JsonException)
        {
            // Not JSON: fall through to the generic message.
        }
        return null;
    }

    /// <summary>Validation failures from model binding arrive as { "errors": { "Field": [ "message" ] } }.</summary>
    private static string? FirstValidationError(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var field in errors.EnumerateObject())
                {
                    if (field.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var message in field.Value.EnumerateArray())
                        {
                            if (message.ValueKind == JsonValueKind.String)
                            {
                                return message.GetString();
                            }
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed bodies.
        }
        return null;
    }
}
