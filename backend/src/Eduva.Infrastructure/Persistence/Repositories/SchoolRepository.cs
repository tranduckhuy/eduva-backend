using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Eduva.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class SchoolRepository : GenericRepository<School, int>, ISchoolRepository
    {
        public SchoolRepository(AppDbContext context) : base(context)
        {

        }
        public async Task<School?> GetByUserIdAsync(Guid userId)
        {
            return await _context.Schools
                .Include(s => s.Users)
                .FirstOrDefaultAsync(s => s.Users.Any(u => u.Id == userId));
        }

        public async Task<(int currentUserCount, int maxUserLimit)> GetUserLimitInfoByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            var school = await _context.Schools
                .Include(s => s.Users)
                .Include(s => s.SchoolSubscriptions)
                    .ThenInclude(sub => sub.Plan)
                .FirstOrDefaultAsync(s => s.Users.Any(u => u.Id == userId), cancellationToken) ?? throw new AppException(CustomCode.SchoolNotFound);

            var activeSub = school.SchoolSubscriptions.FirstOrDefault(s => s.SubscriptionStatus == SubscriptionStatus.Active);

            if (activeSub?.Plan == null || activeSub.Plan.MaxUsers <= 0)
            {
                throw new AppException(CustomCode.SubscriptionInvalid);
            }

            return (school.Users.Count(u => u.Status != EntityStatus.Deleted), activeSub.Plan.MaxUsers);
        }
    }
}