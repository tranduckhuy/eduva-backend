using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class UserRepository : GenericRepository<ApplicationUser, Guid>, IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRepository(AppDbContext context, UserManager<ApplicationUser> userManager) : base(context)
        {
            _userManager = userManager;
        }

        public async Task<List<ApplicationUser>> GetUsersBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.SchoolId == schoolId)
                .ToListAsync(cancellationToken);
        }

        public async Task<ApplicationUser?> GetSchoolAdminBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default)
        {
            var users = await GetUsersBySchoolIdAsync(schoolId, cancellationToken);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(Role.SchoolAdmin.ToString()))
                    return user;
            }

            return null;
        }

        public async Task<ApplicationUser?> GetByIdWithSchoolAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.School)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }
    }
}