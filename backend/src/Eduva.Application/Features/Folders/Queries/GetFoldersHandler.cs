using AutoMapper;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Folders.Queries
{
    public class GetFoldersHandler : IRequestHandler<GetFoldersQuery, Pagination<FolderResponse>>
    {
        private readonly IFolderRepository _folderRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetFoldersHandler(
            IFolderRepository folderRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _folderRepository = folderRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }
        public async Task<Pagination<FolderResponse>> Handle(GetFoldersQuery request, CancellationToken cancellationToken)
        {
            // If viewing class folders, check authorization
            if (request.FolderSpecParam.OwnerType == OwnerType.Class &&
                request.FolderSpecParam.ClassId.HasValue &&
                request.UserId.HasValue)
            {
                await CheckClassFolderAccessAsync(request.UserId.Value, request.FolderSpecParam.ClassId.Value);
            }

            var spec = new FolderSpecification(request.FolderSpecParam);

            var folderPagination = await _folderRepository.GetWithSpecAsync(spec);

            var data = _mapper.Map<IReadOnlyCollection<FolderResponse>>(folderPagination.Data);

            return new Pagination<FolderResponse>
            {
                PageIndex = request.FolderSpecParam.PageIndex,
                PageSize = request.FolderSpecParam.PageSize,
                Count = folderPagination.Count,
                Data = data
            };
        }
        private async Task CheckClassFolderAccessAsync(Guid userId, Guid classId)
        {
            // Get the user to check role
            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                throw new AppException(CustomCode.UserNotExists);
            }

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            // System admins can access any folder
            if (roles.Contains(nameof(Role.SystemAdmin)))
            {
                return;
            }

            // Get the class
            var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
            var classroom = await classRepository.GetByIdAsync(classId);

            if (classroom == null)
            {
                throw new AppException(CustomCode.ClassNotFound);
            }

            // School admins can access any folder in their school
            if (roles.Contains(nameof(Role.SchoolAdmin)) && classroom.SchoolId == user.SchoolId)
            {
                return;
            }

            // Teachers can access folders in their own classes
            if (roles.Contains(nameof(Role.Teacher)) && classroom.TeacherId == userId)
            {
                return;
            }

            // Students can access folders in classes they're enrolled in
            if (roles.Contains(nameof(Role.Student)))
            {
                var studentClassRepository = _unitOfWork.GetCustomRepository<IStudentClassRepository>();
                bool isEnrolled = await studentClassRepository.IsStudentEnrolledInClassAsync(userId, classId);

                if (isEnrolled)
                {
                    return;
                }
            }

            // If we get here, access is denied
            throw new AppException(CustomCode.Forbidden);
        }
    }
}
