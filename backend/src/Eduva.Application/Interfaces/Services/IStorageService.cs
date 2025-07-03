namespace Eduva.Application.Interfaces.Services
{
    public interface IStorageService
    {
        Task<ICollection<string>> GenerateUploadSasTokens(List<string> blobNames);
        string GetReadableUrl(string blobUrl);
        Task DeleteFileAsync(string blobName);
        Task DeleteRangeFileAsync(List<string> blobNames);
    }
}
