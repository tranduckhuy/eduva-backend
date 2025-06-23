using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface IFolderRepository : IGenericRepository<Folder, Guid>
    {
        Task<int> GetMaxOrderAsync(Guid? userId, Guid? classId);
    }
}
