using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Users.Commands
{
    public class UnlockAccountCommandHandler : IRequestHandler<UnlockAccountCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public UnlockAccountCommandHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Unit> Handle(UnlockAccountCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == request.ExecutorId)
            {
                throw new AppException(CustomCode.CannotUnlockSelf);
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

            var isLocked = targetUser.LockoutEnd.HasValue && targetUser.LockoutEnd > DateTimeOffset.UtcNow;
            if (!isLocked)
            {
                throw new AppException(CustomCode.UserNotLocked);
            }

            targetUser.LockoutEnd = null;
            targetUser.LockoutEnabled = false;
            targetUser.Status = EntityStatus.Active;

            var result = await _userManager.UpdateAsync(targetUser);
            if (!result.Succeeded)
            {
                throw new AppException(CustomCode.SystemError);
            }

            await _unitOfWork.CommitAsync();
            return Unit.Value;
        }
    }
}