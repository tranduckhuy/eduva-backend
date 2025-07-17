using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries.GetSchoolPublicLessonMaterials
{
    public class GetSchoolPublicLessonMaterialsHandler : IRequestHandler<GetSchoolPublicLessonMaterialsQuery, Pagination<LessonMaterialResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSchoolPublicLessonMaterialsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<LessonMaterialResponse>> Handle(GetSchoolPublicLessonMaterialsQuery request, CancellationToken cancellationToken)
        {
            var spec = new PublicLessonMaterialSpecification(request.LessonMaterialSpecParam);

            var result = await _unitOfWork.GetCustomRepository<ILessonMaterialRepository>()
                .GetWithSpecAsync(spec);

            var lessonMaterials = AppMapper<AppMappingProfile>.Mapper.Map<Pagination<LessonMaterialResponse>>(result);

            return lessonMaterials;
        }
    }
}
