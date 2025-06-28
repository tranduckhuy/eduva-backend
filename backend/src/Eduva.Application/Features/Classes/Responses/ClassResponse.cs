using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Classes.Responses
{
    public class ClassResponse
    {
        public Guid Id { get; set; }
        public int SchoolId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ClassCode { get; set; }
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string? TeacherAvatarUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastModifiedAt { get; set; }
        public EntityStatus Status { get; set; }
    }
}
