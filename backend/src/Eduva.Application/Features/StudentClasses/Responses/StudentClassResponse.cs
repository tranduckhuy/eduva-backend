using Eduva.Domain.Enums;

namespace Eduva.Application.Features.StudentClasses.Responses
{
    public class StudentClassResponse
    {
        public int Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string ClassCode { get; set; } = string.Empty;
        public DateTimeOffset EnrolledAt { get; set; }
        public EntityStatus ClassStatus { get; set; }
    }
}
