using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Commands.RestoreLessonMaterial
{
    public class RestoreLessonMaterialCommand : IRequest<bool>
    {
        public Guid PersonalFolderId { get; set; }
        public List<Guid> LessonMaterialIds { get; set; } = new();
        public Guid CurrentUserId { get; set; }
    }
}