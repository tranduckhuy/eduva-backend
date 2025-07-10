using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Eduva.Application.Exceptions.FileStorage;
using Eduva.Application.Interfaces.Services;
using Eduva.Infrastructure.Configurations;
using Microsoft.AspNetCore.Http;

namespace Eduva.Infrastructure.Services
{
    public class AzureBlobStorageService : IStorageService
    {
        private readonly AzureBlobStorageOptions _options;
        private readonly BlobContainerClient _containerClient;
        private readonly BlobContainerClient _tempContainerClient;
        private readonly IStorageQuotaService _storageQuotaService;

        public AzureBlobStorageService(AzureBlobStorageOptions options, IStorageQuotaService storageQuotaService)
        {
            _options = options;
            _storageQuotaService = storageQuotaService;
            var blobServiceClient = new BlobServiceClient(_options.ConnectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            _tempContainerClient = blobServiceClient.GetBlobContainerClient(_options.TemporaryContainerName);
        }

        private string GenerateUploadSasToken(string blobName, DateTimeOffset expiresOn)
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

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            var encodedBlobName = Uri.EscapeDataString(blobName);

            var baseUri = $"{sasUri.Scheme}://{sasUri.Host}";
            var finalUri = $"{baseUri}/{_options.ContainerName}/{encodedBlobName}{sasUri.Query}";

            return finalUri;
        }

        public async Task<ICollection<string>> GenerateUploadSasTokens(List<string> blobNames)
        {
            var sasTokens = new List<string>();
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);
            foreach (var blobName in blobNames)
            {
                var sasToken = GenerateUploadSasToken(blobName, expiresOn);
                sasTokens.Add(sasToken);
            }
            return await Task.FromResult(sasTokens);
        }

        public string GetReadableUrl(string blobUrl)
        {
            var blobName = GetBlobNameFromUrl(blobUrl);
            var sasToken = GenerateReadSasToken(blobName, DateTimeOffset.UtcNow.AddHours(1));
            return $"{blobUrl}{sasToken}";
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

        public async Task DeleteRangeFileAsync(List<string> blobNames)
        {
            foreach (var blobName in blobNames)
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                var response = await blobClient.DeleteIfExistsAsync();
                if (!response.Value)
                {
                    throw new BlobNotFoundException();
                }
            }
        }

        public async Task<ICollection<string>> GenerateUploadSasTokensWithQuotaCheck(List<string> blobNames, List<long> fileSizes, int schoolId)
        {
            // Validate file sizes match blob names
            if (blobNames.Count != fileSizes.Count)
            {
                throw new ArgumentException("The number of blob names must match the number of file sizes");
            }

            // Validate storage quota before generating SAS tokens
            await _storageQuotaService.ValidateUploadQuotaAsync(schoolId, fileSizes);

            // Generate SAS tokens if quota check passes
            return await GenerateUploadSasTokens(blobNames);
        }

        public async Task<string> UploadFileToTempContainerAsync(IFormFile file, string blobName)
        {
            var blobClient = _tempContainerClient.GetBlobClient(blobName);
            
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }
            
            return blobClient.Uri.ToString();
        }

        private static string GetBlobNameFromUrl(string blobUrl)
        {
            var uri = new Uri(blobUrl);
            var encodedBlobName = uri.Segments[^1];
            return Uri.UnescapeDataString(encodedBlobName);
        }
    }
}
