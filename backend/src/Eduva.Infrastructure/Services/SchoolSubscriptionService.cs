using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;

namespace Eduva.Infrastructure.Services
{
    public class SchoolSubscriptionService : ISchoolSubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork; public SchoolSubscriptionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // Get the current and latest subscription for a school even if it has expired
        public async Task<SchoolSubscription?> GetCurrentSubscriptionAsync(int schoolId)
        {
            var schoolSubscriptionRepository = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();

            return await schoolSubscriptionRepository.GetLatestSubscriptionBySchoolIdAsync(schoolId);
        }
    }
}
