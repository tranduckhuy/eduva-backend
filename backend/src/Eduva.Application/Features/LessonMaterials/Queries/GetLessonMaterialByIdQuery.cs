using Eduva.Application.Features.LessonMaterials.Responses;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public class GetLessonMaterialByIdQuery : IRequest<LessonMaterialResponse>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int? SchoolId { get; set; }
    }
}
