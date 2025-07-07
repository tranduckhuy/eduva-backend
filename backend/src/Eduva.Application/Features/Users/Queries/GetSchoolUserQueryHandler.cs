using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Responses;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Users.Queries
{
    public class GetSchoolUserQueryHandler : IRequestHandler<GetSchoolUserQuery, UserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISchoolSubscriptionService _schoolSubscriptionService;

        public GetSchoolUserQueryHandler(IUserRepository userRepository, UserManager<ApplicationUser> userManager, ISchoolSubscriptionService schoolSubscriptionService)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _schoolSubscriptionService = schoolSubscriptionService;
        }

        public async Task<UserResponse> Handle(GetSchoolUserQuery request, CancellationToken cancellationToken)
        {
            var schoolAdmin = await _userRepository.GetByIdWithSchoolAsync(request.SchoolAdminId, cancellationToken);
            if (schoolAdmin?.SchoolId == null)
            {
                throw new AppException(CustomCode.UserNotPartOfSchool);
            }

            var targetUser = await _userRepository.GetByIdWithSchoolAsync(request.TargetUserId, cancellationToken) ?? throw new UserNotExistsException();

            if (targetUser.SchoolId != schoolAdmin.SchoolId)
            {
                throw new AppException(CustomCode.CannotViewUserFromDifferentSchool);
            }

            var targetUserRoles = await _userManager.GetRolesAsync(targetUser);
            var allowedRoles = new[] { nameof(Role.Student), nameof(Role.Teacher), nameof(Role.ContentModerator) };

            if (!targetUserRoles.Any(role => allowedRoles.Contains(role)))
            {
                throw new AppException(CustomCode.CannotViewRestrictedUserRoles);
            }

            var roles = await _userManager.GetRolesAsync(targetUser);
            var is2FAEnabled = await _userManager.GetTwoFactorEnabledAsync(targetUser);
            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(targetUser);

            var (isActive, endDate) = await _schoolSubscriptionService.GetUserSubscriptionStatusAsync(targetUser.Id);

            var userResponse = AppMapper<AppMappingProfile>.Mapper.Map<UserResponse>(targetUser);
            userResponse.Roles = roles.ToList();
            userResponse.Is2FAEnabled = is2FAEnabled;
            userResponse.IsEmailConfirmed = isEmailConfirmed;
            userResponse.UserSubscriptionResponse = new UserSubscriptionResponse
            {
                IsSubscriptionActive = isActive,
                SubscriptionEndDate = endDate
            };

            return userResponse;
        }
    }
}