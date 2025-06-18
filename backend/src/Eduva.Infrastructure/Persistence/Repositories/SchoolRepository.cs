using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class SchoolRepository : GenericRepository<School, int>, ISchoolRepository
    {
        public SchoolRepository(AppDbContext context) : base(context)
        {

        }
    }
}