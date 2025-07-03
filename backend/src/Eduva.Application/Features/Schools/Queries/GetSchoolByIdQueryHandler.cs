using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Schools.Queries
{
    public class GetSchoolByIdQueryHandler : IRequestHandler<GetSchoolByIdQuery, SchoolDetailResponse>
    {
        private readonly ISchoolRepository _schoolRepo;
        private readonly IUserRepository _userRepo;

        public GetSchoolByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _schoolRepo = unitOfWork.GetCustomRepository<ISchoolRepository>();
            _userRepo = unitOfWork.GetCustomRepository<IUserRepository>();
        }

        public async Task<SchoolDetailResponse> Handle(GetSchoolByIdQuery request, CancellationToken cancellationToken)
        {
            var school = await _schoolRepo.GetByIdAsync(request.Id)
                ?? throw new SchoolNotFoundException();

            var schoolAdmin = await _userRepo.GetSchoolAdminBySchoolIdAsync(school.Id, cancellationToken);

            return new SchoolDetailResponse
            {
                Id = school.Id,
                Name = school.Name,
                ContactEmail = school.ContactEmail,
                ContactPhone = school.ContactPhone,
                Address = school.Address,
                WebsiteUrl = school.WebsiteUrl,
                Status = school.Status,
                SchoolAdminId = schoolAdmin?.Id,
                SchoolAdminFullName = schoolAdmin?.FullName,
                SchoolAdminEmail = schoolAdmin?.Email
            };
        }
    }
}