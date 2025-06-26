using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Users.Responses;
using Eduva.Application.Features.Users.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Users.Queries
{
    public class GetUsersBySpecQueryHandler : IRequestHandler<GetUsersBySpecQuery, Pagination<UserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetUsersBySpecQueryHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Pagination<UserResponse>> Handle(GetUsersBySpecQuery request, CancellationToken cancellationToken)
        {
            var spec = new UserSpecification(request.Param);
            var result = await _unitOfWork
                .GetRepository<ApplicationUser, Guid>()
                .GetWithSpecAsync(spec);

            var roleName = request.Param.Role?.ToString();

            var filteredUsers = new List<UserResponse>();

            foreach (var user in result.Data)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roleName == null || roles.Contains(roleName))
                {
                    var mapped = AppMapper.Mapper.Map<UserResponse>(user);
                    mapped.Roles = roles.ToList();
                    filteredUsers.Add(mapped);
                }
            }

            return new Pagination<UserResponse>
            {
                PageIndex = result.PageIndex,
                PageSize = result.PageSize,
                Count = filteredUsers.Count,
                Data = filteredUsers
            };
        }
    }
}