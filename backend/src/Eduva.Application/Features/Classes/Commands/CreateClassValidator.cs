using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using FluentValidation;

namespace Eduva.Application.Features.Classes.Commands
{
    public class CreateClassValidator : AbstractValidator<CreateClassCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateClassValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            RuleFor(x => x.Name)
              .NotEmpty().WithMessage("Class name is required")
              .MaximumLength(100).WithMessage("Class name must not exceed 100 characters")
              .MustAsync(ClassNameUniqueForTeacher).WithMessage("You already have a class with this name");

            RuleFor(x => x.SchoolId)
                .NotEmpty().WithMessage("School ID is required")
                .MustAsync(SchoolExists).WithMessage("School with specified ID does not exist");
        }

        private async Task<bool> ClassNameUniqueForTeacher(CreateClassCommand command, string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name) || command.TeacherId == Guid.Empty)
                return true;

            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();
            return !await classroomRepository.ExistsAsync(c =>
                c.TeacherId == command.TeacherId &&
                c.Name.ToLower() == name.ToLower());
        }

        private async Task<bool> SchoolExists(int schoolId, CancellationToken cancellationToken)
        {
            if (schoolId <= 0) return false;

            var schoolRepository = _unitOfWork.GetRepository<School, int>();
            return await schoolRepository.ExistsAsync(schoolId);
        }
    }
}
