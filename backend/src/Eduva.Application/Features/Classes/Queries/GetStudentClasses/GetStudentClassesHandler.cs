using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries.GetStudentClasses
{
    public class GetStudentClassesHandler : IRequestHandler<GetStudentClassesQuery, Pagination<StudentClassResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetStudentClassesHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<StudentClassResponse>> Handle(GetStudentClassesQuery request, CancellationToken cancellationToken)
        {
            // Ensure StudentId is set in the spec params
            request.StudentClassSpecParam.StudentId = request.StudentId;

            // Create specification
            var spec = new StudentClassSpecification(request.StudentClassSpecParam);

            // Get repository and use GetWithSpecAsync method
            var result = await _unitOfWork.GetCustomRepository<IStudentClassRepository>()
                .GetWithSpecAsync(spec);

            // Map to response model
            var studentClasses = AppMapper.Mapper.Map<Pagination<StudentClassResponse>>(result);

            return studentClasses;
        }
    }
}
