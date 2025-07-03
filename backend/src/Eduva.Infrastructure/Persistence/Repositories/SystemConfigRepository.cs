using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly AppDbContext _context;

        public SystemConfigRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SystemConfig?> GetByKeyAsync(string key)
        {
            return await _context.SystemConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == key);
        }

        public async Task<IEnumerable<SystemConfig>> GetAllAsync()
        {
            return await _context.SystemConfigs
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task UpdateAsync(SystemConfig config)
        {
            var existing = await _context.SystemConfigs
                .FirstOrDefaultAsync(x => x.Key == config.Key);

            if (existing != null)
            {
                existing.Value = config.Value;
                existing.Description = config.Description;
                existing.LastModifiedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
