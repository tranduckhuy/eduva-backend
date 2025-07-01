using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface IStudentClassRepository : IGenericRepository<StudentClass, Guid>
    {
        Task<List<Classroom>> GetClassesForStudentAsync(Guid studentId);

        Task<bool> IsStudentEnrolledInClassAsync(Guid studentId, Guid classId);

        Task<StudentClass?> GetStudentClassAsync(Guid studentId, Guid classId);
        Task<StudentClass?> GetStudentClassByIdAsync(Guid studentClassId);

        Task<bool> HasAccessToMaterialAsync(Guid userId, Guid lessonMaterialId);
        Task<bool> IsEnrolledInAnyClassAsync(Guid userId);
        Task<bool> HasValidClassInSchoolAsync(Guid userId, int schoolId);
        Task<bool> TeacherHasActiveClassAsync(Guid teacherId);
        Task<bool> TeacherHasValidClassInSchoolAsync(Guid teacherId, int schoolId);
        Task<bool> TeacherHasAccessToMaterialAsync(Guid teacherId, Guid lessonMaterialId);
    }
}
