using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Schools.Commands.CreateSchool
{
    public class CreateSchoolCommandHandler : IRequestHandler<CreateSchoolCommand, SchoolResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateSchoolCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SchoolResponse> Handle(CreateSchoolCommand request, CancellationToken cancellationToken)
        {
            var schoolRepo = _unitOfWork.GetCustomRepository<ISchoolRepository>();
            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();

            var user = await userRepo.GetByIdAsync(request.SchoolAdminId) ?? throw new AppException(CustomCode.UserIdNotFound);

            if (user.SchoolId != null)
            {
                throw new UserAlreadyHasSchoolException();
            }

            var school = new School
            {
                Name = request.Name,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Address = request.Address,
                WebsiteUrl = request.WebsiteUrl,
                Status = EntityStatus.Inactive
            };

            await schoolRepo.AddAsync(school);
            await _unitOfWork.CommitAsync();

            user.SchoolId = school.Id;
            userRepo.Update(user);
            await _unitOfWork.CommitAsync();

            return AppMapper.Mapper.Map<SchoolResponse>(school);
        }
    }
}