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
            bool isSystemAdmin = userRoles.Contains(nameof(Role.SystemAdmin));
            bool isSchoolAdmin = userRoles.Contains(nameof(Role.SchoolAdmin));

            // Only allow admins to view classes
            if (!isSystemAdmin && !isSchoolAdmin)
            {
                throw new AppException(CustomCode.NotAdminForClassList);
            }

            if (isSchoolAdmin && !isSystemAdmin)
            {
                if (currentUser.SchoolId == null)
                {
                    throw new AppException(CustomCode.SchoolNotFound);
                }
                request.ClassSpecParam.SchoolId = currentUser.SchoolId.Value;
            }

            var spec = new ClassSpecification(request.ClassSpecParam);

            var result = await _unitOfWork.GetCustomRepository<IClassroomRepository>()
                .GetWithSpecAsync(spec);

            var classrooms = AppMapper.Mapper.Map<Pagination<ClassResponse>>(result);

            if (classrooms.Data.Count > 0)
            {
                var classIds = classrooms.Data.Select(c => c.Id).ToList();

                var folderRepository = _unitOfWork.GetRepository<Folder, Guid>();
                var allFolders = await folderRepository.GetAllAsync();
                var folders = allFolders.Where(f =>
                    f.OwnerType == OwnerType.Class &&
                    f.ClassId.HasValue &&
                    classIds.Contains(f.ClassId.Value)
                ).ToList();

                if (folders.Count > 0)
                {
                    var foldersByClass = folders.GroupBy(f => f.ClassId!.Value)
                        .ToDictionary(g => g.Key, g => g.Select(f => f.Id).ToList());

                    var allFolderIds = folders.Select(f => f.Id).ToList();

                    if (allFolderIds.Count > 0)
                    {
                        var lessonMaterialRepo = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();
                        var countsByFolder = await lessonMaterialRepo.GetApprovedMaterialCountsByFolderAsync(allFolderIds, cancellationToken);

                        foreach (var classResponse in classrooms.Data)
                        {
                            int totalCount = 0;

                            if (foldersByClass.TryGetValue(classResponse.Id, out var classFolderIds))
                            {
                                foreach (var folderId in classFolderIds)
                                {
                                    if (countsByFolder.TryGetValue(folderId, out var count))
                                    {
                                        totalCount += count;
                                    }
                                }
                            }

                            classResponse.CountLessonMaterial = totalCount;
                        }
                    }
                }
            }

            return classrooms;
        }
    }
}