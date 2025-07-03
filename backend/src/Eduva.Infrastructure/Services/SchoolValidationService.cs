using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Services
{
    public class SchoolValidationService : ISchoolValidationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SchoolValidationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task ValidateCanAddUsersAsync(int schoolId, int additionalUsers = 1, CancellationToken cancellationToken = default)
        {
            var schoolRepo = _unitOfWork.GetRepository<School, int>();
            var school = await schoolRepo.FirstOrDefaultAsync(
                s => s.Id == schoolId,
                include: q => q.Include(x => x.SchoolSubscriptions).ThenInclude(sub => sub.Plan),
                cancellationToken: cancellationToken
            ) ?? throw new SchoolNotFoundException();

            if (school.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.SchoolInactive);
            }

            var activeSubscription = school.SchoolSubscriptions.FirstOrDefault(s => s.SubscriptionStatus == SubscriptionStatus.Active);

            if (activeSubscription == null || activeSubscription.Plan == null)
            {
                throw new AppException(CustomCode.SubscriptionInvalid);
            }

            var maxUsers = activeSubscription.Plan.MaxUsers;

            if (maxUsers <= 0)
            {
                throw new AppException(CustomCode.SubscriptionInvalid);
            }

            var userRepo = _unitOfWork.GetCustomRepository<IUserRepository>();
            var currentUserCount = await userRepo.CountAsync(
                u => u.SchoolId == schoolId && u.Status != EntityStatus.Deleted,
                cancellationToken
            );

            if (currentUserCount + additionalUsers > maxUsers)
            {
                throw new AppException(CustomCode.ExceedUserLimit);
            }
        }
    }
}