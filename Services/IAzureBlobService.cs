namespace RaceDay.API_Part2.Services;

/// <summary>
/// Stores image files for the API. Controllers call this interface only; the MVC front end never talks to storage.
/// </summary>
public interface IAzureBlobService
{
    /// <summary>
    /// Validates and stores an image and returns where it can be read.
    /// Azure URLs are absolute; the local development fallback returns a path beginning with /uploads/.
    /// </summary>
    /// <exception cref="InvalidImageException">The file is not an accepted image.</exception>
    Task<string> UploadImageAsync(Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default);

    /// <summary>Deletes a previously stored image. Missing files are ignored.</summary>
    Task DeleteAsync(string? url, CancellationToken ct = default);
}

/// <summary>Raised when an uploaded file is not an accepted image. The message is safe to show to the user.</summary>
public sealed class InvalidImageException : Exception
{
    public InvalidImageException(string message) : base(message) { }
}
