using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Commands.DeleteLessonMaterial
{
    public class DeleteLessonMaterialCommand : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int? SchoolId { get; set; }
        public bool Permanent { get; set; } = false;
    }
}
