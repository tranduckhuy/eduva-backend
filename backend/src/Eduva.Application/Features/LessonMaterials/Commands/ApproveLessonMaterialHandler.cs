using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.LessonMaterials.Commands
{
    public class ApproveLessonMaterialHandler : IRequestHandler<ApproveLessonMaterialCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApproveLessonMaterialHandler(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Unit> Handle(ApproveLessonMaterialCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Domain.Entities.LessonMaterial, Guid>();
            var lesson = await repo.GetByIdAsync(request.Id)
                ?? throw new AppException(CustomCode.LessonMaterialNotFound);

            var userRepo = _unitOfWork.GetRepository<Domain.Entities.ApplicationUser, Guid>();
            var moderator = await userRepo.GetByIdAsync(request.ModeratorId)
                ?? throw new AppException(CustomCode.UserNotExists);

            var roles = await _userManager.GetRolesAsync(moderator);
            bool isModerator = roles.Contains(nameof(Role.ContentModerator));
            bool isSchoolAdmin = roles.Contains(nameof(Role.SchoolAdmin));

            if ((isModerator || isSchoolAdmin) && moderator.SchoolId == lesson.SchoolId)
            {
                if (request.Status == LessonMaterialStatus.Rejected && string.IsNullOrWhiteSpace(request.Feedback))
                    throw new AppException(CustomCode.ReasonIsRequiredWhenRejectingLessonMaterial);

                if (lesson.LessonStatus == request.Status)
                {
                    if (request.Status == LessonMaterialStatus.Approved)
                        throw new AppException(CustomCode.LessonMaterialAlreadyApproved);
                    if (request.Status == LessonMaterialStatus.Rejected)
                        throw new AppException(CustomCode.LessonMaterialAlreadyRejected);
                }

                lesson.LessonStatus = request.Status;
                repo.Update(lesson);

                var approvalRepo = _unitOfWork.GetRepository<LessonMaterialApproval, Guid>();
                var approval = new LessonMaterialApproval
                {
                    LessonMaterialId = lesson.Id,
                    ApproverId = moderator.Id,
                    StatusChangeTo = request.Status,
                    Feedback = request.Feedback?.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await approvalRepo.AddAsync(approval);

                await _unitOfWork.CommitAsync();
                return Unit.Value;
            }

            throw new AppException(CustomCode.MaterialNotAccessibleToStudent);
        }
    }
}
