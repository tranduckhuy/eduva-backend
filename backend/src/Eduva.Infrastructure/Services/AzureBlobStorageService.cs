using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Eduva.Application.Exceptions.FileStorage;
using Eduva.Application.Interfaces.Services;
using Eduva.Infrastructure.Configurations;

namespace Eduva.Infrastructure.Services
{
    public class AzureBlobStorageService : IStorageService
    {
        private readonly AzureBlobStorageOptions _options;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageService(AzureBlobStorageOptions options)
        {
            _options = options;
            _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
            _containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        }

        public Task<string> GenerateUploadSasToken(string blobName, DateTimeOffset expiresOn)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _options.ContainerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = expiresOn
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

            return Task.FromResult(blobClient.GenerateSasUri(sasBuilder).ToString());
        }

        public string GetReadableUrl(string blobUrl)
        {
            var blobName = GetBlobNameFromUrl(blobUrl);
            return $"{blobUrl}?{GenerateReadSasToken(blobName, DateTimeOffset.UtcNow.AddHours(1))}";
        }

        private string GenerateReadSasToken(string blobName, DateTimeOffset expiresOn)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _options.ContainerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = expiresOn
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).Query;
        }

        public async Task DeleteFileAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DeleteIfExistsAsync();
            if (!response.Value)
            {
                throw new BlobNotFoundException();
            }
        }

        private string GetBlobNameFromUrl(string blobUrl)
        {
            var uri = new Uri(blobUrl);
            return uri.Segments[^1];
        }
    }
}
