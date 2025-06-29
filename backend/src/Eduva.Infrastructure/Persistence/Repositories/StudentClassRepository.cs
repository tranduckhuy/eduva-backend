using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class StudentClassRepository : GenericRepository<StudentClass, int>, IStudentClassRepository
    {
        public StudentClassRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<Classroom>> GetClassesForStudentAsync(Guid studentId)
        {
            return await _context.StudentClasses
                .Where(sc => sc.StudentId == studentId)
                .Include(sc => sc.Class)
                    .ThenInclude(c => c.Teacher)
                .Include(sc => sc.Class)
                    .ThenInclude(c => c.School)
                .Select(sc => sc.Class)
                .ToListAsync();
        }

        public async Task<bool> IsStudentEnrolledInClassAsync(Guid studentId, Guid classId)
        {
            return await _context.StudentClasses
                .Include(sc => sc.Class)
                .AnyAsync(sc => sc.StudentId == studentId && sc.ClassId == classId);
        }

        public async Task<StudentClass?> GetStudentClassAsync(Guid studentId, Guid classId)
        {
            return await _context.StudentClasses
                .Include(sc => sc.Class)
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.ClassId == classId);
        }
        public async Task<StudentClass?> GetStudentClassByIdAsync(Guid studentClassId)
        {
            return await _context.StudentClasses
                .Include(sc => sc.Class)
                    .ThenInclude(c => c.Teacher)
                .Include(sc => sc.Class)
                    .ThenInclude(c => c.School)
                .FirstOrDefaultAsync(sc => sc.StudentId == studentClassId);
        }
    }
}