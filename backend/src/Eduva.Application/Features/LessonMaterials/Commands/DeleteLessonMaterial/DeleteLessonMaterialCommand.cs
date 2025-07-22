using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.LessonMaterials.Commands.DeleteLessonMaterial
{
    public class DeleteLessonMaterialCommand : IRequest<Unit>
    {
        public List<Guid> Ids { get; set; } = [];

        [JsonIgnore]
        public Guid UserId { get; set; }

        [JsonIgnore]
        public int? SchoolId { get; set; }

        public bool Permanent { get; set; } = false;
    }
}
