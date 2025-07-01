using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface ILessonMaterialQuestionRepository : IGenericRepository<LessonMaterialQuestion, Guid>
    {
        Task<LessonMaterialQuestion?> GetQuestionWithFullDetailsAsync(Guid questionId);
        Task<bool> IsQuestionAccessibleToUserAsync(Guid questionId, Guid userId, string userRole);
    }
}