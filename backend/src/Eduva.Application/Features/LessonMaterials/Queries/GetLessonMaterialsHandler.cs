using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public class GetLessonMaterialsHandler : IRequestHandler<GetLessonMaterialsQuery, Pagination<LessonMaterialResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetLessonMaterialsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<LessonMaterialResponse>> Handle(GetLessonMaterialsQuery request, CancellationToken cancellationToken)
        {
            var spec = new LessonMaterialSpecification(request.LessonMaterialSpecParam);

            var result = await _unitOfWork.GetCustomRepository<ILessonMaterialRepository>()
                .GetWithSpecAsync(spec);

            var lessonMaterials = AppMapper.Mapper.Map<Pagination<LessonMaterialResponse>>(result);

            return lessonMaterials;
        }
    }
}
