using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class StudentClassRepository : GenericRepository<StudentClass, Guid>, IStudentClassRepository
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

        public async Task<bool> HasAccessToMaterialAsync(Guid userId, Guid lessonMaterialId)
        {
            return await _context.StudentClasses
                .Where(sc => sc.StudentId == userId)
                .Join(_context.Folders.Where(f => f.Status == EntityStatus.Active),
                    sc => sc.ClassId,
                    f => f.ClassId,
                    (sc, f) => f)
                .Join(_context.FolderLessonMaterials,
                    f => f.Id,
                    flm => flm.FolderId,
                    (f, flm) => flm)
                .AnyAsync(flm => flm.LessonMaterialId == lessonMaterialId);
        }

        public async Task<bool> IsEnrolledInAnyClassAsync(Guid userId)
        {
            return await _context.StudentClasses
                .AnyAsync(sc => sc.StudentId == userId);
        }

        public async Task<bool> HasValidClassInSchoolAsync(Guid userId, int schoolId)
        {
            return await _context.StudentClasses
                .Where(sc => sc.StudentId == userId)
                .Join(_context.Classes.Where(c => c.Status == EntityStatus.Active),
                    sc => sc.ClassId,
                    c => c.Id,
                    (sc, c) => c)
                .AnyAsync(c => c.SchoolId == schoolId);
        }

        public async Task<bool> TeacherHasActiveClassAsync(Guid teacherId)
        {
            return await _context.Classes
                .Where(c => c.TeacherId == teacherId && c.Status == EntityStatus.Active)
                .AnyAsync();
        }

        public async Task<bool> TeacherHasValidClassInSchoolAsync(Guid teacherId, int schoolId)
        {
            return await _context.Classes
                .Where(c => c.TeacherId == teacherId &&
                           c.SchoolId == schoolId &&
                           c.Status == EntityStatus.Active)
                .AnyAsync();
        }

        public async Task<bool> TeacherHasAccessToMaterialAsync(Guid teacherId, Guid lessonMaterialId)
        {
            return await _context.Classes
                .Where(c => c.TeacherId == teacherId && c.Status == EntityStatus.Active)
                .Join(_context.Folders.Where(f => f.Status == EntityStatus.Active),
                    c => c.Id,
                    f => f.ClassId,
                    (c, f) => f)
                .Join(_context.FolderLessonMaterials,
                    f => f.Id,
                    flm => flm.FolderId,
                    (f, flm) => flm)
                .AnyAsync(flm => flm.LessonMaterialId == lessonMaterialId);
        }
    }
}