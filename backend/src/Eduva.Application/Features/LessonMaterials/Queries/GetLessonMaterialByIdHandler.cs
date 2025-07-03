using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.LessonMaterial;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public class GetLessonMaterialByIdHandler : IRequestHandler<GetLessonMaterialByIdQuery, LessonMaterialResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStorageService _storageService;

        public GetLessonMaterialByIdHandler(IUnitOfWork unitOfWork, IStorageService storageService)
        {
            _unitOfWork = unitOfWork;
            _storageService = storageService;
        }

        public async Task<LessonMaterialResponse> Handle(GetLessonMaterialByIdQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();

            var lessonMaterial = await repository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
                ?? throw new LessonMaterialNotFountException(request.Id);

            // Check if the user has access to the lesson material
            var user = await _unitOfWork.GetCustomRepository<IUserRepository>()
                .GetByIdAsync(request.UserId);

            if (user == null || !HasAccessToLessonMaterial(user.SchoolId, lessonMaterial.SchoolId))
            {
                throw new AppException(CustomCode.Forbidden);
            }


            var response = AppMapper.Mapper.Map<LessonMaterialResponse>(lessonMaterial);
            response.SourceUrl = _storageService.GetReadableUrl(lessonMaterial.SourceUrl);

            return response;
        }

        private static bool HasAccessToLessonMaterial(int? userSchoolId, int? lessonMaterialSchoolId)
        {
            if (lessonMaterialSchoolId == null || userSchoolId == null)
            {
                return false;
            }

            return userSchoolId.HasValue && userSchoolId.Value == lessonMaterialSchoolId.Value;
        }
    }
}
