using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;

namespace RaceDay.MVC.Services;

/// <summary>
/// The only route from the MVC front end to the RaceDay API. It forwards the user's API session cookie on every call,
/// stores any refreshed cookie, and converts failures into <see cref="ApiResult"/> values instead of exceptions.
/// </summary>
public sealed class ApiClient
{
    /// <summary>Name of the API's session cookie.</summary>
    public const string ApiSessionCookieName = ".RaceDay.Session";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession? Session => _accessor.HttpContext?.Features.Get<ISessionFeature>()?.Session;

    public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, ct);

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Post, path, body is null ? null : JsonContent.Create(body, options: JsonOptions), ct);

    public Task<ApiResult<T>> PutAsync<T>(string path, object body, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Put, path, JsonContent.Create(body, options: JsonOptions), ct);

    public Task<ApiResult<T>> PostMultipartAsync<T>(string path, MultipartFormDataContent content, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Post, path, content, ct);

    public Task<ApiResult<object>> DeleteAsync(string path, CancellationToken ct = default) =>
        SendAsync<object>(HttpMethod.Delete, path, null, ct);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };

        var sessionCookie = Session?.GetString(MvcSessionKeys.ApiCookie);
        if (!string.IsNullOrWhiteSpace(sessionCookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", sessionCookie);
        }

        try
        {
            using var response = await _http.SendAsync(request, ct);
            RefreshSessionCookie(response);

            var text = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<T>.Failure(response.StatusCode, text);
            }

            if (response.StatusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(text))
            {
                return ApiResult<T>.Success(response.StatusCode, default);
            }

            return ApiResult<T>.Success(response.StatusCode, JsonSerializer.Deserialize<T>(text, JsonOptions));
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Failure(HttpStatusCode.ServiceUnavailable, null);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            // The HTTP client timed out (the caller did not cancel).
            return ApiResult<T>.Failure(HttpStatusCode.ServiceUnavailable, null);
        }
        catch (JsonException)
        {
            return ApiResult<T>.Failure(HttpStatusCode.BadGateway, null);
        }
    }

    /// <summary>Keeps the stored API cookie current. An empty value (after logout) removes it.</summary>
    private void RefreshSessionCookie(HttpResponseMessage response)
    {
        var session = Session;
        if (session is null || !response.Headers.TryGetValues("Set-Cookie", out var headers))
        {
            return;
        }

        foreach (var header in headers)
        {
            var pair = header.Split(';')[0].Trim();
            if (!pair.StartsWith(ApiSessionCookieName + "=", StringComparison.Ordinal))
            {
                continue;
            }

            if (pair.Length > ApiSessionCookieName.Length + 1)
            {
                session.SetString(MvcSessionKeys.ApiCookie, pair);
            }
            else
            {
                session.Remove(MvcSessionKeys.ApiCookie);
            }
        }
    }
}
