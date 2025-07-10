using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface IFolderRepository : IGenericRepository<Folder, Guid>
    {
        Task<int> GetMaxOrderAsync(Guid? userId, Guid? classId);

        Task<Folder?> GetFolderWithMaterialsAsync(Guid folderId);
        Task<IEnumerable<Folder>> ListAsync(Expression<Func<Folder, bool>> predicate, CancellationToken cancellationToken);
    }
}
