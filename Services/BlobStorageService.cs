using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EventEase.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureBlobStorage:ConnectionString"]
                ?? configuration["AzureStorage:ConnectionString"]
                ?? configuration.GetConnectionString("AzureStorage")
                ?? "UseDevelopmentStorage=true";
            var containerName = configuration["AzureBlobStorage:ContainerName"]
                ?? configuration["AzureStorage:ContainerName"]
                ?? "venue-images";

            var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2021_12_02);
            _containerClient = new BlobContainerClient(connectionString, containerName, options);
        }

        public async Task<string?> UploadAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only image files can be uploaded.");
            }

            await _containerClient.CreateIfNotExistsAsync();

            var extension = Path.GetExtension(file.FileName);
            var blobName = $"{Guid.NewGuid():N}{extension}";
            var blobClient = _containerClient.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: false);

            return blobName;
        }

        public async Task<BlobDownloadResult?> DownloadAsync(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return null;
            }

            var blobName = GetBlobName(imagePath);
            var blobClient = _containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            return await blobClient.DownloadContentAsync();
        }

        private static string GetBlobName(string imagePath)
        {
            if (Uri.TryCreate(imagePath, UriKind.Absolute, out var uri))
            {
                return Path.GetFileName(uri.LocalPath);
            }

            return imagePath;
        }
    }
}
