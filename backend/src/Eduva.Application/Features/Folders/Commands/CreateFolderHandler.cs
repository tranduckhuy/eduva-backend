using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Folders.Commands
{
    public class CreateFolderHandler : IRequestHandler<CreateFolderCommand, FolderResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateFolderHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<FolderResponse> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
        {
            // Default to personal folder
            request.OwnerType = OwnerType.Personal;
            request.UserId = request.CurrentUserId;

            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepository.GetByIdAsync(request.CurrentUserId);
            if (user == null)
            {
                throw new AppException(CustomCode.UserIdNotFound);
            }

            // If classId is provided and not empty, check if a class folder can be created
            if (request.ClassId.HasValue && request.ClassId.Value != Guid.Empty)
            {
                var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
                var classroom = await classRepository.GetByIdAsync(request.ClassId.Value);

                if (classroom != null && classroom.TeacherId == request.CurrentUserId)
                {
                    // If class exists and user is the teacher of the class -> create class folder
                    request.OwnerType = OwnerType.Class;
                    request.UserId = null;
                }
                else
                {
                    // If class doesn't exist or user is not the teacher -> create personal folder
                    request.ClassId = null;
                }
            }
            else
            {
                // Explicitly set ClassId to null if it's empty or not provided
                request.ClassId = null;
            }

            // Check for duplicate folder name within the same scope
            var folderRepository = _unitOfWork.GetCustomRepository<IFolderRepository>();
            bool folderExists = await folderRepository.ExistsAsync(f =>
                f.Name == request.Name &&
                (request.OwnerType == OwnerType.Personal && f.UserId == request.UserId ||
                 request.OwnerType == OwnerType.Class && f.ClassId == request.ClassId) &&
                f.Status == EntityStatus.Active);

            if (folderExists)
            {
                throw new AppException(CustomCode.FolderNameAlreadyExists);
            }

            // Get the next order number
            int nextOrder = await folderRepository.GetMaxOrderAsync(request.UserId, request.ClassId) + 1;

            // Create new folder
            var folder = new Folder
            {
                Name = request.Name,
                UserId = request.UserId,
                ClassId = request.ClassId,
                OwnerType = request.OwnerType,
                Order = nextOrder,
                Status = EntityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            try
            {
                // Save folder first
                await folderRepository.AddAsync(folder);
                await _unitOfWork.CommitAsync();

                // Load navigation properties for correct mapping
                if (folder.OwnerType == OwnerType.Personal && folder.UserId.HasValue)
                {
                    folder.User = await userRepository.GetByIdAsync(folder.UserId.Value);
                }
                else if (folder.OwnerType == OwnerType.Class && folder.ClassId.HasValue)
                {
                    var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
                    folder.Class = await classRepository.GetByIdAsync(folder.ClassId.Value);
                }

                // Map to response with loaded navigation properties
                return AppMapper<AppMappingProfile>.Mapper.Map<FolderResponse>(folder);
            }
            catch (Exception)
            {
                throw new AppException(CustomCode.FolderCreateFailed);
            }
        }
    }
}
