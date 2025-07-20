using Eduva.Application.Common.Mappings;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.LessonMaterials.Commands.CreateLessonMaterial
{
    public class CreateLessonMaterialHandler : IRequestHandler<CreateLessonMaterialCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateLessonMaterialHandler> _logger;
        private readonly IStorageService _storageService;

        public CreateLessonMaterialHandler(IUnitOfWork unitOfWork, ILogger<CreateLessonMaterialHandler> logger, IStorageService storageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _storageService = storageService;
        }

        public async Task<Unit> Handle(CreateLessonMaterialCommand request, CancellationToken cancellationToken)
        {
            var lessonMaterialRepository = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();
            var folderLessonMaterialRepository = _unitOfWork.GetRepository<FolderLessonMaterial, int>();

            var createdLessonMaterials = new List<LessonMaterial>();

            try
            {
                foreach (var materialRequest in request.LessonMaterials)
                {
                    // Map to entity
                    var lessonMaterial = AppMapper<AppMappingProfile>.Mapper.Map<LessonMaterial>(materialRequest);

                    // Set default status and visibility
                    lessonMaterial.Id = Guid.NewGuid();
                    lessonMaterial.LessonStatus = LessonMaterialStatus.Pending;
                    lessonMaterial.Visibility = LessonMaterialVisibility.Private;
                    lessonMaterial.CreatedByUserId = request.CreatedBy;
                    lessonMaterial.SchoolId = request.SchoolId;

                    // Add to repository
                    await lessonMaterialRepository.AddAsync(lessonMaterial);
                    createdLessonMaterials.Add(lessonMaterial);
                }

                // Create folder-lesson material relationships
                for (int i = 0; i < createdLessonMaterials.Count; i++)
                {
                    var folderLessonMaterial = new FolderLessonMaterial
                    {
                        FolderId = request.FolderId,
                        LessonMaterialId = createdLessonMaterials[i].Id,
                    };

                    await folderLessonMaterialRepository.AddAsync(folderLessonMaterial);
                }

                // Final commit
                await _unitOfWork.CommitAsync();

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lesson materials.");

                // Remove blobs if any were created
                await _storageService.DeleteRangeFileAsync(request.BlobNames);

                throw;
            }
        }
    }
}
