using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<ApplicationUser, Guid>
    {
        Task<List<ApplicationUser>> GetUsersBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default);
        Task<ApplicationUser?> GetSchoolAdminBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default);
        Task<ApplicationUser?> GetByIdWithSchoolAsync(Guid userId, CancellationToken cancellationToken = default);
        Task UpdateCreditBalanceAsync(Guid userId, int amount, CancellationToken cancellationToken = default);
    }
}
