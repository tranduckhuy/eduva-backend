namespace Eduva.Application.Interfaces.Services
{
    public interface IStorageService
    {
        Task<string> GenerateUploadSasToken(string blobName, DateTimeOffset expiresOn);
        string GetReadableUrl(string blobUrl);
        Task DeleteFileAsync(string blobName);
    }
}
