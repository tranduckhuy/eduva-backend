using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries
{
    public class GetTeacherClassesHandler : IRequestHandler<GetTeacherClassesQuery, Pagination<ClassResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTeacherClassesHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<ClassResponse>> Handle(GetTeacherClassesQuery request, CancellationToken cancellationToken)
        {
            // Create ClassSpecParam with TeacherId
            var classSpecParam = request.ClassSpecParam;
            classSpecParam.TeacherId = request.TeacherId;

            var spec = new ClassSpecification(classSpecParam);

            var result = await _unitOfWork.GetCustomRepository<IClassroomRepository>()
                .GetWithSpecAsync(spec);

            var classrooms = AppMapper.Mapper.Map<Pagination<ClassResponse>>(result);

            return classrooms;
        }
    }
}
