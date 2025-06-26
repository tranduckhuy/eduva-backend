using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Classes.Queries.GetClasses
{
    public class GetClassesHandler : IRequestHandler<GetClassesQuery, Pagination<ClassResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetClassesHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Pagination<ClassResponse>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
        {
            // Get the current user
            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var currentUser = await userRepository.GetByIdAsync(request.UserId)
                ?? throw new AppException(CustomCode.UserNotExists);

            // Check user roles
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            bool isAdmin = userRoles.Contains(nameof(Role.SystemAdmin)) || userRoles.Contains(nameof(Role.SchoolAdmin));

            // Only allow admins to view all classes
            if (!isAdmin)
            {
                throw new AppException(CustomCode.NotAdminForClassList);
            }

            var spec = new ClassSpecification(request.ClassSpecParam);

            var result = await _unitOfWork.GetCustomRepository<IClassroomRepository>()
                .GetWithSpecAsync(spec);

            var classrooms = AppMapper.Mapper.Map<Pagination<ClassResponse>>(result);

            return classrooms;
        }
    }
}
