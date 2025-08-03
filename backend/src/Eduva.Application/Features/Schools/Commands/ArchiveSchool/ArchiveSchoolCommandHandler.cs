using Eduva.Application.Exceptions.School;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.Schools.Commands.ArchiveSchool
{
    public class ArchiveSchoolCommandHandler : IRequestHandler<ArchiveSchoolCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISchoolRepository _schoolRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;

        public ArchiveSchoolCommandHandler(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _schoolRepository = _unitOfWork.GetCustomRepository<ISchoolRepository>();
            _userRepository = _unitOfWork.GetCustomRepository<IUserRepository>();
            _authService = authService;
        }

        public async Task<Unit> Handle(ArchiveSchoolCommand request, CancellationToken cancellationToken)
        {
            var school = await _schoolRepository.GetByIdAsync(request.SchoolId) ?? throw new SchoolNotFoundException();

            if (school.Status == EntityStatus.Archived)
            {
                throw new SchoolAlreadyArchivedException();
            }

            school.Status = EntityStatus.Archived;
            school.LastModifiedAt = DateTimeOffset.UtcNow;
            _schoolRepository.Update(school);

            var users = await _userRepository.GetUsersBySchoolIdAsync(school.Id, cancellationToken);
            foreach (var user in users)
            {
                user.Status = EntityStatus.Inactive;
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                _userRepository.Update(user);

                await _authService.InvalidateAllUserTokensAsync(user.Id.ToString());
            }

            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}