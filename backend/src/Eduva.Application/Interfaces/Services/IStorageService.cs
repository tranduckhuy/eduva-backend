using Microsoft.AspNetCore.Http;

namespace Eduva.Application.Interfaces.Services
{
    public interface IStorageService
    {
        ICollection<string> GenerateUploadSasTokens(List<string> blobNames, int schoolId);
        Task<ICollection<string>> GenerateUploadSasTokensWithQuotaCheck(List<string> blobNames, List<long> fileSizes, int schoolId);
        string GetReadableUrl(string blobUrl);
        (string blobNameUrl, string readableUrl) GetReadableUrlFromBlobName(string blobName);
        Task DeleteFileAsync(string blobName);
        Task DeleteRangeFileAsync(List<string> blobNames);
        Task DeleteRangeTempFileAsync(List<string> blobNames);
        Task<string> UploadFileToTempContainerAsync(IFormFile file, string blobName);
    }
}
