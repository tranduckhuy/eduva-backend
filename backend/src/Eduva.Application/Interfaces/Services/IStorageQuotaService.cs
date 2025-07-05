using Eduva.Application.Features.StorageQuota;

namespace Eduva.Application.Interfaces.Services
{
    public interface IStorageQuotaService
    {
        Task<(long usedBytes, long limitBytes)> GetStorageUsageAsync(int schoolId, CancellationToken cancellationToken = default);
        Task ValidateUploadQuotaAsync(int schoolId, List<long> fileSizes, CancellationToken cancellationToken = default);
        Task<long> GetRemainingStorageAsync(int schoolId, CancellationToken cancellationToken = default);
        Task<StorageQuotaDetailsResponse> GetStorageQuotaDetailsAsync(int schoolId, CancellationToken cancellationToken = default);
    }
}
