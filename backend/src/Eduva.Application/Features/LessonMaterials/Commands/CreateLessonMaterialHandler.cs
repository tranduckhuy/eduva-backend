using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Commands
{
    public class CreateLessonMaterialHandler : IRequestHandler<CreateLessonMaterialCommand, LessonMaterialResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateLessonMaterialHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<LessonMaterialResponse> Handle(CreateLessonMaterialCommand request, CancellationToken cancellationToken)
        {
            var lessonMaterialRepository = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();

            var lessonMaterial = AppMapper.Mapper.Map<LessonMaterial>(request);

            lessonMaterial.LessonStatus = LessonMaterialStatus.Draft;
            lessonMaterial.Visibility = LessonMaterialVisibility.Private;

            await lessonMaterialRepository.AddAsync(lessonMaterial);

            try
            {
                await _unitOfWork.CommitAsync();
                return AppMapper.Mapper.Map<LessonMaterialResponse>(lessonMaterial);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
