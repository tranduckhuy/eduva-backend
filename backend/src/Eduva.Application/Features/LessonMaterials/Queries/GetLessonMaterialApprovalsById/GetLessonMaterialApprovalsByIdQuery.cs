using Eduva.Application.Features.LessonMaterials.Responses;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovalsById
{
    public record GetLessonMaterialApprovalsByIdQuery(Guid LessonMaterialId, Guid UserId) : IRequest<List<LessonMaterialApprovalResponse>>;
}
