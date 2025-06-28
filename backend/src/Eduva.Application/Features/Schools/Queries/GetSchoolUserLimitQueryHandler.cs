using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Schools.Queries
{
    public class GetSchoolUserLimitQueryHandler : IRequestHandler<GetSchoolUserLimitQuery, SchoolUserLimitResponse>
    {
        private readonly ISchoolRepository _schoolRepository;

        public GetSchoolUserLimitQueryHandler(ISchoolRepository schoolRepository)
        {
            _schoolRepository = schoolRepository;
        }

        public async Task<SchoolUserLimitResponse> Handle(GetSchoolUserLimitQuery request, CancellationToken cancellationToken)
        {
            var (currentCount, maxCount) = await _schoolRepository.GetUserLimitInfoByUserIdAsync(request.ExecutorId, cancellationToken);

            return new SchoolUserLimitResponse
            {
                CurrentUserCount = currentCount,
                MaxUsers = maxCount
            };
        }
    }
}