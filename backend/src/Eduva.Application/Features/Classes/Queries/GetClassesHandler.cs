using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries
{
    public class GetClassesHandler : IRequestHandler<GetClassesQuery, Pagination<ClassResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetClassesHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<ClassResponse>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
        {
            var spec = new ClassSpecification(request.ClassSpecParam);

            var result = await _unitOfWork.GetCustomRepository<IClassroomRepository>()
                .GetWithSpecAsync(spec);

            var classrooms = AppMapper.Mapper.Map<Pagination<ClassResponse>>(result);

            return classrooms;
        }
    }
}
