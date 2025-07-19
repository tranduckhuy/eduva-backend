using Eduva.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.LessonMaterials.Commands
{
    public class UpdateLessonMaterialCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public Guid CreatorId { get; set; }

        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? Duration { get; set; }
        public LessonMaterialVisibility? Visibility { get; set; }
    }
}
