using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Users.Commands
{
    public class AssignUserRolesCommandHandler : IRequestHandler<AssignUserRolesCommand, Unit>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignUserRolesCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Unit> Handle(AssignUserRolesCommand request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var schoolAdmin = await _userManager.FindByIdAsync(request.SchoolAdminId.ToString())
                ?? throw new UserNotExistsException();

            var schoolAdminRoles = await _userManager.GetRolesAsync(schoolAdmin);
            if (!schoolAdminRoles.Contains(nameof(Role.SchoolAdmin)))
            {
                throw new AppException(CustomCode.InsufficientPermissionToManageRoles);
            }

            if (schoolAdmin.SchoolId == null)
            {
                throw new UserNotPartOfSchoolException();
            }

            var targetUser = await _userManager.FindByIdAsync(request.TargetUserId.ToString())
                ?? throw new UserNotExistsException();

            if (targetUser.SchoolId != schoolAdmin.SchoolId)
            {
                throw new AppException(CustomCode.CannotManageUserFromDifferentSchool);
            }

            var currentTargetRoles = await _userManager.GetRolesAsync(targetUser);
            if (currentTargetRoles.Contains(nameof(Role.SystemAdmin)) ||
                currentTargetRoles.Contains(nameof(Role.SchoolAdmin)))
            {
                throw new AppException(CustomCode.CannotModifyRestrictedUserRoles);
            }

            if (request.TargetUserId == request.SchoolAdminId)
            {
                throw new AppException(CustomCode.CannotModifyOwnRoles);
            }

            var rolesToRemove = currentTargetRoles.Where(r =>
                r != nameof(Role.SystemAdmin) &&
                r != nameof(Role.SchoolAdmin)).ToList();

            if (rolesToRemove.Count != 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(targetUser, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    var errors = removeResult.Errors.Select(e => e.Description).ToList();
                    throw new AppException(CustomCode.RoleRemovalFailed, errors);
                }
            }

            var newRoleNames = request.Roles.Select(r => r.ToString()).ToList();
            var addResult = await _userManager.AddToRolesAsync(targetUser, newRoleNames);
            if (!addResult.Succeeded)
            {
                var errors = addResult.Errors.Select(e => e.Description).ToList();
                throw new AppException(CustomCode.RoleAssignmentFailed, errors);
            }

            return Unit.Value;
        }

        private static void ValidateRequest(AssignUserRolesCommand request)
        {
            if (request.TargetUserId == Guid.Empty)
            {
                throw new AppException(CustomCode.ProvidedInformationIsInValid);
            }

            if (request.SchoolAdminId == Guid.Empty)
            {
                throw new AppException(CustomCode.ProvidedInformationIsInValid);
            }

            if (request.Roles == null || request.Roles.Count == 0)
            {
                throw new AppException(CustomCode.RoleListEmpty);
            }

            if (request.Roles.Contains(Role.SystemAdmin) || request.Roles.Contains(Role.SchoolAdmin))
            {
                throw new AppException(CustomCode.RestrictedRoleNotAllowed);
            }

            if (request.Roles.Contains(Role.Student))
            {
                throw new AppException(CustomCode.StudentRoleNotAssignable);
            }

            var uniqueRoles = request.Roles.Distinct().ToList();

            if (uniqueRoles.Count == 1)
            {
                var role = uniqueRoles.First();
                if (role is not (Role.Teacher or Role.ContentModerator))
                {
                    throw new AppException(CustomCode.InvalidSingleRole);
                }
            }
            else if (uniqueRoles.Count == 2)
            {
                if (!(uniqueRoles.Contains(Role.ContentModerator) && uniqueRoles.Contains(Role.Teacher)))
                {
                    throw new AppException(CustomCode.InvalidMultipleRoleCombination);
                }
            }
            else
            {
                throw new AppException(CustomCode.TooManyRolesAssigned);
            }
        }
    }
}