using AutoMapper;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovalsById
{
    public class GetLessonMaterialApprovalsByIdHandler : IRequestHandler<GetLessonMaterialApprovalsByIdQuery, List<LessonMaterialApprovalResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetLessonMaterialApprovalsByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
        }

        public async Task<List<LessonMaterialApprovalResponse>> Handle(GetLessonMaterialApprovalsByIdQuery request, CancellationToken cancellationToken)
        {
            // Get repositories
            var materialRepo = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();
            var userRepository = _unitOfWork.GetCustomRepository<IUserRepository>();

            // Get lesson material
            var lessonMaterial = await materialRepo.GetByIdAsync(request.LessonMaterialId);
            if (lessonMaterial == null)
                throw new AppException(CustomCode.LessonMaterialNotFound);

            // Get user and roles
            var user = await userRepository.GetByIdAsync(request.UserId);
            if (user == null)
                throw new AppException(CustomCode.UserNotExists);

            var roles = await _userManager.GetRolesAsync(user);

            // Phân quyền
            if (roles.Contains(nameof(Role.SystemAdmin)))
            {
                // System admin xem tất cả
            }
            else if (roles.Contains(nameof(Role.SchoolAdmin)) || roles.Contains(nameof(Role.ContentModerator)))
            {
                if (lessonMaterial.SchoolId != user.SchoolId)
                    throw new AppException(CustomCode.Forbidden);
            }
            else if (roles.Contains(nameof(Role.Teacher)))
            {
                if (lessonMaterial.CreatedByUserId != user.Id)
                    throw new AppException(CustomCode.Forbidden);
            }
            else
            {
                throw new AppException(CustomCode.Forbidden);
            }

            // After authorization, proceed with fetching approvals
            var repo = _unitOfWork.GetRepository<LessonMaterialApproval, Guid>();
            var approvals = new List<LessonMaterialApproval>();

            var approval = await repo.FirstOrDefaultAsync(
                x => x.LessonMaterialId == request.LessonMaterialId,
                query => query
                    .Include(x => x.LessonMaterial)
                    .Include(x => x.Approver)
                    .OrderByDescending(x => x.CreatedAt),
                cancellationToken);

            if (approval != null)
            {
                approvals.Add(approval);

                while (true)
                {
                    approval = await repo.FirstOrDefaultAsync(
                        x => x.LessonMaterialId == request.LessonMaterialId && x.CreatedAt < approval.CreatedAt,
                        query => query
                            .Include(x => x.LessonMaterial)
                            .Include(x => x.Approver)
                            .OrderByDescending(x => x.CreatedAt),
                        cancellationToken);

                    if (approval != null)
                        approvals.Add(approval);
                    else
                        break;
                }
            }

            return _mapper.Map<List<LessonMaterialApprovalResponse>>(approvals);
        }
    }
}
