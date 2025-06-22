using Eduva.Application.Common.Mappings;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Commands
{
    public class CreateLessonMaterialHandler : IRequestHandler<CreateLessonMaterialCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateLessonMaterialHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(CreateLessonMaterialCommand request, CancellationToken cancellationToken)
        {
            var lessonMaterialRepository = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();
            var folderLessonMaterialRepository = _unitOfWork.GetRepository<FolderLessonMaterial, int>();

            var createdLessonMaterials = new List<LessonMaterial>();

            try
            {
                // Begin transaction
                await _unitOfWork.BeginTransactionAsync();

                foreach (var materialRequest in request.LessonMaterials)
                {
                    // Map to entity
                    var lessonMaterial = AppMapper.Mapper.Map<LessonMaterial>(materialRequest);

                    // Set default status and visibility
                    lessonMaterial.Id = Guid.NewGuid();
                    lessonMaterial.LessonStatus = LessonMaterialStatus.Draft;
                    lessonMaterial.Visibility = LessonMaterialVisibility.Private;
                    lessonMaterial.CreatedBy = request.CreatedBy;
                    lessonMaterial.SchoolId = request.SchoolId;

                    // Add to repository
                    await lessonMaterialRepository.AddAsync(lessonMaterial);
                    createdLessonMaterials.Add(lessonMaterial);
                }

                // Create folder-lesson material relationships
                for (int i = 0; i < createdLessonMaterials.Count; i++)
                {
                    var materialRequest = request.LessonMaterials[i];
                    var lessonMaterial = createdLessonMaterials[i];

                    var folderLessonMaterial = new FolderLessonMaterial
                    {
                        FolderID = request.FolderId,
                        LessonMaterialID = lessonMaterial.Id,
                    };

                    await folderLessonMaterialRepository.AddAsync(folderLessonMaterial);
                }

                // Final commit
                await _unitOfWork.CommitAsync();

                return Unit.Value;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
