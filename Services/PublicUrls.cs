using Microsoft.AspNetCore.Http;

namespace RaceDay.API_Part2.Services;

/// <summary>Turns storage locations into absolute URLs that a browser can open.</summary>
public static class PublicUrls
{
    /// <summary>
    /// Azure URLs are returned unchanged. Local uploads are prefixed with <c>App:PublicBaseUrl</c>,
    /// or with the scheme and host of the current request when that setting is empty.
    /// </summary>
    public static string? ToPublic(string? storedUrl, IConfiguration configuration, HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(storedUrl) || storedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return storedUrl;
        }

        var baseUrl = configuration["App:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = $"{request.Scheme}://{request.Host}";
        }
        return baseUrl + storedUrl;
    }
}
