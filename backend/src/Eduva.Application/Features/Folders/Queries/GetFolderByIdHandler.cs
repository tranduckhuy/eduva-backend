using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Folders.Queries
{
    public class GetFolderByIdHandler : IRequestHandler<GetFolderByIdQuery, FolderResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetFolderByIdHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
                if (classroom.TeacherId == user.Id)
                {
                    // OK
                }
                else if (user.SchoolId != null && classroom.SchoolId == user.SchoolId)
                {
                    // OK
                }
                else if (user.SchoolId == null)
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