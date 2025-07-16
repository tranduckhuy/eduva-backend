using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Folders.Queries
{
    public class GetFolderByIdHandler : IRequestHandler<GetFolderByIdQuery, FolderResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        public GetFolderByIdHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }
        public async Task<FolderResponse> Handle(GetFolderByIdQuery request, CancellationToken cancellationToken)
        {
            var folderRepository = _unitOfWork.GetCustomRepository<IFolderRepository>();
            var userRepository = _unitOfWork.GetCustomRepository<IUserRepository>();
            var classRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();

            var folder = await folderRepository.GetByIdAsync(request.Id);
            if (folder == null)
                throw new AppException(CustomCode.FolderNotFound);

            if (folder.OwnerType == OwnerType.Personal)
            {
                if (folder.UserId != request.UserId)
                    throw new AppException(CustomCode.Forbidden);
            }
            else if (folder.OwnerType == OwnerType.Class)
            {
                if (folder.ClassId == null)
                    throw new AppException(CustomCode.FolderNotFound);
                var classroom = await classRepository.GetByIdAsync(folder.ClassId.Value);
                if (classroom == null)
                    throw new AppException(CustomCode.ClassNotFound);
                var user = await userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                    throw new AppException(CustomCode.UserNotExists);

                var roles = await _userManager.GetRolesAsync(user);

                if ((roles.Contains(nameof(Role.Teacher)) || roles.Contains(nameof(Role.ContentModerator))) && classroom.TeacherId == user.Id)
                {
                    // OK
                }
                else if (roles.Contains(nameof(Role.SchoolAdmin)) && user.SchoolId != null && classroom.SchoolId == user.SchoolId)
                {
                    // OK
                }
                else if (roles.Contains(nameof(Role.SystemAdmin)))
                {
                    // OK
                }
                else
                {
                    throw new AppException(CustomCode.Forbidden);
                }
            }
            else
            {
                throw new AppException(CustomCode.Forbidden);
            }

            // Map folder to response
            var response = AppMapper<AppMappingProfile>.Mapper.Map<FolderResponse>(folder);

            var lessonMaterialRepo = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();
            var countsByFolder = await lessonMaterialRepo.GetApprovedMaterialCountsByFolderAsync(new List<Guid> { folder.Id }, cancellationToken);

            response.CountLessonMaterial = countsByFolder.TryGetValue(folder.Id, out var count) ? count : 0;

            return response;
        }
    }
}