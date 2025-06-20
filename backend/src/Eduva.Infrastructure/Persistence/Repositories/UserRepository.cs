using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class UserRepository : GenericRepository<ApplicationUser, Guid>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<ApplicationUser>> GetUsersBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.SchoolId == schoolId)
                .ToListAsync(cancellationToken);
        }
    }
}