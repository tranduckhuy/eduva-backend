using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class LessonMaterialQuestionRepository : GenericRepository<LessonMaterialQuestion, Guid>, ILessonMaterialQuestionRepository
    {
        private readonly IStudentClassRepository _studentClassRepository;

        public LessonMaterialQuestionRepository(AppDbContext context, IStudentClassRepository studentClassRepository) : base(context)
        {
            _studentClassRepository = studentClassRepository;
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
                        return await _studentClassRepository.HasAccessToMaterialAsync(userId, lessonMaterialId);
                    case "Teacher":
                    case "ContentModerator":
                        return true;
                    default:
                        return false;
                }
            }

            // For viewing OTHER people's questions, apply original restrictions
            switch (userRole)
            {
                case "Student":
                    return await _studentClassRepository.HasAccessToMaterialAsync(userId, lessonMaterialId);

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

                    return await _studentClassRepository.TeacherHasAccessToMaterialAsync(userId, lessonMaterialId);

                default:
                    return false;
            }
        }
    }
}