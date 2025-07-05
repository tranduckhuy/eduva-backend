using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Storage;
using Eduva.Application.Features.StorageQuota;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Services
{
    public class StorageQuotaService : IStorageQuotaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StorageQuotaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(long usedBytes, long limitBytes)> GetStorageUsageAsync(int schoolId, CancellationToken cancellationToken = default)
        {
            var schoolSubscriptionRepo = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();

            // Get the active subscription for the school
            var activeSubscription = await schoolSubscriptionRepo.GetLatestSubscriptionBySchoolIdAsync(schoolId, cancellationToken)
                ?? throw new AppException(CustomCode.SubscriptionInvalid);

            var limitBytes = (long)(activeSubscription.Plan.StorageLimitGB * 1024 * 1024 * 1024); // Convert GB to bytes

            // Calculate used storage from lesson materials
            var lessonMaterialRepo = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();
            var usedBytes = await lessonMaterialRepo.GetTotalFileSizeBySchoolAsync(schoolId, cancellationToken);

            return (usedBytes, limitBytes);
        }

        public async Task ValidateUploadQuotaAsync(int schoolId, List<long> fileSizes, CancellationToken cancellationToken = default)
        {
            var (usedBytes, limitBytes) = await GetStorageUsageAsync(schoolId, cancellationToken);

            var totalNewBytes = fileSizes.Sum();
            var projectedUsage = usedBytes + totalNewBytes;

            if (projectedUsage > limitBytes)
            {
                var remainingBytes = limitBytes - usedBytes;
                throw new StorageQuotaExceededException(
                    usedBytes,
                    limitBytes,
                    totalNewBytes,
                    remainingBytes
                );
            }
        }

        public async Task<long> GetRemainingStorageAsync(int schoolId, CancellationToken cancellationToken = default)
        {
            var (usedBytes, limitBytes) = await GetStorageUsageAsync(schoolId, cancellationToken);
            return Math.Max(0, limitBytes - usedBytes);
        }

        public async Task<StorageQuotaDetailsResponse> GetStorageQuotaDetailsAsync(int schoolId, CancellationToken cancellationToken = default)
        {
            var (usedBytes, limitBytes) = await GetStorageUsageAsync(schoolId, cancellationToken);
            var remainingBytes = Math.Max(0, limitBytes - usedBytes);

            const double bytesToGB = 1024.0 * 1024.0 * 1024.0;

            return new StorageQuotaDetailsResponse
            {
                UsedBytes = usedBytes,
                LimitBytes = limitBytes,
                RemainingBytes = remainingBytes,
                UsedGB = Math.Round(usedBytes / bytesToGB, 2),
                LimitGB = Math.Round(limitBytes / bytesToGB, 2),
                RemainingGB = Math.Round(remainingBytes / bytesToGB, 2),
                UsagePercentage = limitBytes > 0 ? Math.Round((double)usedBytes / limitBytes * 100, 2) : 0
            };
        }
    }
}
