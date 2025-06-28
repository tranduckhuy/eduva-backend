using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Users.Commands
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteUserCommandHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            if (request.ExecutorId == request.UserId)
            {
                throw new AppException(CustomCode.CannotDeleteYourOwnAccount);
            }

            var user = await _userManager.FindByIdAsync(request.UserId.ToString()) ?? throw new AppException(CustomCode.UserNotFound);

            if (user.Status == EntityStatus.Deleted)
            {
                throw new AppException(CustomCode.UserAlreadyDeleted);
            }

            if (user.Status != EntityStatus.Inactive)
            {
                throw new AppException(CustomCode.UserMustBeLockedBeforeDelete);
            }

            user.Status = EntityStatus.Deleted;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new AppException(CustomCode.SystemError);
            }

            await _unitOfWork.CommitAsync();
            return Unit.Value;
        }
    }
}