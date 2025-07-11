using Microsoft.AspNetCore.Http;

namespace Eduva.Application.Interfaces.Services
{
    public interface IStorageService
    {
        Task<ICollection<string>> GenerateUploadSasTokens(List<string> blobNames);
        Task<ICollection<string>> GenerateUploadSasTokensWithQuotaCheck(List<string> blobNames, List<long> fileSizes, int schoolId);
        string GetReadableUrl(string blobUrl);
        Task DeleteFileAsync(string blobName);
        Task DeleteRangeFileAsync(List<string> blobNames);
        Task<string> UploadFileToTempContainerAsync(IFormFile file, string blobName);
    }
}
