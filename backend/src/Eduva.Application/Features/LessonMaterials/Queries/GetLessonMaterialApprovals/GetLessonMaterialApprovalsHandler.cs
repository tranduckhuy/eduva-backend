using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials.Queries.Extensions;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovals
{
    public class GetLessonMaterialApprovalsHandler : IRequestHandler<GetLessonMaterialApprovalsQuery, Pagination<LessonMaterialApprovalResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetLessonMaterialApprovalsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<LessonMaterialApprovalResponse>> Handle(GetLessonMaterialApprovalsQuery request, CancellationToken cancellationToken)
        {
            var specParam = request.SpecParam;

            // For regular teachers, only show approvals for their materials
            if (request.UserRoles.HasTeacherRole() &&
                !request.UserRoles.HasSchoolAdminRole() &&
                !request.UserRoles.HasContentModeratorRole() &&
                !request.UserRoles.HasSystemAdminRole())
            {
                specParam.CreatedByUserId = request.UserId;
            }

            var spec = new LessonMaterialApprovalsSpecification(specParam);

            var repository = _unitOfWork.GetRepository<LessonMaterialApproval, Guid>();
            var result = await repository.GetWithSpecAsync(spec);

            var approvalHistory = AppMapper<AppMappingProfile>.Mapper.Map<Pagination<LessonMaterialApprovalResponse>>(result);

            return approvalHistory;
        }
    }
}