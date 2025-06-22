using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
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
    }
}