using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Schools.Reponses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Schools.Commands
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

            var school = AppMapper.Mapper.Map<School>(request);
            school.Status = EntityStatus.Inactive;

            await schoolRepo.AddAsync(school);
            await _unitOfWork.CommitAsync();

            user.SchoolId = school.Id;
            userRepo.Update(user);
            await _unitOfWork.CommitAsync();

            return AppMapper.Mapper.Map<SchoolResponse>(school);
        }
    }
}