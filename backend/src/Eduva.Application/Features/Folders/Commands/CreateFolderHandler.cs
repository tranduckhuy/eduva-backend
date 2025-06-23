using Eduva.Application.Common.Exceptions;

using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.Folders.Commands
{
    public class CreateFolderHandler : IRequestHandler<CreateFolderCommand, FolderResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateFolderHandler> _logger;

        public CreateFolderHandler(IUnitOfWork unitOfWork, ILogger<CreateFolderHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<FolderResponse> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
        {
            // Default to personal folder
            request.OwnerType = OwnerType.Personal;
            request.UserId = request.CurrentUserId;

            // If classId is provided, check if a class folder can be created
            if (request.ClassId.HasValue)
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
                await folderRepository.AddAsync(folder);
                await _unitOfWork.CommitAsync();

                return AppMapper.Mapper.Map<FolderResponse>(folder);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Failed to create folder: {Message}", ex.Message);
                throw new AppException(CustomCode.FolderCreateFailed);
            }
        }
    }
}
