using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Users.Commands
{
    public class LockAccountCommandHandler : IRequestHandler<LockAccountCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthService _authService;

        public LockAccountCommandHandler(
               IUnitOfWork unitOfWork,
               UserManager<ApplicationUser> userManager,
               IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _authService = authService;
        }

        public async Task<Unit> Handle(LockAccountCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == request.ExecutorId)
            {
                throw new AppException(CustomCode.CannotLockSelf);
            }

            var targetUser = await _userManager.FindByIdAsync(request.UserId.ToString()) ?? throw new UserNotExistsException();

            var executorUser = await _userManager.FindByIdAsync(request.ExecutorId.ToString()) ?? throw new UserNotExistsException();

            var targetRoles = await _userManager.GetRolesAsync(targetUser);
            var executorRoles = await _userManager.GetRolesAsync(executorUser);

            var isTargetSystemAdmin = targetRoles.Contains(Role.SystemAdmin.ToString());
            var isExecutorSchoolAdmin = executorRoles.Contains(Role.SchoolAdmin.ToString());

            if (isExecutorSchoolAdmin && isTargetSystemAdmin)
            {
                throw new AppException(CustomCode.Forbidden);
            }

            if (isExecutorSchoolAdmin)
            {
                if (executorUser.SchoolId != targetUser.SchoolId)
                {
                    throw new AppException(CustomCode.Forbidden);
                }
            }

            if (targetUser.LockoutEnd.HasValue && targetUser.LockoutEnd > DateTimeOffset.UtcNow)
            {
                throw new AppException(CustomCode.UserAlreadyLocked);
            }

            targetUser.LockoutEnabled = true;
            targetUser.LockoutEnd = DateTimeOffset.MaxValue;
            targetUser.Status = EntityStatus.Inactive;

            var result = await _userManager.UpdateAsync(targetUser);
            if (!result.Succeeded)
            {
                throw new AppException(CustomCode.SystemError);
            }

            await _authService.InvalidateAllUserTokensAsync(targetUser.Id.ToString());

            await _unitOfWork.CommitAsync();
            return Unit.Value;
        }
    }

}
