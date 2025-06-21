using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Responses;
using Eduva.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Users.Queries
{
    public class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, UserResponse>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public GetUserProfileHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserResponse> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                throw new UserNotExistsException();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userResponse = AppMapper.Mapper.Map<UserResponse>(user);
            userResponse.Roles = roles.ToList();

            return userResponse;
        }
    }
}
