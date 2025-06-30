using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Users.Queries
{
    public class GetUserByIdForSchoolAdminQueryHandler : IRequestHandler<GetUserByIdForSchoolAdminQuery, UserResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISchoolSubscriptionService _schoolSubscriptionService;

        public GetUserByIdForSchoolAdminQueryHandler(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ISchoolSubscriptionService schoolSubscriptionService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _schoolSubscriptionService = schoolSubscriptionService;
        }

        public async Task<UserResponse> Handle(GetUserByIdForSchoolAdminQuery request, CancellationToken cancellationToken)
        {
            var userRepository = _unitOfWork.GetCustomRepository<IUserRepository>();

            var requester = await userRepository.GetByIdAsync(request.RequesterId) ?? throw new UserNotExistsException();

            var targetUser = await userRepository.GetByIdWithSchoolAsync(request.TargetUserId, cancellationToken) ?? throw new UserNotExistsException();

            var requesterRoles = await _userManager.GetRolesAsync(requester);
            if (!requesterRoles.Contains(nameof(Role.SchoolAdmin)))
            {
                throw new AppException(CustomCode.Forbidden);
            }

            if (requester.SchoolId == null)
            {
                throw new UserNotPartOfSchoolException();
            }

            if (targetUser.SchoolId != requester.SchoolId)
            {
                throw new AppException(CustomCode.Forbidden);
            }

            var targetUserRoles = await _userManager.GetRolesAsync(targetUser);
            var is2FAEnabled = await _userManager.GetTwoFactorEnabledAsync(targetUser);
            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(targetUser);

            var (isActive, endDate) = await _schoolSubscriptionService.GetUserSubscriptionStatusAsync(targetUser.Id);

            var response = AppMapper.Mapper.Map<UserResponse>(targetUser);
            response.Roles = targetUserRoles.ToList();
            response.Is2FAEnabled = is2FAEnabled;
            response.IsEmailConfirmed = isEmailConfirmed;
            response.UserSubscriptionResponse = new UserSubscriptionResponse
            {
                IsSubscriptionActive = isActive,
                SubscriptionEndDate = endDate
            };

            return response;
        }
    }
}