using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries.GetOwnLessonMaterials
{
    public class GetOwnLessonMaterialsHandler : IRequestHandler<GetOwnLessonMaterialsQuery, Pagination<LessonMaterialResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetOwnLessonMaterialsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<LessonMaterialResponse>> Handle(GetOwnLessonMaterialsQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetOwnLessonMaterialsQuerySpecification(request.LessonMaterialSpecParam, request.UserId);

            var repository = _unitOfWork.GetRepository<LessonMaterial, Guid>();

            var lessonMaterials = await repository.GetWithSpecAsync(spec);

            return AppMapper<AppMappingProfile>.Mapper.Map<Pagination<LessonMaterialResponse>>(lessonMaterials);
        }
    }
}
