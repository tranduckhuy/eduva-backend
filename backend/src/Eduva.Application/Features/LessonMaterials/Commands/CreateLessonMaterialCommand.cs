using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.LessonMaterials.Commands
{
    public class CreateLessonMaterialCommand : IRequest<LessonMaterialResponse>
    {
        [JsonIgnore]
        public Guid CreatedBy { get; set; }

        public int? SchoolId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ContentType ContentType { get; set; }
        public string? Tag { get; set; }
        public int Duration { get; set; }
        public bool IsAIContent { get; set; }
        public string SourceUrl { get; set; } = string.Empty;
    }
}