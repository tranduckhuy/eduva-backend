using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class UserRepository : GenericRepository<ApplicationUser, Guid>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }
    }
}