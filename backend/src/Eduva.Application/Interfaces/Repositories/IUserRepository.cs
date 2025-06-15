using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<ApplicationUser, Guid>
    {
    }
}
