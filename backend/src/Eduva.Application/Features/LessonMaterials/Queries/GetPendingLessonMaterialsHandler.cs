using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials.Queries.Extensions;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public class GetPendingLessonMaterialsHandler : IRequestHandler<GetPendingLessonMaterialsQuery, Pagination<LessonMaterialResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetPendingLessonMaterialsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<LessonMaterialResponse>> Handle(GetPendingLessonMaterialsQuery request, CancellationToken cancellationToken)
        {
            // Modify the spec param to filter only pending materials
            var pendingSpecParam = request.LessonMaterialSpecParam;
            pendingSpecParam.LessonStatus = LessonMaterialStatus.Pending;

            // For Teachers, only show their own pending materials
            if (request.UserRoles.HasTeacherRole() &&
                !request.UserRoles.HasSchoolAdminRole() &&
                !request.UserRoles.HasContentModeratorRole())
            {
                pendingSpecParam.CreatedByUserId = request.UserId;
            }

            var spec = new PendingLessonMaterialSpecification(pendingSpecParam);

            var result = await _unitOfWork.GetCustomRepository<ILessonMaterialRepository>()
                .GetWithSpecAsync(spec);

            var lessonMaterials = AppMapper<AppMappingProfile>.Mapper.Map<Pagination<LessonMaterialResponse>>(result);

            return lessonMaterials;
        }
    }
}
