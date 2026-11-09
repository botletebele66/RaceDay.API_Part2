using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API_Part2.Services;

/// <summary>
/// Stores images in Azure Blob Storage when <c>Azure:Storage:ConnectionString</c> is configured.
/// Without a connection string it writes to <c>wwwroot/uploads</c>, so the project runs out of the box in development.
/// </summary>
public sealed class AzureBlobService : IAzureBlobService
{
    /// <summary>Largest accepted image (5 MB).</summary>
    public const long MaxImageBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
    };

    private readonly BlobContainerClient? _container;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AzureBlobService> _log;
    private bool _containerReady;

    public AzureBlobService(IConfiguration configuration, IWebHostEnvironment env, ILogger<AzureBlobService> log)
    {
        _env = env;
        _log = log;

        var connectionString = configuration["Azure:Storage:ConnectionString"];
        var containerName = configuration["Azure:Storage:Container"] ?? "event-images";

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _container = new BlobContainerClient(connectionString, containerName);
            _log.LogInformation("Image storage: Azure Blob container '{Container}'.", containerName);
        }
        else
        {
            _log.LogWarning("Azure:Storage:ConnectionString is not set. Images are stored in wwwroot/uploads.");
        }
    }

    public async Task<string> UploadImageAsync(Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedTypes.TryGetValue(extension, out var mimeType))
        {
            throw new InvalidImageException("Images must be JPG, PNG, WEBP or GIF.");
        }

        // Read into memory so the size and file signature are checked before anything is stored.
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        if (buffer.Length == 0 || buffer.Length > MaxImageBytes)
        {
            throw new InvalidImageException("Images must be between 1 byte and 5 MB.");
        }

        var bytes = buffer.GetBuffer();
        if (!HasImageSignature(bytes, (int)buffer.Length, extension))
        {
            throw new InvalidImageException("The file does not look like a valid image.");
        }

        buffer.Position = 0;
        var safeFolder = folder.Trim('/');
        var blobName = $"{safeFolder}/{Guid.NewGuid():N}{extension}";

        if (_container is not null)
        {
            await EnsureContainerAsync(ct);
            var client = _container.GetBlobClient(blobName);
            await client.UploadAsync(buffer, new BlobHttpHeaders { ContentType = mimeType }, cancellationToken: ct);
            return client.Uri.ToString();
        }

        // Local development fallback.
        var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var localPath = Path.Combine(root, "uploads", safeFolder, Path.GetFileName(blobName));
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await using (var file = File.Create(localPath))
        {
            await buffer.CopyToAsync(file, ct);
        }
        return $"/uploads/{blobName}";
    }

    public async Task DeleteAsync(string? url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var absolute) &&
                absolute.Host.EndsWith(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                await DeleteBlobAsync(absolute, ct);
                return;
            }

            var path = absolute?.AbsolutePath ?? url;
            if (path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var root = Path.GetFullPath(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"));
                var fullPath = Path.GetFullPath(Path.Combine(root, path.TrimStart('/')));
                // Never delete anything outside the uploads folder.
                if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failed clean-up must not fail the request that replaced the image.
            _log.LogWarning(ex, "Could not delete the previous image.");
        }
    }

    private async Task DeleteBlobAsync(Uri uri, CancellationToken ct)
    {
        if (_container is null)
        {
            return;
        }

        // Path is /{container}/{blob name}; the blob name may contain folders.
        var parts = uri.AbsolutePath.TrimStart('/').Split('/', 2);
        if (parts.Length == 2)
        {
            await _container.GetBlobClient(Uri.UnescapeDataString(parts[1])).DeleteIfExistsAsync(cancellationToken: ct);
        }
    }

    private async Task EnsureContainerAsync(CancellationToken ct)
    {
        if (_containerReady || _container is null)
        {
            return;
        }
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
        _containerReady = true;
    }

    /// <summary>Checks the first bytes of the file so that renamed non-image files are rejected.</summary>
    private static bool HasImageSignature(byte[] bytes, int length, string extension)
    {
        bool StartsWith(params byte[] signature) => length >= signature.Length && bytes.Take(signature.Length).SequenceEqual(signature);

        return extension switch
        {
            ".jpg" or ".jpeg" => StartsWith(0xFF, 0xD8, 0xFF),
            ".png" => StartsWith(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
            ".gif" => StartsWith(0x47, 0x49, 0x46, 0x38),
            ".webp" => length >= 12 && StartsWith(0x52, 0x49, 0x46, 0x46) && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50,
            _ => false,
        };
    }
}
