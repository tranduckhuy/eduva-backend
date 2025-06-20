using Eduva.Application.Exceptions.School;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.Schools.Commands.ActivateSchool
{
    public class ActivateSchoolCommandHandler : IRequestHandler<ActivateSchoolCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISchoolRepository _schoolRepository;
        private readonly IUserRepository _userRepository;

        public ActivateSchoolCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _schoolRepository = _unitOfWork.GetCustomRepository<ISchoolRepository>();
            _userRepository = _unitOfWork.GetCustomRepository<IUserRepository>();
        }

        public async Task<Unit> Handle(ActivateSchoolCommand request, CancellationToken cancellationToken)
        {
            var school = await _schoolRepository.GetByIdAsync(request.SchoolId)
                ?? throw new SchoolNotFoundException();

            if (school.Status == EntityStatus.Active)
            {
                throw new SchoolAlreadyActiveException();
            }

            school.Status = EntityStatus.Active;
            school.LastModifiedAt = DateTimeOffset.UtcNow;
            _schoolRepository.Update(school);

            var users = await _userRepository.GetUsersBySchoolIdAsync(school.Id, cancellationToken);
            foreach (var user in users)
            {
                user.Status = EntityStatus.Active;
                user.LockoutEnabled = false;
                user.LockoutEnd = null;
                _userRepository.Update(user);
            }

            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}