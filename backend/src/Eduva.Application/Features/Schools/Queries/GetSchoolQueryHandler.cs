using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Features.Schools.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.Schools.Queries
{
    public class GetSchoolQueryHandler : IRequestHandler<GetSchoolQuery, Pagination<SchoolResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSchoolQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<SchoolResponse>> Handle(GetSchoolQuery request, CancellationToken cancellationToken)
        {
            var spec = new SchoolSpecification(request.Param);

            var result = await _unitOfWork
                .GetRepository<School, int>()
                .GetWithSpecAsync(spec);

            return AppMapper<AppMappingProfile>.Mapper.Map<Pagination<SchoolResponse>>(result);
        }
    }
}