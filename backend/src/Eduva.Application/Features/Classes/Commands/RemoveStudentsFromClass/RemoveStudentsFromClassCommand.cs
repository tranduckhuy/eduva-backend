using MediatR;

namespace Eduva.Application.Features.Classes.Commands.RemoveStudentsFromClass
{
    public class RemoveStudentsFromClassCommand : IRequest<Unit>
    {
        public Guid ClassId { get; set; }
        public List<Guid> StudentIds { get; set; } = new List<Guid>();
        public Guid RequestUserId { get; set; }
        public bool IsTeacher { get; set; }
        public bool IsSchoolAdmin { get; set; }
        public bool IsSystemAdmin { get; set; }
        public bool IsContentModerator { get; set; }

    }
}