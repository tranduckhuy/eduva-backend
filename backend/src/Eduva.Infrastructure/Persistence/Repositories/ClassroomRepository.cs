using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class ClassroomRepository : GenericRepository<Classroom, Guid>, IClassroomRepository
    {
        public ClassroomRepository(AppDbContext context) : base(context)
        {
        }
    }
}
