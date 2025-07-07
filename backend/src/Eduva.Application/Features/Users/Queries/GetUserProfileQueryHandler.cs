using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Responses;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Users.Queries
{
    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISchoolSubscriptionService _schoolSubscriptionService;

        public GetUserProfileQueryHandler(IUserRepository userRepository, UserManager<ApplicationUser> userManager, ISchoolSubscriptionService schoolSubscriptionService)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _schoolSubscriptionService = schoolSubscriptionService;
        }

        public async Task<UserResponse> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdWithSchoolAsync(request.UserId, cancellationToken) ?? throw new UserNotExistsException();
            var roles = await _userManager.GetRolesAsync(user);
            var is2FAEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

            var (isActive, endDate) = await _schoolSubscriptionService.GetUserSubscriptionStatusAsync(user.Id);

            var userResponse = AppMapper<AppMappingProfile>.Mapper.Map<UserResponse>(user);
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
