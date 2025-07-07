using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public class GetAllLessonMaterialsHandler : IRequestHandler<GetAllLessonMaterialsQuery, IReadOnlyList<LessonMaterialResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllLessonMaterialsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<LessonMaterialResponse>> Handle(GetAllLessonMaterialsQuery request, CancellationToken cancellationToken)
        {
            var lessonMaterials = await _unitOfWork.GetCustomRepository<ILessonMaterialRepository>()
                .GetAllBySchoolAsync(
                    request.UserId,
                    request.IsStudent,
                    request.SchoolId,
                    request.ClassId,
                    request.FolderId,
                    cancellationToken);

            return AppMapper<AppMappingProfile>.Mapper.Map<IReadOnlyList<LessonMaterialResponse>>(lessonMaterials);
        }
    }
}