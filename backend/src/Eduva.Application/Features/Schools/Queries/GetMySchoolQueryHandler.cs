using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Schools.Queries
{
    public class GetMySchoolQueryHandler : IRequestHandler<GetMySchoolQuery, SchoolResponse>
    {
        private readonly ISchoolRepository _schoolRepo;
        private readonly IUserRepository _userRepo;

        public GetMySchoolQueryHandler(IUnitOfWork unitOfWork)
        {
            _schoolRepo = unitOfWork.GetCustomRepository<ISchoolRepository>();
            _userRepo = unitOfWork.GetCustomRepository<IUserRepository>();
        }

        public async Task<SchoolResponse> Handle(GetMySchoolQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepo.GetByIdAsync(request.SchoolAdminId) ?? throw new UserNotExistsException();

            if (user.SchoolId == null)
            {
                throw new UserNotPartOfSchoolException();
            }

            var school = await _schoolRepo.GetByIdAsync(user.SchoolId.Value) ?? throw new SchoolNotFoundException();

            return AppMapper.Mapper.Map<SchoolResponse>(school);
        }
    }
}