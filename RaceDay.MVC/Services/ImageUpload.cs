using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace RaceDay.MVC.Services;

/// <summary>Checks an image before it is sent to the API. The API validates again; this gives quicker feedback.</summary>
public static class ImageUpload
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    /// <summary>Returns a user-facing problem, or null when the file is acceptable.</summary>
    public static string? Validate(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return "The image must be a JPG, PNG, WEBP or GIF file.";
        }
        if (file.Length > MaxBytes)
        {
            return "The image must be 5 MB or smaller.";
        }
        return null;
    }

    /// <summary>Wraps the file as multipart form data under the field name the API expects ("file").</summary>
    public static MultipartFormDataContent ToContent(IFormFile file, Stream stream)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", file.FileName);
        return content;
    }
}
