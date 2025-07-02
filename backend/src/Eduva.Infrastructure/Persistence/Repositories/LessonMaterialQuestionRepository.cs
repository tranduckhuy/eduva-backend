using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class LessonMaterialQuestionRepository : GenericRepository<LessonMaterialQuestion, Guid>, ILessonMaterialQuestionRepository
    {
        public LessonMaterialQuestionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<LessonMaterialQuestion?> GetQuestionWithFullDetailsAsync(Guid questionId)
        {
            return await _context.LessonMaterialQuestions
                .Include(q => q.CreatedByUser)
                .Include(q => q.LessonMaterial)
                    .ThenInclude(lm => lm.School)
                .Include(q => q.Comments.OrderBy(c => c.CreatedAt))
                    .ThenInclude(c => c.CreatedByUser)
                .Include(q => q.Comments)
                    .ThenInclude(c => c.Replies.OrderBy(r => r.CreatedAt))
                        .ThenInclude(r => r.CreatedByUser)
                .FirstOrDefaultAsync(q => q.Id == questionId);
        }

        public async Task<bool> IsQuestionAccessibleToUserAsync(Guid questionId, Guid userId, string userRole)
        {
            var question = await _context.LessonMaterialQuestions
                .Include(q => q.LessonMaterial)
                .Include(q => q.CreatedByUser)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return false;
            }

            if (userRole == "SchoolAdmin" || userRole == "SystemAdmin")
            {
                return true;
            }

            var lessonMaterialId = question.LessonMaterialId;

            if (question.CreatedByUserId == userId)
            {
                // Users can view their own questions if they have access to the material
                switch (userRole)
                {
                    case "Student":
                        return await StudentHasAccessToMaterial(userId, lessonMaterialId);
                    case "Teacher":
                    case "ContentModerator":
                        return await TeacherHasAccessToMaterial(userId, lessonMaterialId);
                    default:
                        return false;
                }
            }

            // For viewing OTHER people's questions, apply original restrictions
            switch (userRole)
            {
                case "Student":
                    return await StudentHasAccessToMaterial(userId, lessonMaterialId);

                case "Teacher":
                case "ContentModerator":
                    var questionOwnerRoles = await _context.UserRoles
                        .Where(ur => ur.UserId == question.CreatedByUserId)
                        .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                        .ToListAsync();

                    if (!questionOwnerRoles.Contains("Student"))
                    {
                        return false;
                    }

                    return await TeacherHasAccessToMaterial(userId, lessonMaterialId);

                default:
                    return false;
            }
        }

        private async Task<bool> StudentHasAccessToMaterial(Guid userId, Guid lessonMaterialId)
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

        private async Task<bool> TeacherHasAccessToMaterial(Guid teacherId, Guid lessonMaterialId)
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