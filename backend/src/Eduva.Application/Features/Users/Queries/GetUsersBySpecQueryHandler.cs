using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Schools.Responses;
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
            var roleName = request.Param.Role?.ToString();
            var filteredUsers = new List<UserResponse>();

            if (roleName != null)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

                var query = usersInRole.AsQueryable();

                if (request.Param.SchoolId.HasValue)
                {
                    query = query.Where(u => u.SchoolId == request.Param.SchoolId.Value);
                }

                if (request.Param.Status.HasValue)
                {
                    query = query.Where(u => u.Status == request.Param.Status.Value);
                }

                if (!string.IsNullOrWhiteSpace(request.Param.SearchTerm))
                {
                    var searchTerm = request.Param.SearchTerm.ToLower();
                    query = query.Where(u =>
                        (u.FullName ?? "").ToLower().Contains(searchTerm) ||
                        (u.Email ?? "").ToLower().Contains(searchTerm));
                }

                if (!string.IsNullOrWhiteSpace(request.Param.SortBy))
                {
                    bool isDesc = request.Param.SortDirection.ToLower() == "desc";
                    string sort = request.Param.SortBy.ToLower();

                    query = sort switch
                    {
                        "fullname" => isDesc
                            ? query.OrderByDescending(u => u.FullName)
                            : query.OrderBy(u => u.FullName),
                        "email" => isDesc
                            ? query.OrderByDescending(u => u.Email)
                            : query.OrderBy(u => u.Email),
                        "status" => isDesc
                            ? query.OrderByDescending(u => u.Status)
                            : query.OrderBy(u => u.Status),
                        "totalcredits" => isDesc
                            ? query.OrderByDescending(u => u.TotalCredits)
                            : query.OrderBy(u => u.TotalCredits),
                        "phonenumber" => isDesc
                            ? query.OrderByDescending(u => u.PhoneNumber)
                            : query.OrderBy(u => u.PhoneNumber),
                        "accessfailedcount" => isDesc
                            ? query.OrderByDescending(u => u.AccessFailedCount)
                            : query.OrderBy(u => u.AccessFailedCount),
                        "createdat" => isDesc
                            ? query.OrderByDescending(u => u.CreatedAt)
                            : query.OrderBy(u => u.CreatedAt),
                        "lastmodifiedat" => isDesc
                            ? query.OrderByDescending(u => u.LastModifiedAt)
                            : query.OrderBy(u => u.LastModifiedAt),
                        "lastloginin" => isDesc
                            ? query.OrderByDescending(u => u.LastLoginAt)
                            : query.OrderBy(u => u.LastLoginAt),
                        _ => isDesc
                            ? query.OrderByDescending(u => u.FullName)
                            : query.OrderBy(u => u.FullName)
                    };
                }

                var totalCount = query.Count();
                var pagedUsers = query
                    .Skip((request.Param.PageIndex - 1) * request.Param.PageSize)
                    .Take(request.Param.PageSize)
                    .ToList();

                var schoolIds = pagedUsers.Where(u => u.SchoolId.HasValue)
                    .Select(u => u.SchoolId!.Value)
                    .Distinct()
                    .ToList();

                var schools = new Dictionary<int, School>();
                foreach (var schoolId in schoolIds)
                {
                    var school = await _unitOfWork.GetRepository<School, int>().GetByIdAsync(schoolId);
                    if (school != null)
                        schools[schoolId] = school;
                }

                foreach (var user in pagedUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var mapped = AppMapper<AppMappingProfile>.Mapper.Map<UserResponse>(user);
                    mapped.Roles = roles.ToList();

                    if (user.SchoolId.HasValue && schools.TryGetValue(user.SchoolId.Value, out var schoolEntity))
                    {
                        mapped.School = AppMapper<AppMappingProfile>.Mapper.Map<SchoolResponse>(schoolEntity);
                    }

                    filteredUsers.Add(mapped);
                }

                return new Pagination<UserResponse>
                {
                    PageIndex = request.Param.PageIndex,
                    PageSize = request.Param.PageSize,
                    Count = totalCount,
                    Data = filteredUsers
                };
            }
            else
            {
                var spec = new UserSpecification(request.Param);
                var result = await _unitOfWork
                    .GetRepository<ApplicationUser, Guid>()
                    .GetWithSpecAsync(spec);

                foreach (var user in result.Data)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var mapped = AppMapper<AppMappingProfile>.Mapper.Map<UserResponse>(user);
                    mapped.Roles = roles.ToList();
                    filteredUsers.Add(mapped);
                }

                return new Pagination<UserResponse>
                {
                    PageIndex = result.PageIndex,
                    PageSize = result.PageSize,
                    Count = result.Count,
                    Data = filteredUsers
                };
            }
        }
    }
}