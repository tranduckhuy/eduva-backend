using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.LessonMaterials.Commands
{
    public class CreateLessonMaterialCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public Guid CreatedBy { get; set; }

        [JsonIgnore]
        public int? SchoolId { get; set; }

        public Guid FolderId { get; set; }

        public List<string> BlobNames { get; set; } = [];
        public List<LessonMaterialRequest> LessonMaterials { get; set; } = [];
    }
}