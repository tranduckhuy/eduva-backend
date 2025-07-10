using Eduva.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.LessonMaterials.Commands
{
    public class ApproveLessonMaterialCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public Guid Id { get; set; }
        [JsonIgnore]
        public Guid ModeratorId { get; set; }
        public LessonMaterialStatus Status { get; set; } = LessonMaterialStatus.Approved;
        public string Feedback { get; set; } = string.Empty;
    }
}