using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayAPI.Services;

// Wraps Azure Blob Storage so controllers never touch the Azure SDK directly.
// The MVC app never talks to Azure - it sends files to the API, and the API stores them here.
public interface IBlobStorageService
{
    /// <summary>Uploads an image and returns its public URL.</summary>
    Task<string> UploadImageAsync(IFormFile file, string folder);
}

public class BlobStorageService : IBlobStorageService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private readonly BlobContainerClient? _container;
    private bool _containerReady;

    public BlobStorageService(IConfiguration config)
    {
        string? connectionString = config["AzureBlob:ConnectionString"];
        string containerName     = config["AzureBlob:ContainerName"] ?? "raceday-images";

        // Not configured = uploads are unavailable, but the rest of the API still works.
        if (!string.IsNullOrWhiteSpace(connectionString))
            _container = new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        if (_container == null)
            throw new InvalidOperationException("Image storage is not configured.");
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file was uploaded.");
        if (file.Length > MaxBytes)
            throw new ArgumentException("The image must be 5 MB or smaller.");
        if (!AllowedTypes.Contains(file.ContentType))
            throw new ArgumentException("Only JPEG, PNG, WEBP or GIF images are allowed.");

        // Create the container on first use; blobs are publicly readable so the
        // browser can display them directly from the URL we return.
        if (!_containerReady)
        {
            await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);
            _containerReady = true;
        }

        // Unique name so two users uploading "photo.jpg" never overwrite each other
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string blobName  = $"{folder}/{Guid.NewGuid():N}{extension}";

        var blob = _container.GetBlobClient(blobName);
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blob.Uri.ToString();
    }
}
